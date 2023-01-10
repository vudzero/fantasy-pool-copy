using MassTransit;

namespace MassTest;

public class MessageGenerator : BackgroundService
{
    private readonly IBus _bus;

    public MessageGenerator(IBus bus)
    {
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var messageIndex = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);

            messageIndex++;
            //Console.WriteLine("****Publishing message with index {0}***", messageIndex);
            await _bus.Publish(new Step1() { PartitionId = 1, CorrelatedId = Guid.NewGuid(), MessageIndex = messageIndex }, stoppingToken);
        }
    }
}