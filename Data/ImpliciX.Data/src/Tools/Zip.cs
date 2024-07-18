using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ImpliciX.Data.Tools;
using ImpliciX.Language.Core;
using Mono.Unix.Native;
using static ImpliciX.Language.Core.SideEffect;

namespace ImpliciX.Data;

public static class Zip
{
  public static Result<Unit> ExtractToDirectory(string zipFile, string folder)
  {
    return TryRun(() =>
    {
      if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);

      using var archive = ZipFile.OpenRead(zipFile);
      foreach (var entry in archive.Entries)
      {
        var entryFullNameCleaned = entry.FullName.StartsWith(Path.DirectorySeparatorChar)
          ? entry.FullName[1..]
          : entry.FullName;
        var destinationFullPath = Path.GetFullPath(Path.Combine(Path.GetFullPath(folder), entryFullNameCleaned));
        var destinationDirectoryName = Path.GetDirectoryName(destinationFullPath);
        if (destinationDirectoryName != null) Directory.CreateDirectory(destinationDirectoryName);
        if (IsDirectory(entry)) continue;
        entry.ExtractToFile(destinationFullPath, true);
        SetPermissionsFromArchive(entry, destinationFullPath);
      }

      return default(Unit);
    }, ex => new ZipError(ex.CascadeMessage()));
  }

  public static Result<Unit> ExtractToDirectory(IStoreFile zipFile, IStoreFolder folder)
  {
    return TryRun(() =>
    {
      var Res = IStoreFolder.Combine(folder.GetParentAsync().Result, folder.Name);
      using var archive = new ZipArchive(zipFile.OpenReadAsync().Result);
      foreach (var entry in archive.Entries)
      {
        var entryFullNameCleaned = entry.FullName.StartsWith(Path.DirectorySeparatorChar)
          ? entry.FullName[1..]
          : entry.FullName;
        if (IsDirectory(entry))
        {
          IStoreFolder.Combine(folder, entry.FullName);
          continue;
        }

        var destination = folder.CreateFileAsync(entry.FullName).Result;
        //entry.ExtractToFile(destinationFullPath, true);
        entry.Open().CopyTo(destination.OpenWriteAsync().Result);
        SetPermissionsFromArchive(entry, destination.Path.LocalPath);
      }

      return default(Unit);
    }, ex => new ZipError(ex.CascadeMessage()));
  }

  public static Result<string> CreateZipFromFiles(IEnumerable<string> inputFiles, string outputZipFilePath)
  {
    return CreateZip(inputFiles.Select(c => (c, Path.GetFileName(c))), outputZipFilePath);
  }

  public static Result<string> CreateZip(IEnumerable<(string, string)> inputPaths, string outputZipFilePath)
  {
    return TryRun(() =>
    {
      using var zipArchive =
        new ZipArchive(new FileStream(outputZipFilePath, FileMode.Create), ZipArchiveMode.Create);
      foreach (var (inputPath, destPath) in inputPaths)
      {
        Log.Verbose($"Adding {inputPath} to {destPath} in {outputZipFilePath}");
        if (File.Exists(inputPath))
        {
          var entry = zipArchive.CreateEntry(Path.GetFileName(destPath));
          SetPermissionsToArchive(entry, inputPath);
          using var entryStream = entry.Open();
          using var fileStream = File.OpenRead(inputPath);
          fileStream.CopyTo(entryStream);
        }
        else if (IsDirectory(inputPath))
        {
          var di = new DirectoryInfo(inputPath);
          foreach (var file in di.EnumerateFiles("*", SearchOption.AllDirectories))
          {
            var relativePath = file.FullName;
            relativePath = relativePath.Replace(di.FullName, "");
            relativePath = Path.Combine(destPath, relativePath);
            var entry = zipArchive.CreateEntry(relativePath);
            SetPermissionsToArchive(entry, file.FullName);
            using var entryStream = entry.Open();
            using var fileStream = file.OpenRead();
            fileStream.CopyTo(entryStream);
          }
        }
        else
        {
          throw new FileNotFoundException("The specified path does not exist", inputPath);
        }
      }

      return outputZipFilePath;
    }, ex => new ZipError(ex.CascadeMessage()));
  }


  private static void SetPermissionsFromArchive(ZipArchiveEntry entry, string destinationFullPath)
  {
    if (Environment.OSVersion.Platform != PlatformID.Unix) return;
    const int extractPermissionMask = 0x1FF;
    var permissions = entry.ExternalAttributes >> 16 & extractPermissionMask;
    Syscall.chmod(destinationFullPath, (FilePermissions)permissions);
  }

  private static void SetPermissionsToArchive(ZipArchiveEntry entry, string sourcePath)
  {
    if (Environment.OSVersion.Platform == PlatformID.Unix)
      entry.ExternalAttributes = (int)Fs.GetFilePermissions(sourcePath) << 16;
  }

  private static bool IsDirectory(ZipArchiveEntry entry) => entry.FullName.EndsWith("/");

  private static bool IsDirectory(string path) => new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory);
}

public class ZipError : Error
{
  public ZipError(string message) : base(nameof(ZipError), message)
  {
  }
}
