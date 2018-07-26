﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.RabbitMQ
{
    public class RabbitMQHealthContributor : IHealthContributor
    {
        private readonly RabbitMQProviderConnectorFactory _factory;
        private readonly ILogger<RabbitMQHealthContributor> _logger;

        public RabbitMQHealthContributor(RabbitMQProviderConnectorFactory factory, ILogger<RabbitMQHealthContributor> logger = null)
        {
            _factory = factory;
            _logger = logger;
        }

        public string Id => "RabbitMQ";

        public HealthCheckResult Health()
        {
            _logger?.LogTrace("Checking RabbitMQ connection health");
            var result = new HealthCheckResult();
            try
            {
                var connFactory = _factory.Create(null);
                var createConnectionMethod = ConnectorHelpers.FindMethod(connFactory.GetType(), "CreateConnection", new Type[0]);
                var connection = ConnectorHelpers.Invoke(createConnectionMethod, connFactory, null);

                if (connection == null)
                {
                    throw new ConnectorException("Failed to open RabbitMQ connection!");
                }

                // Spring Boot health checks also include RMQ version the server is running
                result.Details.Add("status", HealthStatus.UP.ToString());
                result.Status = HealthStatus.UP;
                _logger?.LogTrace("RabbitMQ connection up!");
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                {
                    e = e.InnerException;
                }

                _logger?.LogError("RabbitMQ connection down! {HealthCheckException}", e.Message);
                result.Details.Add("error", e.GetType().Name + ": " + e.Message);
                result.Details.Add("status", HealthStatus.DOWN.ToString());
                result.Status = HealthStatus.DOWN;
                result.Description = e.Message;
            }

            return result;
        }
    }
}