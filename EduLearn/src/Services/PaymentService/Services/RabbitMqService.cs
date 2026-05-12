using System.Text;
using System.Text.Json;
using EduLearn.PaymentService.Messages;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EduLearn.PaymentService.Services
{
    public interface IRabbitMqService
    {
        Task PublishPaymentProcessedAsync(PaymentProcessedMessage message);
        Task PublishPaymentFailedAsync(PaymentFailedMessage message);
        Task PublishPaymentRefundedAsync(PaymentRefundedMessage message);
    }

    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMqService> _logger;

        public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
        {
            _logger = logger;
            
            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/"
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Declare exchanges
                _channel.ExchangeDeclare("payment.exchange", ExchangeType.Fanout, durable: true);
                _channel.ExchangeDeclare("notification.exchange", ExchangeType.Fanout, durable: true);
                
                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                throw;
            }
        }

        public async Task PublishPaymentProcessedAsync(PaymentProcessedMessage message)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageBody);

                _channel.BasicPublish(
                    exchange: "payment.exchange",
                    routingKey: "",
                    basicProperties: null,
                    body: body);

                _logger.LogInformation($"Published payment processed message for payment {message.PaymentId}");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish payment processed message");
                throw;
            }
        }

        public async Task PublishPaymentFailedAsync(PaymentFailedMessage message)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageBody);

                _channel.BasicPublish(
                    exchange: "payment.exchange",
                    routingKey: "",
                    basicProperties: null,
                    body: body);

                _logger.LogInformation($"Published payment failed message for payment {message.PaymentId}");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish payment failed message");
                throw;
            }
        }

        public async Task PublishPaymentRefundedAsync(PaymentRefundedMessage message)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageBody);

                _channel.BasicPublish(
                    exchange: "payment.exchange",
                    routingKey: "",
                    basicProperties: null,
                    body: body);

                _logger.LogInformation($"Published payment refunded message for payment {message.PaymentId}");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish payment refunded message");
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
