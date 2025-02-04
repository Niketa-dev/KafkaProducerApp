using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

public class RetryQueueService
{
    private readonly QueueClient _retryQueue;
    private readonly QueueClient _deadLetterQueue;

    public RetryQueueService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:QueueConnectionString"];
        _retryQueue = new QueueClient(connectionString, configuration["AzureStorage:RetryQueueName"]);
        _deadLetterQueue = new QueueClient(connectionString, configuration["AzureStorage:DeadLetterQueueName"]);
        
    }

    public async Task AddToRetryQueueAsync(string message)
    {
        await _retryQueue.SendMessageAsync(message);
        Console.WriteLine("Message added to retry queue.");
    }

    public async Task AddToDeadLetterQueueAsync(string message)
    {
        await _deadLetterQueue.SendMessageAsync(message);
        Console.WriteLine("Message added to dead letter queue.");
    }
}
