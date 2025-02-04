using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Trace;

public class OpenTelemetryMetrics
{
    private static readonly Meter _meter = new Meter("KafkaProducerMetrics", "1.0");
    private static readonly Counter<long> _messageDeliveryCounter;
    private static readonly Counter<long> _messageFailureCounter;
    private static readonly ActivitySource _activitySource;

    static OpenTelemetryMetrics()
    {
        _messageDeliveryCounter = _meter.CreateCounter<long>("message_deliveries", description: "Count of successful Kafka messages delivered");
        _messageFailureCounter = _meter.CreateCounter<long>("message_failures", description: "Count of Kafka message delivery failures");
        _activitySource = new ActivitySource("KafkaProducerTracer");
    }

    public static void RecordMessageDelivery()
    {
        _messageDeliveryCounter.Add(1);
    }

    public static void RecordMessageFailure(string errorReason)
    {
        _messageFailureCounter.Add(1);
        CaptureErrorTrace(errorReason);
    }

    private static void CaptureErrorTrace(string errorReason)
    {
        using (var activity = _activitySource.StartActivity("KafkaMessageFailure"))
        {
            activity?.SetTag("error.reason", errorReason);
            activity?.SetStatus(ActivityStatusCode.Error, errorReason);
            Console.WriteLine($"Error traced: {errorReason}");
        }
    }
}
