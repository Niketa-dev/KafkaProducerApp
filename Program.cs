using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

public class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<IKafkaProducer,KafkaProducer>();
           // services.AddSingleton<RetryQueueService>();
          // services.AddHostedService<RetryProcessor>();
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource("KafkaProducer")
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddMeter("KafkaProducer")
                .AddConsoleExporter());
    })
    .Build();



        var kafkaProducer = host.Services.GetRequiredService<IKafkaProducer>();

        var payload = new ProducerPayload
        {
            OrderKey = "order1234",
            SubmittedDate = "2025-02-04",
            SourceSystem = "MyApp",
            ReferenceText = "Sample order",
            TotalOrderValue = 100.50m,
            LineItems = new List<LineItem>
            {
                new LineItem { ItemNumber = "item1", Quantity = 2, UnitPrice = 25.25m, PaymentTerm = "Net 30" }
            }
        };

        await kafkaProducer.SendMessageAsync(payload);

        await host.RunAsync();
    }
}
