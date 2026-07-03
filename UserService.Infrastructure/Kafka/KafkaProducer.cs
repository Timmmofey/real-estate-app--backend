using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace UserService.Infrastructure.Kafka
{
    public class KafkaProducer: IKafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        public KafkaProducer(IConfiguration config)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"],
                MessageTimeoutMs = int.Parse(config["Kafka:MessageTimeoutMs"]!),
                SocketTimeoutMs = int.Parse(config["Kafka:SocketTimeoutMs"]!),
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task ProduceAsync(string topic, Message<string, string> message)
        {
            await _producer.ProduceAsync(topic, message);
        }
    }
}
