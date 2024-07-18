using System;
using System.Collections.Generic;
using System.IO;
using ImpliciX.Data;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Factory;

namespace ImpliciX.SystemSoftware.Tests.States
{
    public class StatesHelper
    {


        public static SystemSoftwareModuleDefinition Model(bool isPackageAllowed = true) => new SystemSoftwareModuleDefinition()
        {
            IsPackageAllowedForUpdate = _ => isPackageAllowed,
            GeneralUpdateCommand = dummy._update,
            ReleaseVersion = dummy.release_version,
            CleanVersionSettings = dummy._clean_version_settings,
            CommitUpdateCommand = dummy._commit,
            RebootCommand = dummy._reboot,
            UpdateState = dummy.update_state,
            SoftwareMap = new Dictionary<string, SoftwareDeviceNode>()
            {
                ["app1"] = dummy.app1,
                ["bsp"] = dummy.bsp,
                ["mcu1"] = dummy.mcu1,
                ["mcu2"] = dummy.mcu2,
                ["mcu3"] = dummy.mcu3,
            },
            
            
        };
        
        public static IDomainEventFactory DomainEventFactory()
        {
            var modelFactory = new ModelFactory(new[] { typeof(dummy).Assembly });
            return new DomainEventFactory(modelFactory, () => TimeSpan.Zero);
        }
        
        public static Context CreateContext()
        {
            var context = new Context(Model().SoftwareMap)
            {
                SupportedForUpdate = new HashSet<SoftwareDeviceNode>()
                {
                    dummy.mcu1,
                    dummy.mcu2,
                    dummy.app1,
                    dummy.bsp,
                },
                UpdateManifestPath = UpdateManifestFilePath,
                FallbackReleaseManifestPath = "package_examples/fallback_release_dummy_manifest.json",
                CurrentReleaseManifestPath = "package_examples/current_release_dummy_manifest.json"
            };
 
            return context;
        }
        
        public static readonly Loader WithMcuOnlyLoader = (location, softwareDeviceMap) => new Package(
            new Manifest()
            {
                Device = "device",
                SHA256 = "42",
                Revision = "revision",
                Date = DateTime.Now,
                Content = new Manifest.ContentData()
                {
                    MCU = new []
                    {
                        new Manifest.PartData()
                        {
                            Revision = "1.2.3.4",
                            Target = "mcu1",
                            FileName = "mcu1.bin",
                        },
                        new Manifest.PartData()
                        {
                            Revision = "1.2.3.4",
                            Target = "mcu2",
                            FileName = "mcu2.bin",
                        },
                        new Manifest.PartData()
                        {
                            Revision = "1.2.3.4",
                            Target = "mcu3",
                            FileName = "mcu3.bin",
                        }
                    },
                    APPS = Array.Empty<Manifest.PartData>(),
                    BSP = Array.Empty<Manifest.PartData>()
                }
            },
            new FileInfo[]
            {
                new FileInfo("/some/path/mcu1.bin"),
                new FileInfo("/some/path/mcu2.bin"),
                new FileInfo("/some/path/mcu3.bin"),
            },
            k => new Dictionary<string, SoftwareDeviceNode>()
            {
                ["mcu1"] = dummy.mcu1,
                ["mcu2"] = dummy.mcu2,
                ["mcu3"] = dummy.mcu3,
            }[k]);

        public static Loader FullLoader(string appVersion = null, string bspVersion = null, string firmwareVersion = null) => (location, softwareDeviceMap) => new Package(
            new Manifest()
            {
                Device = "device",
                SHA256 = "42",
                Revision = "revision",
                Date = DateTime.Now,
                Content = new Manifest.ContentData()
                {
                    MCU = new []
                    {
                        new Manifest.PartData()
                        {
                            Revision = firmwareVersion ?? "1.2.3.4",
                            Target = "mcu1",
                            FileName = "mcu1.bin",
                        },
                        new Manifest.PartData()
                        {
                            Revision = firmwareVersion ?? "1.2.3.4",
                            Target = "mcu2",
                            FileName = "mcu2.bin",
                        },
                        new Manifest.PartData()
                        {
                            Revision = firmwareVersion ?? "1.2.3.4",
                            Target = "mcu3",
                            FileName = "mcu3.bin",
                        }
                    },
                    APPS = new []
                    {
                        new Manifest.PartData()
                        {
                            Revision = appVersion ?? "1.2.3.4",
                            Target = "app1",
                            FileName = "app1.zip"
                        }
                    },
                    BSP = new Manifest.PartData[]
                    {
                        new Manifest.PartData()
                        {
                            Revision = bspVersion ?? "1.2.3.4",
                            Target = "bsp1",
                            FileName = "bsp1.raucb"
                        }
                    }
                }
            },
            new FileInfo[]
            {
                new FileInfo("/some/path/mcu1.bin"),
                new FileInfo("/some/path/mcu2.bin"),
                new FileInfo("/some/path/mcu3.bin"),
                new FileInfo("/some/path/app1.zip"),
                new FileInfo("/some/path/bsp1.raucb"),
            },
            k => new Dictionary<string, SoftwareDeviceNode>()
            {
                ["mcu1"] = dummy.mcu1,
                ["mcu2"] = dummy.mcu2,
                ["mcu3"] = dummy.mcu3,
                ["bsp1"] = dummy.bsp,
                ["app1"] = dummy.app1
            }[k]);

        
        public const string UpdateManifestFilePath = "./new_manifest.json";
        public static readonly CommandNode<PackageLocation> GeneralUpdateCommand = dummy._update;
        public static readonly PackageLocation PackageLocation = PackageLocation.FromString("file://dummy/mydummy.zip").Value;
    }
    
    
}