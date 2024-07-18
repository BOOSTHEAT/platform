using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ImpliciX.Data;

namespace ImpliciX.Linker;

public static class Compression
{
  public static GZipOutputStream CreateGz(string gzFilename)
  {
    var outStream = File.Create(gzFilename);
    var gzoStream = new GZipOutputStream(outStream);
    gzoStream.SetLevel(9);
    return gzoStream;
  }

  public static void MakeTar(this Stream oStream, string sourceDirectory)
  {
    using var outputStream = new TarOutputStream(oStream, null);
    using var archive = TarArchive.CreateOutputTarArchive(outputStream);
    archive.RootPath = sourceDirectory.Substring(1);
    AddToTar((archive, outputStream), sourceDirectory);
    archive.Close();
  }
  
  private static void AddToTar((TarArchive, TarOutputStream) tar, string fsItem)
  {
    var fileInfo = new FileInfo(fsItem);
    if (fileInfo.LinkTarget == null)
      CreateFolderOrFileInTar(tar, fileInfo);
    else
      CreateSymLinkInTar(tar, fileInfo);
  }

  private static void CreateFolderOrFileInTar((TarArchive, TarOutputStream) tar, FileInfo fileInfo)
  {
    TarEntry tarEntry = TarEntry.CreateEntryFromFile(fileInfo.FullName);
    ConfigureEntry(tarEntry);
    const int extractPermissionMask = 0x1FF;
    int permissions = (int) Fs.GetFilePermissions(fileInfo.FullName) & extractPermissionMask;
    tarEntry.TarHeader.Mode = permissions;
    tar.Item1.WriteEntry(tarEntry, false);

    if (Directory.Exists(fileInfo.FullName))
    {
      var children = Directory.GetFileSystemEntries(fileInfo.FullName);
      foreach (var child in children)
        AddToTar(tar, child);
    }
  }

  private static void CreateSymLinkInTar((TarArchive, TarOutputStream) tar, FileInfo fileInfo)
  {
    var tarEntry = TarEntry.CreateTarEntry(fileInfo.FullName.Substring(tar.Item1.RootPath.Length + 2));
    tarEntry.TarHeader.TypeFlag = TarHeader.LF_SYMLINK;
    tarEntry.TarHeader.LinkName = fileInfo.LinkTarget;
    ConfigureEntry(tarEntry);
    tar.Item2.PutNextEntry(tarEntry);
    tar.Item2.CloseEntry();
  }

  private static void ConfigureEntry(TarEntry tarEntry)
  {
    var header = tarEntry.TarHeader;
    header.UserName = "root";
    header.UserId = 0;
    header.GroupName = "root";
    header.GroupId = 0;
  }
}