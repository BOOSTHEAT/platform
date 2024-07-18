using System;
using System.Linq;
using System.Text.Json;
using ImpliciX.Language.Core;
using static System.IO.File;
using static System.Text.Json.JsonSerializer;
using static ImpliciX.Language.Core.SideEffect;
using static ImpliciX.Data.Errors;

namespace ImpliciX.Data
{
    public class Manifest
    {
        public string Device { get; set; }
        public string Revision { get; set; }
        public DateTime Date { get; set; }
        public string SHA256 { get; set; }

        public ContentData Content { get; set; }

        public PartData[] AllPartsManifests() =>
            Content.MCU
                .Concat(Content.APPS)
                .Concat(Content.BSP)
                .ToArray();
        
        public class ContentData
        {
            public ContentData()
            {
                MCU = Array.Empty<PartData>();
                APPS = Array.Empty<PartData>();
                BSP = Array.Empty<PartData>();
            }
            public PartData[] MCU { get; set; }
            public PartData[] APPS { get; set; }
        
            public PartData[] BSP { get; set; }
        }

        public struct PartData
        {
            public string Target { get; set; }
            public string Revision { get; set; }
            public string FileName { get; set; }
        }

        public string PackageContentFileName() => $"{Revision}.zip";
        
        public static Result<Manifest> FromFile(string manifestFile) =>
            TryRun(
                () => Deserialize<Manifest>(ReadAllText(manifestFile), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                , DeserializeManifestError
            );
        
        public Result<string> ToFile(string manifestFilePath) =>
            TryRun(() =>
            {
                WriteAllText(manifestFilePath, Serialize(this));
                return manifestFilePath;
            }, SerializeManifestError);

        public string ToJsonText() => Serialize(this);
    }
}