
using Confluent.Kafka;

namespace UserService.Infrastructure.Kafka
{
    public interface IKafkaProducer
    {
        public Task ProduceAsync(string topic, Message<string, string> message);
    }
}
