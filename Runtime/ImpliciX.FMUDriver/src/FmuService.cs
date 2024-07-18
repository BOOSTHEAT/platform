using System;
using System.Collections.Generic;
using System.Linq;
using Femyou;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Scheduling;

namespace ImpliciX.FmuDriver
{
    public static class FmuService
    {
        public static DomainEventHandler<Idle> ReadState(
            DriverFmuModuleDefinition moduleDefinition, ModelFactory modelFactory, IFmuInstance fmuInstance,
            IEnumerable<IVariable> fmuVariables, IClock clock, FmuDriverSettings settings)
        {
            return trigger =>
            {
                return SideEffect.TryRun(() =>
                    {
                        fmuInstance.AdvanceTime(settings.SimulationTimeStep);

                        var values = fmuInstance.Read(fmuVariables);

                        var dataModelValues = moduleDefinition.ReadVariables
                            .Zip(values, (first, second) => (First: first, Second: second))
                            .SelectMany(zippedValues =>
                            {
                                var ((measure, status, _), value) = zippedValues;
                                var result = new[]
                                    { (IDataModelValue) modelFactory.CreateWithLog(measure, Convert.ToSingle(value), clock.Now()).GetValueOrDefault() };
                                return status != string.Empty
                                    ? result.Append(
                                        (IDataModelValue) modelFactory.CreateWithLog(status, MeasureStatus.Success, clock.Now()).GetValueOrDefault())
                                    : result;
                            });

                        var events = new DomainEvent[] { PropertiesChanged.Create(dataModelValues, clock.Now()) };

                        return events;
                    },
                    FmuCommunicationError.Create).GetValueOrDefault();
            };
        }

        public static DomainEventHandler<CommandRequested> FmuCommandHandler(DriverFmuModuleDefinition moduleDefinition, ModelFactory modelFactory,
            FmuContext fmuContext, IClock clockAdapter)
        {
            return trigger => trigger switch
            {
                _ when IsDriverCommand(moduleDefinition, trigger) => SendFmuDriverCommand(
                    modelFactory, fmuContext, fmuContext.FmuInstance, fmuContext.FmuWrittenVariables, clockAdapter)(trigger),
                _ when IsFmuConfig(moduleDefinition, trigger) => SendFmuCommand(moduleDefinition, fmuContext, fmuContext.FmuInstance)(trigger),
                _ => throw new NotSupportedException("Cannot handle command in FMU")
            };
        }

        public static DomainEventHandler<CommandRequested> SendFmuDriverCommand(
            ModelFactory modelFactory, FmuContext fmuContext, IFmuIO fmuIO, IVariable[] writtenVariables, IClock clockAdapter)
        {
            return trigger =>
            {
                return SideEffect.TryRun(() =>
                {
                    var (_, key, writeFunc) = fmuContext.FmuWrittenVariablesMap.First(t => t.urn.Equals(trigger.Urn));
                    var variable = writtenVariables.First(v => v.Name == key);

                    writeFunc(trigger.Arg, fmuIO, variable);

                    var result = new[]
                    {
                        (IDataModelValue) modelFactory.CreateWithLog(Urn.BuildUrn(trigger.Urn, "measure"), trigger.Arg, clockAdapter.Now())
                            .GetValueOrDefault()
                    };
                    var events = new DomainEvent[] { PropertiesChanged.Create(result, clockAdapter.Now()) };
                    return events;
                }, FmuCommunicationError.Create).GetValueOrDefault();
            };
        }

        public static DomainEventHandler<CommandRequested> SendFmuCommand(DriverFmuModuleDefinition moduleDefinition, FmuContext fmuContext,
            IFmuSimulation fmuSimulation)
        {
            return trigger =>
            {
                return SideEffect.TryRun(() =>
                {
                    if (trigger.Urn.Equals(moduleDefinition.StartSimulation))
                    {
                        fmuSimulation.StartSimulation();
                    }
                    else if (trigger.Urn.Equals(moduleDefinition.StopSimulation))
                    {
                        fmuSimulation.StopSimulation();
                        fmuSimulation.CreateNewSimulation(fmuContext);
                    }
                    else throw new ContractException($"Cannot handle FMU configuration for trigger {trigger.Urn}");

                    return Array.Empty<DomainEvent>();
                }, FmuCommunicationError.Create).GetValueOrDefault();
            };
        }

        public static Func<Idle, bool> CanHandleQuery(IFmuSimulation fmuSimulation) =>
            _ => fmuSimulation.FmuStarted;

        public static Func<CommandRequested, bool> CanExecuteCommand(DriverFmuModuleDefinition moduleDefinition) =>
            cr => IsFmuConfig(moduleDefinition, cr) || IsDriverCommand(moduleDefinition, cr);

        private static bool IsFmuConfig(DriverFmuModuleDefinition moduleDefinition, CommandRequested command)
            => new[] { moduleDefinition.StartSimulation, moduleDefinition.StopSimulation }.Any(c => c.Equals(command.Urn));

        private static bool IsDriverCommand(DriverFmuModuleDefinition moduleDefinition, CommandRequested cr)
        {
            return moduleDefinition.WriteVariables.Any(tuple =>
            {
                var (urn, _) = tuple;
                return urn.Equals(cr.Urn);
            });
        }
    }

    public class FmuCommunicationError : Error
    {
        public static FmuCommunicationError Create()
        {
            return new FmuCommunicationError();
        }

        private FmuCommunicationError() : base(nameof(FmuCommunicationError), "Error occured while communicating with the MCU")
        {
        }
    }
}