// Copyright 2015 Serilog Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using RabbitMQ.Client;
using Serilog.Sinks.RabbitMQ.Sinks.RabbitMQ;

namespace Serilog.Sinks.RabbitMQ
{
    /// <summary>
    /// RabbitMqClient - this class is the engine that lets you send messages to RabbitMq
    /// </summary>
    public class RabbitMQClient : IDisposable
    {
        // configuration member
        private readonly RabbitMQClientConfiguration _config;
        private readonly PublicationAddress _publicationAddress;

        // endpoint members
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private IBasicProperties _properties;

        /// <summary>
        /// Constructor for RabbitMqClient
        /// </summary>
        /// <param name="configuration">mandatory</param>
        public RabbitMQClient(RabbitMQClientConfiguration configuration)
        {
            // load configuration
            _config = configuration;
            _publicationAddress = new PublicationAddress(_config.ExchangeType, _config.Exchange, _config.RouteKey);

            // initialize
            _connectionFactory = GetConnectionFactory();
        }

        /// <summary>
        /// Configures a new ConnectionFactory, and returns it
        /// </summary>
        /// <returns></returns>
        private IConnectionFactory GetConnectionFactory()
        {
            // prepare connection factory
            var connectionFactory = new ConnectionFactory
            {
                UserName = _config.Username,
                Password = _config.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(2),
                UseBackgroundThreadsForIO = _config.UseBackgroundThreadsForIO
            };

            if (_config.SslOption != null)
            {
                connectionFactory.Ssl.Version = _config.SslOption.Version;
                connectionFactory.Ssl.CertPath = _config.SslOption.CertPath;
                connectionFactory.Ssl.ServerName = _config.SslOption.ServerName;
                connectionFactory.Ssl.Enabled = _config.SslOption.Enabled;
                connectionFactory.Ssl.AcceptablePolicyErrors = _config.SslOption.AcceptablePolicyErrors;
            }
            // setup heartbeat if needed
            if (_config.Heartbeat > 0)
                connectionFactory.RequestedHeartbeat = _config.Heartbeat;

            // only set, if has value, otherwise leave default
            if (_config.Port > 0) connectionFactory.Port = _config.Port;
            if (!string.IsNullOrEmpty(_config.VHost)) connectionFactory.VirtualHost = _config.VHost;
            if (_config.Protocol != null) connectionFactory.Protocol = _config.Protocol;

            // return factory
            return connectionFactory;
        }

        /// <summary>
        /// Publishes a message to RabbitMq Exchange
        /// </summary>
        /// <param name="message"></param>
        public void Publish(string message)
        {
            // Publish message to exchange.
            var channel = GetChannel();
            channel.BasicPublish(_publicationAddress, _properties, System.Text.Encoding.UTF8.GetBytes(message));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _model?.Dispose();
            _connection?.Dispose();
        }

        private IModel GetChannel()
        {
            if (_connection == null)
            {
                _connection = _connectionFactory.CreateConnection(_config.Hostnames);
            }

            if (_model == null)
            {
                _model = _connection.CreateModel();
            }

            if (_properties == null)
            {
                _properties = _model.CreateBasicProperties();
                _properties.DeliveryMode = (byte)_config.DeliveryMode; //persistence
            }

            return _model;
        }
    }
}
