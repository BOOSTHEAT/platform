using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BrahmaBoard;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{

  public class FakeBoard : IBrahmaBoardSlave, IBoardSlave
    {
        public FakeBoard(DeviceNode deviceNode, BurnerNode genericBurner = null)
        {
            DeviceNode = deviceNode;
            SettingsUrns = new Urn[] { };
            GenericBurner = genericBurner;
        }
        
        
        public uint ReadPaceInSystemTicks { get; set; }
        
        public DeviceNode DeviceNode { get; set; }
        
        public List<IExecuteCommandSimulation> CommandExecutionSimulations { get; set; }
        
        public ReadAndDecodeSimulatedResults ReadAndDecodeMainFirmwareSimulation { get; set; }
        
        public ReadAndDecodeSimulatedResults ReadAndDecodeBootloaderSimulation { get; set; }
        
        public ReadAndDecodeSimulatedResults ReadAndDecodeCommonSimulation { get; set; }


        public Result2<IDataModelValue[], CommunicationDetails> ReadProperties(MapKind mapKind)
        {
            return mapKind switch {
                MapKind.Bootloader => ReadAndDecodeBootloaderSimulation.Simulate(),
                MapKind.MainFirmware => ReadAndDecodeMainFirmwareSimulation.Simulate(),
                MapKind.Common => ReadAndDecodeCommonSimulation.Simulate(),
                _ => throw new ArgumentOutOfRangeException(nameof(mapKind), mapKind, null)
            };
        }
        
        public Result2<IDataModelValue[],CommunicationDetails> ExecuteCommand(Urn commandUrn, object arg) =>
            CommandExecutionSimulations
                .Single(def => def.CommandUrn.Equals(commandUrn))
                .Simulate(arg);

        public bool IsConcernedByCommandRequested(Urn crUrn)
        {
            return CommandExecutionSimulations.Any(def => def.CommandUrn.Equals(crUrn));
        }


        public string Name => $"{nameof(FakeBoard)}_{nameof(Name)}";
        public Urn[] SettingsUrns { get; set; }
        public BurnerNode GenericBurner { get; }
    } 
    
}