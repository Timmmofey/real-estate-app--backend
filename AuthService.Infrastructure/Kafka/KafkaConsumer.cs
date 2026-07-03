using AuthService.Domain.Abstactions;
using Classified.Shared.Constants;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthService.Infrastructure.Kafka  
{
    public class KafkaConsumer : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;

        public KafkaConsumer(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _config = config;
        }

        protected override  Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => { 
                _ = ConsumeAsync(KafkaTopic.RecalledSessionsTopic, stoppingToken);
            }, stoppingToken);
        }

        public async Task ConsumeAsync(string topic, CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                GroupId = "recalled-session-group",
                //BootstrapServers = "localhost:9092",
                BootstrapServers = _config["Kafka:BootstrapServers"],
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult == null) continue;

                var userId = consumeResult.Message.Value;
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

                if (Guid.TryParse(userId, out var guid))
                {
                    await dbContext.DeleteAllRefreskTokensByUserId(guid, stoppingToken);
                }
            }

            consumer.Close();
        }
    }
}
