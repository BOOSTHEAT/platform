using System;
using System.Collections.Generic;
using System.Linq;
using Femyou;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Clock;

namespace ImpliciX.FmuDriver
{
    public class FmuContext
    {
        private IModel FmuModel { get; }
        public IVariable[] FmuReadVariables { get; }
        public IVariable[] FmuWrittenVariables { get; }
        public IVariable[] FmuWrittenParameters { get; }
        public FmuInstance FmuInstance { get; set; }

        public IEnumerable<(string key, string filepath, Action<object, IFmuIO, IVariable> write)> FmuWrittenParametersMap { get; }
        public IEnumerable<(Urn urn, string key, Action<object, IFmuIO, IVariable> Write)> FmuWrittenVariablesMap { get; }

        public FmuContext(IClock clock, DriverFmuModuleDefinition moduleDefinition, ICallbacks callback = null)
        {
            FmuModel = Model.Load(moduleDefinition.FmuPackage);
            var readVariablesMap = moduleDefinition.ReadVariables.Select(v => v);
            FmuReadVariables = moduleDefinition.ReadVariables.Select(c => FmuModel.Variables[c.Item3]).ToArray();
            FmuWrittenVariables = moduleDefinition.WriteVariables.Select(c => FmuModel.Variables[c.Item2]).ToArray();
            FmuWrittenParameters = moduleDefinition.ParameterFiles.Select(c => FmuModel.Variables[c.Item1]).ToArray();
            FmuWrittenVariablesMap =
                moduleDefinition.WriteVariables.Select(v => (v.Item1, v.Item2, CreateAction(v.Item1))).ToArray();
            FmuWrittenParametersMap =
                moduleDefinition.ParameterFiles.Select(f => (f.Item1, f.Item2, ActionForType[typeof(string)])).ToArray();
            FmuInstance = new FmuInstance(() => FmuModel.CreateCoSimulationInstance("Simulation", callback), FmuWrittenParameters, clock);
        }

        private Action<object, IFmuIO, IVariable> CreateAction(Urn urn)
        {
            var propertyType = urn.GetType().GenericTypeArguments[0];
            return ActionForType[propertyType];
        }

        private static readonly Dictionary<Type, Action<object, IFmuIO, IVariable>> ActionForType =
            new Dictionary<Type, Action<object, IFmuIO, IVariable>>
            {
                { typeof(string), ActionToActionObject<string>(FmuWriter.FromFilePath) },
                { typeof(Percentage), ActionToActionObject<IFloat>(FmuWriter.FromIFloat) },
                { typeof(PowerSupply), ActionToActionObject<PowerSupply>(FmuWriter.FromPower) },
                { typeof(ThreeWayValvePosition), ActionToActionObject<ThreeWayValvePosition>(FmuWriter.From3WaysValvePosition) }
            };

        private static Action<object, IFmuIO, IVariable> ActionToActionObject<T>(Action<T, IFmuIO, IVariable> func)
        {
            return (value, instance, variable) =>
            {
                func((T) value, instance, variable);
            };
        }

        public void Dispose()
        {
            FmuInstance.Dispose();
            FmuModel.Dispose();
        }
    }
}