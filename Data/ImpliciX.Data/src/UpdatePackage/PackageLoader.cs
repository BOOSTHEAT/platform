using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using static System.IO.File;
using static ImpliciX.Data.Errors;
using static ImpliciX.Language.Core.SideEffect;

namespace ImpliciX.Data;

public delegate Result<Package> Loader(PackageLocation location, Func<string, SoftwareDeviceNode> softwareDeviceMap);

public static class PackageLoader
{
  private const string ManifestSearchPattern = "manifest.json";
  public static Result<Package> Load(PackageLocation location, Func<string, SoftwareDeviceNode> softwareDeviceMap) =>
    from tmpCopy in MakeLocalTempCopy(location)
    from packageFiles in Decompress(tmpCopy, "")
    from manifestFile in GetManifestFile(packageFiles)
    from manifest in Manifest.FromFile(manifestFile)
    from packageContent in ExtractPackageContent(packageFiles, manifest)
    select new Package(manifest, packageContent.ToArray(), softwareDeviceMap);

  private static Result<string> GetManifestFile(IEnumerable<FileInfo> files) => TryRun(() =>
  {
    var manifestFile = files.Single(f => f.Name.EndsWith(ManifestSearchPattern)).FullName;
    return manifestFile;
  }, NotFoundManifestError);

  private static Result<IEnumerable<FileInfo>> ExtractPackageContent(IEnumerable<FileInfo> packageFiles,
    Manifest manifest) =>
    (
      from contentArchiveFile in FindContentArchiveFile(packageFiles, manifest)
      from _ in ValidateSha256(contentArchiveFile, manifest)
      select Decompress(contentArchiveFile, "content")
    )
    .UnWrap();

  private static Result<FileInfo> FindContentArchiveFile(IEnumerable<FileInfo> packageFiles, Manifest manifest) =>
    TryRun(
      () => packageFiles.First(f => f.Name.Equals(manifest.PackageContentFileName())),
      () => PackageContentArchiveNotFound(manifest.PackageContentFileName())
    );

  private static Result<Unit> ValidateSha256(FileInfo contentArchiveFile, Manifest manifest) =>
    TryRun(() =>
    {
      var hashStr = Sha256.OfFile(contentArchiveFile.FullName);
      if (hashStr != manifest.SHA256)
        throw new InvalidSha256Exception(hashStr, manifest.SHA256);

      return default(Unit);
    }, CorruptionError);

  public static Result<FileInfo> MakeLocalTempCopy(PackageLocation location)
  {
    return TryRun(() =>
    {
      return location.Value.Scheme switch
      {
        var s when s.Equals("file", StringComparison.OrdinalIgnoreCase) =>
          CopyFromLocal(location),
        _ => throw new NotSupportedException()
      };
    }, () => TmpCopyError(location.Value));
  }

  private static FileInfo CopyFromLocal(PackageLocation location)
  {
    var filePath = Path.Combine("./",
      location.Value.LocalPath.TrimStart('\\').Replace('\\', Path.DirectorySeparatorChar));
    var tmpFolder = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(location.Value.AbsolutePath));
    var tmpFile = Path.Combine(tmpFolder, Path.GetRandomFileName());

    if (Directory.Exists(tmpFolder))
      Directory.Delete(tmpFolder, true);

    Directory.CreateDirectory(tmpFolder);
    Copy(filePath, tmpFile, true);
    return new FileInfo(tmpFile);
  }
  private static Result<IEnumerable<FileInfo>> Decompress(FileInfo archive, string destination)
  {
    return TryRun(() =>
    {
      var destinationDirectory = new DirectoryInfo(Path.Combine(archive.DirectoryName!, destination));
      if (!destinationDirectory.Exists) destinationDirectory.Create();
      Zip.ExtractToDirectory(archive.FullName, destinationDirectory.FullName);
      Delete(archive.FullName);
      return destinationDirectory.EnumerateFiles();
    }, e => DecompressFileError(archive.FullName, e));
  }
}
