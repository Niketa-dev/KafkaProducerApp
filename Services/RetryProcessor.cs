using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class RetryProcessor : BackgroundService
{
    private readonly QueueClient _retryQueue;
    private readonly KafkaProducer _kafkaProducer;
    private readonly RetryQueueService _retryQueueService;

    public RetryProcessor(IConfiguration configuration, KafkaProducer kafkaProducer, RetryQueueService retryQueueService)
    {
        var connectionString = configuration["AzureStorage:QueueConnectionString"];
        _retryQueue = new QueueClient(connectionString, configuration["AzureStorage:RetryQueueName"]);
        _kafkaProducer = kafkaProducer;
        _retryQueueService = retryQueueService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await _retryQueue.ReceiveMessageAsync();
            if (message.Value != null)
            {
                string messageText = Encoding.UTF8.GetString(Convert.FromBase64String(message.Value.MessageText));

                var payload = JsonSerializer.Deserialize<ProducerPayload>(messageText);

                if (payload != null)
                {
                    try{
                    await _kafkaProducer.SendMessageAsync(payload);

                        await _retryQueue.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);
                        
                    }
                    catch (Exception)
                    {
                        await _retryQueueService.AddToDeadLetterQueueAsync(messageText);
                    }
                }
                else
                {
                    Console.WriteLine("Failed to deserialize message to ProducerPayload.");
                    await _retryQueueService.AddToDeadLetterQueueAsync(messageText);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
