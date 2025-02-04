using System.Threading.Tasks;

public interface IKafkaProducer
{
    Task SendMessageAsync(ProducerPayload payload);
}
