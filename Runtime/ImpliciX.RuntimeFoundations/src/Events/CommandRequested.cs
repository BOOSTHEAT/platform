using System;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RuntimeFoundations.Events
{
    public class CommandRequested : PublicDomainEvent, IEquatable<CommandRequested>
    {
        private IModelCommand ModelCommand { get; }
       
        private CommandRequested(IModelCommand modelCommand, TimeSpan at) : base(Guid.NewGuid(), at)
        {
            ModelCommand = modelCommand;
        }

        public Urn Urn => ModelCommand.Urn;
        public object Arg => ModelCommand.Arg;

        public static CommandRequested Create<T>(CommandUrn<T> urn, T arg, TimeSpan at)=>
            new CommandRequested(Command<T>.Create(urn, arg), at );

        public static CommandRequested Create<T>(Command<T> command, TimeSpan at) => new CommandRequested(command, at);

        // TODO: This constructor should be internal
        public static CommandRequested Create(IModelCommand modelCommand, TimeSpan at)
        {
            return new CommandRequested(modelCommand, at);
        }

        public bool Equals(CommandRequested other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(ModelCommand, other.ModelCommand);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CommandRequested) obj);
        }

        public override int GetHashCode()
        {
            return (ModelCommand != null ? ModelCommand.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return $"{Urn}:={Arg}";
        }
    }
}