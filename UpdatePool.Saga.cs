using MassTransit;

namespace MassTest;

public class Step1
{
    public int PartitionId { get; set; }
    public int MessageIndex { get; set; }
    public Guid CorrelatedId { get; set; }
}

public class Step2
{
    public int PartitionId { get; set; }
    public int MessageIndex { get; set; }
    public Guid CorrelatedId { get; set; }
}

public class Step3
{
    public int PartitionId { get; set; }
    public int MessageIndex { get; set; }
    public Guid CorrelatedId { get; set; }
}

public class StateMachineState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public int CurrentState { get; set; }
}

public class StateMachine : MassTransitStateMachine<StateMachineState>
{
    public StateMachine()
    {
        InstanceState(x => x.CurrentState, Step1, Step2, Step3);

        Event(() => OnStep1, x => x.CorrelateById(context => context.Message.CorrelatedId));
        Event(() => OnStep2, x => x.CorrelateById(context => context.Message.CorrelatedId));
        Event(() => OnStep3, x => x.CorrelateById(context => context.Message.CorrelatedId));

        Initially(
            When(OnStep1)
                .ThenAsync(async context =>
                {
                    Console.WriteLine("Executing Step 1 for message {0}", context.Message.MessageIndex);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                })
                .PublishAsync(context => context.Init<Step2>(new Step2()
                {
                    CorrelatedId = context.Saga.CorrelationId,
                    PartitionId = context.Message.PartitionId,
                    MessageIndex = context.Message.MessageIndex
                }))
                .TransitionTo(Step1));

        DuringAny(
            When(OnStep2)
                .ThenAsync(async context =>
                {
                    Console.WriteLine("Executing Step 2 for message {0}", context.Message.MessageIndex);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                })
                .PublishAsync(context => context.Init<Step3>(new Step3()
                {
                    CorrelatedId = context.Saga.CorrelationId,
                    PartitionId = context.Message.PartitionId,
                    MessageIndex = context.Message.MessageIndex
                }))
                .TransitionTo(Step2),
            When(OnStep3)
                .ThenAsync(async context =>
                {
                    Console.WriteLine("Executing Step 3 for message {0}", context.Message.MessageIndex);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                })
                .TransitionTo(Step3)
                .Finalize());

        SetCompletedWhenFinalized();
    }

    // States
    public State Step1 { get; private set; } = default!;
    public State Step2 { get; private set; } = default!;
    public State Step3 { get; private set; } = default!;

    // Events
    public Event<Step1> OnStep1 { get; private set; } = default!;
    public Event<Step2> OnStep2 { get; private set; } = default!;
    public Event<Step3> OnStep3 { get; private set; } = default!;
}

public class UpdatePoolSagaDefinition : SagaDefinition<StateMachineState>
{
    private const int concurrencyLimit = 10;

    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<StateMachineState> sagaConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(5, 1000));
        endpointConfigurator.UseInMemoryOutbox();

        endpointConfigurator.ConcurrentMessageLimit = concurrencyLimit;

        var p = endpointConfigurator.CreatePartitioner(1);

        sagaConfigurator.Message<Step1>(x => x.UsePartitioner(p, m => m.Message.PartitionId));
    }
}