using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

public class ProducerPayload
{
    public string OrderKey { get; set; }
    public string SubmittedDate { get; set; }
    public string SourceSystem { get; set; }
    public string ReferenceText { get; set; }
    public decimal TotalOrderValue { get; set; }
    public List<LineItem> LineItems { get; set; }
}

public class LineItem
{
    public string ItemNumber { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string PaymentTerm { get; set; }
}

public class KafkaProducer: IKafkaProducer
{
    private readonly string _bootstrapServers;
    private readonly string _topic;

    public KafkaProducer(IConfiguration configuration)
    {
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ??"localhost:9092";
        _topic = configuration["Kafka:Topic"] ?? "order-topic"; 
    }

    public async Task SendMessageAsync(ProducerPayload payload)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            Acks = Acks.All,  
            EnableIdempotence = true  
                    };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        var key = $"{payload.OrderKey}-{payload.SubmittedDate}-{payload.SourceSystem}";
        
        var payloadJson = JsonSerializer.Serialize(payload);

        try
        {
            var deliveryResult = await producer.ProduceAsync(_topic, new Message<string, string>
            {
                Key = key,
                Value = payloadJson
            });

            Console.WriteLine($"Delivered message to {deliveryResult.TopicPartitionOffset}");
            OpenTelemetryMetrics.RecordMessageDelivery();
        }
        catch (ProduceException<string, string> e)
        {
             OpenTelemetryMetrics.RecordMessageFailure(e.Error.Reason); 

            // await retryService.AddToRetryQueueAsync(payloadJson);
        }
    }
}
