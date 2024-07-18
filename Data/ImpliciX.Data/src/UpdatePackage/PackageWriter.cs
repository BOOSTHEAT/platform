using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.Core;
using static ImpliciX.Language.Core.SideEffect;

namespace ImpliciX.Data;

public static class PackageWriter
{
  public static Result<string> Write(Manifest manifest, IEnumerable<string> contentFiles, string output,
    IEnumerable<(string, string)> bindedFolders) =>
    from outputFolder in CreateTempOutputFolder()
    from contentZipFile in Zip.CreateZipFromFiles(contentFiles,
      Path.Combine(outputFolder, $"{manifest.Revision}.zip"))
    from manifestWithSha in StampManifestWithSha256(manifest, contentZipFile)
    from manifestFile in manifestWithSha.ToFile(Path.Combine(outputFolder, "manifest.json"))
    from packageFile in Zip.CreateZip(
      new List<(string, string)>
        {
          (contentZipFile, Path.GetFileName(contentZipFile)), (manifestFile, Path.GetFileName(manifestFile))
        }
        .Concat(bindedFolders), output)
    select packageFile;

  private static Result<string> CreateTempOutputFolder()
  {
    return TryRun(() =>
    {
      var outputFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      Directory.CreateDirectory(outputFolder);
      return outputFolder;
    }, ex => new CreateOutputFolderError(ex.CascadeMessage()));
  }

  private static Result<Manifest> StampManifestWithSha256(Manifest manifest, string contentPackageFileName)
  {
    return TryRun(() =>
    {
      manifest.SHA256 = Sha256.OfFile(contentPackageFileName);
      return manifest;
    }, ex => new ComputeSha256Error(ex.CascadeMessage()));
  }

  public class ComputeSha256Error : Error
  {
    public ComputeSha256Error(string message) : base(nameof(ComputeSha256Error), message)
    {
    }
  }

  public class CreateOutputFolderError : Error
  {
    public CreateOutputFolderError(string message) : base(nameof(CreateOutputFolderError), message)
    {
    }
  }
}
