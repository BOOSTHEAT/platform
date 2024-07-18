using System;
using System.IO;
using ImpliciX.Data;
using ImpliciX.Language.Core;


namespace ImpliciX.MmiHost.Services
{
    public class SoftwareUpdate
    {
        public SoftwareUpdate(string softwareName, string revision, FileInfo content, string systemSoftwarePath,
            string bootFsPath, string targetedPartition)
        {
            SoftwareName = softwareName;
            SystemSoftwarePath = systemSoftwarePath;
            BootFsPath = bootFsPath;
            TargetedPartition = targetedPartition;
            Content = content;
            Revision = revision;
        }

        public string SoftwareName { get; set; }
        public string SystemSoftwarePath { get; set; }
        public string BootFsPath { get; set; }
        public string TargetedPartition { get; set; }
        public FileInfo Content { get; set; }
        public string Revision { get; set; }

        public string SoftwarePath() =>
            Path.Combine(SystemSoftwarePath, SoftwareName);
    }
    public static class AppsUpdateService
    {
        public static Unit LaunchUpdate(SoftwareUpdate swUpdate)
        {
            var softwarePath = new DirectoryInfo(swUpdate.SoftwarePath());
            UpdateAppFile(swUpdate.Content, softwarePath, swUpdate.Revision);
            UpdateSymlink(swUpdate.BootFsPath, swUpdate.TargetedPartition, swUpdate.SoftwareName, softwarePath, swUpdate.Revision);
            return default;
        }

        private static void UpdateSymlink(string bootFsPath, string oppositePartition, string softwareName, DirectoryInfo softwarePath,
            string revision)
        {
            var bootsFsPathToUpdate = Path.Combine(bootFsPath, oppositePartition);
            var targetedSymlink = new DirectoryInfo(Path.Combine(bootsFsPathToUpdate, softwareName));
            if (targetedSymlink.Exists) targetedSymlink.Delete();
            var appFileDestinationPath = new FileInfo(Path.Combine(softwarePath.FullName, revision));
            Fs.TryCreateSymbolicLink(appFileDestinationPath.FullName, targetedSymlink.FullName);
        }

        private static void UpdateAppFile(FileInfo appFile, FileSystemInfo softwarePath, string packageRevision)
        {
            SideEffect.TryRun(() =>
            {
                var isZip = appFile.Extension.ToLower() == ".zip";
                if (isZip)
                {
                    var zipDestinationPath = new FileInfo(Path.Combine(softwarePath.FullName, appFile.Name));
                    appFile.MoveTo(zipDestinationPath.FullName);
                    Decompress(zipDestinationPath, packageRevision);
                    zipDestinationPath.Delete();
                }
                else
                {
                    var appFileDestinationPath = new FileInfo(Path.Combine(softwarePath.FullName, packageRevision, appFile.Name));
                    if (appFileDestinationPath.Directory != null && appFileDestinationPath.Directory.Exists) appFileDestinationPath.Directory.Delete(true);
                    appFileDestinationPath.Directory?.Create();
                    appFile.MoveTo(appFileDestinationPath.FullName);
                }
            }, exception => Log.Error($"Error during updating {appFile.FullName}" + Environment.NewLine + exception));
        }

        private static void Decompress(FileInfo appFile, string folderName)
        {
            SideEffect.TryRun(() =>
            {
                var destinationPath = new DirectoryInfo(Path.Combine(appFile.DirectoryName!, folderName));
                if (destinationPath.Exists) destinationPath.Delete(true);
                Zip.ExtractToDirectory(appFile.FullName, destinationPath.FullName);
            }, exception => Log.Error($"Error during decompression of {appFile.FullName}" + Environment.NewLine + exception));
        }
    }
}