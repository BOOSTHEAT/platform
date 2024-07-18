using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.FiniteStateMachine;
using ImpliciX.ThingsBoard.Messages;

namespace ImpliciX.ThingsBoard.States
{
  public class SendMessages : BaseState
  {
    public SendMessages(IClock clock, Queue<IThingsBoardMessage> elementsQueue,
      IPublishingContext context) : base(
      nameof(SendMessages))
    {
      _clock = clock;
      _elementsQueue = elementsQueue;
      _context = context;
      _acceptTicks = true;
    }

    public Transition<BaseState, (Context, DomainEvent)> WhenConnectionIsFailed(BaseState to)
    {
      return new Transition<BaseState, (Context, DomainEvent)>(this, to,
        x => x.Item2 is ConnectToBroker.ConnectionFailed);
    }

    public override bool CanHandle(Context context, DomainEvent @event)
    {
      return _acceptTicks && @event is SystemTicked ||
             @event is ConnectToBroker.ConnectionFailed;
    }

    protected override DomainEvent[] OnState(Context context, DomainEvent @event)
    {
      return @event switch
      {
        SystemTicked _ => SendMessagesFromQueuedElements(context),
        _ => Array.Empty<DomainEvent>()
      };
    }

    private DomainEvent[] SendMessagesFromQueuedElements(Context context)
    {
      while (_elementsQueue.Any())
      {
        var message = _elementsQueue.Peek();
        if (SendMessageWhileBlockingTicks(context, message))
          _elementsQueue.Dequeue();
        else
          return new DomainEvent[] { new ConnectToBroker.ConnectionFailed(_clock.Now()) };
      }

      return Array.Empty<DomainEvent>();
    }

    private bool SendMessageWhileBlockingTicks(Context context, IThingsBoardMessage message)
    {
      _acceptTicks = false;
      var status = context.Adapter.SendMessage(message, _context);
      _acceptTicks = status;
      return status;
    }

    private readonly IClock _clock;
    private readonly Queue<IThingsBoardMessage> _elementsQueue;
    private readonly IPublishingContext _context;
    private bool _acceptTicks;
  }
}