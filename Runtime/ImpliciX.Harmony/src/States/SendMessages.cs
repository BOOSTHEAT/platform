using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Messages.Formatter;
using ImpliciX.Language.Core;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.Harmony.States
{
    public class SendMessages : BaseState
    {
        public SendMessages(IClock clock, Queue<IHarmonyMessage> elementsQueue, IPublishingContext context) : base(
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
                x => x.Item2 is ConnectToIotHub.ConnectionFailed);
        }

        public override bool CanHandle(Context context, DomainEvent @event)
        {
            return _acceptTicks && @event is SystemTicked ||
                   @event is ConnectToIotHub.ConnectionFailed;
        }

        protected override DomainEvent[] OnEntry(Context context, DomainEvent _)
        {
            return SendDiscoveryMessage(context);
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
                    return new DomainEvent[] { new ConnectToIotHub.ConnectionFailed(_clock.Now()) };
            }

            return Array.Empty<DomainEvent>();
        }

        private bool SendMessageWhileBlockingTicks(Context context, IHarmonyMessage message)
        {
            _acceptTicks = false;
            var status = context.AzureIoTHubAdapter.SendMessage(message, _context);
            _acceptTicks = status;
            return status;
        }

        public static DiscoveryMessage CreateDiscoveryMessage(Context context, TimeSpan currentTime)
        {
            return DiscoveryMessage.Create(
                context.SerialNumber,
                context.AppName,
                context.ReleaseVersion,
                context.UserTimeZone.Replace("__", "/"),
                context.DeviceId,
                currentTime);
        }

        private DomainEvent[] SendDiscoveryMessage(Context context)
        {
            var currentTime = _clock.Now();
            var message = CreateDiscoveryMessage(context, currentTime);
            Log.Debug("Sending 'Discover' message to Harmony");
            if (SendMessageWhileBlockingTicks(context, message))
            {
                Log.Information("'Discover' message sent to Harmony");
                return Array.Empty<DomainEvent>();
            }

            Log.Warning("'Discover' message sending timeout");
            return new DomainEvent[] { new ConnectToIotHub.ConnectionFailed(_clock.Now()) };
        }

        private readonly IClock _clock;
        private readonly Queue<IHarmonyMessage> _elementsQueue;
        private readonly IPublishingContext _context;
        private bool _acceptTicks;
    }

    public readonly struct DiscoveryMessage : IHarmonyMessage
    {
        private DiscoveryMessage(string serialNumber, string applicationName, string releaseVersion,
            string timeZone, string deviceId,
            string dateTime)
        {
            SerialNumber = serialNumber;
            ApplicationName = applicationName;
            ActiveRelease = releaseVersion;
            TimeZone = timeZone;
            DeviceId = deviceId;
            DateTime = dateTime;
        }

        public static DiscoveryMessage Create(
            string serialNumber,
            string applicationName,
            string releaseVersion,
            string timeZone,
            string deviceId, TimeSpan currentTime) =>
            new DiscoveryMessage(
                serialNumber,
                applicationName,
                releaseVersion,
                timeZone,
                deviceId,
                currentTime.Format());

        public string SerialNumber { get; }
        public string ApplicationName { get; }
        public string ActiveRelease { get; }
        public string TimeZone { get; }
        public string DeviceId { get; }
        public string DateTime { get; }

        public string Format(IPublishingContext context) => BasicFormatter.Format(this);

        public string GetMessageType() => "Discover";
    }
}