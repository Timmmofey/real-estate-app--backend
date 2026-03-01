using Confluent.Kafka;

namespace UserService.Infrastructure.Kafka
{
    public class KafkaProducer: IKafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        public KafkaProducer() {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
                MessageTimeoutMs = 3000,
                SocketTimeoutMs = 3000
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task ProduceAsync(string topic, Message<string, string> message)
        {
            try
            {
                await _producer.ProduceAsync(topic, message);
            }
            catch (ProduceException<string, string> e)
            {
                throw;
            }
        }
    }
}
