using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data;
using ImpliciX.Data.Api;

namespace ImpliciX.Designer.Simulation
{
    public class MessageEvent
    {
        public const string command = nameof(command); 
        public const string properties = nameof(properties); 
        public const string empty = "";
        
        protected bool Equals(MessageEvent other)
        {
            return At.Equals(other.At) && _kind == other._kind && _args.SequenceEqual(other._args);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MessageEvent) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(At, _kind, _args);
        }

        public MessageEvent(TimeSpan at, string kind)
        {
            At = at;
            _kind = kind;
            _args = new List<(string, string)>();
        }

        public TimeSpan At { get; }
        private readonly string _kind;
        private readonly List<(string, string)> _args;

        public bool IsCommand() => _kind == command;
        
        public MessageEvent Add(string arguments, string value)
        {
            _args.Add((arguments, value));
            return this;
        }

        public string ToJson() =>
            (_kind switch
            {
                command => WebsocketApiV2.CommandMessage
                  .SideEffect(() => Debug.PreCondition(()=>_args.Count == 1, ()=>"Wrong number of arguments in command definition"))
                  .WithParameter(_args[0].Item1, _args[0].Item2),
                properties => WebsocketApiV2.PropertiesMessage.WithProperties(_args),
                _ => throw new NotSupportedException($"Unknown message kind {_kind}")
            }).ToJson();
        
    }
}
