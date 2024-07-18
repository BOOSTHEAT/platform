using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;


namespace ImpliciX.Data
{
    public class Package
    {
        private readonly Manifest _manifest;
        
        public Package(Manifest manifest, FileInfo[] packageContent, Func<string, SoftwareDeviceNode> softwareDeviceMap)
        {
            _manifest = manifest;
            ApplicationName = manifest.Device;
            Revision = manifest.Revision;
            Date = manifest.Date;
            System.Diagnostics.Debug.Assert(softwareDeviceMap != null, nameof(softwareDeviceMap) + " != null");
            SoftwareDeviceMap = softwareDeviceMap;
            _contents = new Dictionary<SoftwareDeviceNode, PackageContent>();
            foreach (var partManifest in manifest.AllPartsManifests())
            {
                var device = TargetToSoftwareDevice(partManifest.Target);
                var firmwareFile = packageContent.Single(fi => fi.Name == partManifest.FileName);
                var content = new PackageContent(device, partManifest.Revision, firmwareFile);
                _contents[device] = content;
            }
        }
        
        public Func<string,SoftwareDeviceNode> SoftwareDeviceMap { get; }

        private SoftwareDeviceNode TargetToSoftwareDevice(string target)
        {
            var device = SoftwareDeviceMap(target);
            Debug.PreCondition(()=>device != null, ()=>$"{target} not supported");
            return device;
        }
      
        public string ApplicationName { get; }
        public string Revision { get; }
        public DateTime Date { get; }
        public Dictionary<SoftwareDeviceNode, PackageContent> _contents { get;  }

        public SoftwareDeviceNode[] SoftwareDevices => _contents.Keys.ToArray();

        public Option<PackageContent> this[SoftwareDeviceNode firmwareUrn] => 
            _contents.GetValueOrDefault(firmwareUrn).ToOption();

        public Result<string> CopyManifest(string filePath) => _manifest.ToFile(filePath);
    }
}