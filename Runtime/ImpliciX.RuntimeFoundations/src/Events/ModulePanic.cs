using System;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Modules;

namespace ImpliciX.RuntimeFoundations.Events
{
    public class ModulePanic : PublicDomainEvent
    {
        public string ModuleId { get; }

        public static ModulePanic Create(string moduleId, TimeSpan at) =>
            new ModulePanic(moduleId, at);

        private ModulePanic(string moduleId, TimeSpan at) : base(Guid.NewGuid(), at)
        {
            ModuleId = moduleId;
        }

        protected bool Equals(ModulePanic other) => ModuleId == other.ModuleId;

        public override bool Equals(object obj) =>
            !ReferenceEquals(null, obj) && (ReferenceEquals(this, obj) ||
                                            obj.GetType() == this.GetType() && Equals((ModulePanic) obj));

        public override int GetHashCode() => ModuleId.GetHashCode();

        public static bool operator ==(ModulePanic left, ModulePanic right) => Equals(left, right);

        public static bool operator !=(ModulePanic left, ModulePanic right) => !Equals(left, right);
    }

    public static class ModulePanicExtensions
    {
        public static ImpliciXFeatureDefinition HandlesPanic(this ImpliciXFeatureDefinition bfd, string moduleId, Action onPanic) =>
            bfd.Handles<ModulePanic>(
                mp =>
                {
                    Log.Information("Handling panic in {0}", moduleId);
                    onPanic();
                    return new DomainEvent[0];
                },
                mp => mp.ModuleId == moduleId);
    }
}