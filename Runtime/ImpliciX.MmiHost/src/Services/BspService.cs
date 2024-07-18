#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.MmiHost.DBusProxies;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using static ImpliciX.MmiHost.Constants;
using static ImpliciX.MmiHost.Services.Errors;
using static ImpliciX.Language.Core.SideEffect;

namespace ImpliciX.MmiHost.Services
{
    public class BspService
    {
        private readonly MmiHostModuleDefinition _moduleDefinition;
        private readonly IDomainEventFactory _df;
        private readonly IRaucInstallProxy _raucInstaller;
        public InstallState CurrentState { get; private set; }
        
/*
        private readonly int _installPct;
*/
        private readonly PropertyUrn<Percentage> _progressUrn;
        private static string? _forceROPath;

        public BspService(MmiHostModuleDefinition moduleDefinition, IDomainEventFactory df, IRaucInstallProxy raucInstaller,
            InstallState currentState = InstallState.NotStarted, string? forceRoPath=null)
        {
            _moduleDefinition = moduleDefinition;
            _df = df;
            _raucInstaller = raucInstaller;
            CurrentState = currentState;
            _progressUrn = _moduleDefinition.BspSoftwareDeviceNode.update_progress;
            _forceROPath = forceRoPath ?? "/sys/block/mmcblk0boot0/force_ro";
        }


        public DomainEvent[] Handle(DomainEvent @event) =>
            @event switch
            {
                CommandRequested cr => HandleCommandRequested(cr),
                SystemTicked st => HandleSystemTicked(st),
                _ => Array.Empty<DomainEvent>()
            };

        private DomainEvent[] HandleSystemTicked(SystemTicked _)
        {
            Func<(int pct, string message, int level)> progressFun = () => 
                _raucInstaller.GetAsync<(int pct, string message, int level)>("Progress").GetAwaiter().GetResult();
            
            Func<string> operationFun = () => _raucInstaller.GetAsync<string>("Operation").GetAwaiter().GetResult();
            
            var (pct, message, level) = progressFun();
            var operation = operationFun();


            CurrentState = NextState(CurrentState, operation, pct);
            Log.Debug("[MmiHost] Bsp update progress {@0}", pct);
            Log.Debug("[MmiHost] Bsp update - State = {@0}", CurrentState);

            if (CurrentState == InstallState.Done)
            {
                Force_Ro(true);
            }
            
            return (_installState: CurrentState, pct) switch
            {
                (_, var p) when p < 100 => new []{_df.NewEventResult(_progressUrn, Percentage.FromFloat(pct / 100f).Value).GetValueOrDefault()},
                (InstallState.Done, 100) => new []{_df.NewEventResult(_progressUrn, Percentage.FromFloat(1f).Value).GetValueOrDefault()},
                (_, _) => new DomainEvent[]{}
            };

        }

        private static InstallState NextState(InstallState prevState, string operation, int pct)
        {
            return (prevState, operation, pct) switch
            {
                (InstallState.Started, _, var p) when p >= 0 => InstallState.InProgress, 
                (InstallState.InProgress, _, var p) when p < 100 => InstallState.InProgress,
                (InstallState.InProgress, _, 100) => InstallState.Full,
                (InstallState.Full, "idle",_) => InstallState.Done,
                (InstallState.Full, _, _) => InstallState.Full,
                _ => throw new ApplicationException("can't compute the state of BSP installation process")
            };
        }

        private DomainEvent[] HandleCommandRequested(CommandRequested @event)
        {
            return 
                (
                    from packageContent in SafeCast<PackageContent>(@event.Arg)
                    from _ in Force_Ro(false)
                    select StartRaucUpdate(_raucInstaller, packageContent))
                .UnWrap()
                .Match(_ => Array.Empty<DomainEvent>(),
                    _ => Array.Empty<DomainEvent>());
        }

        public bool CanHandle(DomainEvent @event)
        {
            return @event switch
            {
                SystemTicked _ => CurrentState!=InstallState.NotStarted,
                CommandRequested cr => cr.Urn.Equals(_moduleDefinition.BspSoftwareDeviceNode._update.command),
                _ => false
            };
        }

        public Result<PropertiesChanged> BspVersions()
        {
            var current_version = Environment.GetEnvironmentVariable(ENV_VARIABLE_CURRENT_SLOT_BSP_VERSION);
            if (string.IsNullOrWhiteSpace(current_version)) return BspVersionNotFound(ENV_VARIABLE_CURRENT_SLOT_BSP_VERSION);

            var fallback_version = Environment.GetEnvironmentVariable(ENV_VARIABLE_OTHER_SLOT_BSP_VERSION);
            if (string.IsNullOrWhiteSpace(fallback_version)) return BspVersionNotFound(ENV_VARIABLE_CURRENT_SLOT_BSP_VERSION);
            return 
                from cv in SoftwareVersion.FromString(current_version)
                from fv in SoftwareVersion.FromString(fallback_version)
                from evt in _df.NewEventResult(new (Urn urn, object value)[]
                {
                    (_moduleDefinition.BspSoftwareDeviceNode.software_version.measure, cv),
                    (_moduleDefinition.BspSoftwareDeviceNode.fallback_version.measure, fv),
                }) 
                select evt ;
        }

        private Result<Unit> StartRaucUpdate(IRaucInstallProxy proxy, PackageContent bspContent)
        {
            Log.Debug("[MmiHost] Start Install bundle {@0}", bspContent.ContentFile?.FullName);
            
            Task.Run(async () =>
            {
                await proxy.InstallBundleAsync(bspContent.ContentFile!.FullName, new Dictionary<string, object>());
            });
            CurrentState = InstallState.Started;
            return default(Unit);
        }

        private static Result<Unit> Force_Ro(bool flag)
        {
            return TryRun(() =>
            {
                File.WriteAllText(_forceROPath, Convert.ToUInt16(flag).ToString());
                return default(Unit);
            }, UpdateBspError);
        }

        public enum InstallState
        {
            NotStarted,
            Started,
            InProgress,
            Done,
            Full
        }

    }


    
}