using MassTransit;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace MassTest;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseKestrel(opt => opt.ListenAnyIP(80, o => o.Protocols = HttpProtocols.Http1));

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(Program).Assembly);

            x.AddSagaStateMachine<StateMachine, StateMachineState, UpdatePoolSagaDefinition>()
                .InMemoryRepository();

            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        builder.Services.AddOptions<MassTransitHostOptions>()
            .Configure(options =>
            {
                // Waits until the bus is started before
                // returning from IHostedService.StartAsync
                options.WaitUntilStarted = true;

                // Limits the wait time when starting the bus
                options.StartTimeout = TimeSpan.FromSeconds(10);
                options.StopTimeout = TimeSpan.FromSeconds(5);
            }); ;

        builder.Services.AddHostedService<MessageGenerator>();

        var app = builder.Build();

        app.Run();
    }
}