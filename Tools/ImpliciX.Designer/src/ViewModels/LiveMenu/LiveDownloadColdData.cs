using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImpliciX.Data;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;
using JetBrains.Annotations;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Data.Records.ColdRecords;

namespace ImpliciX.Designer.ViewModels.LiveMenu;

public class LiveDownloadColdData : ConditionalMenuViewModel<ITargetSystem>
{
    public static MenuItemViewModel Menu(ILightConcierge concierge) =>
        new MenuItemViewModel
        {
            Text = "Download",
            StaticItems = new []
            {
                XlMetrics(concierge, Sha256.OfFile, Zip.ExtractToDirectory, () => DateTime.Now),
                SqlMetrics(concierge, Sha256.OfFile, Zip.ExtractToDirectory, () => DateTime.Now),
                XlRecords(concierge, Sha256.OfFile, Zip.ExtractToDirectory, () => DateTime.Now),
            }
        };
    
    public const string XlFileExtension = ".xlsx";
    public const string SqlFileExtension = ".sqlite";

    
    public static LiveDownloadColdData XlMetrics([NotNull] ILightConcierge concierge,
        [NotNull] Func<string, string> computeFileChecksum,
        [NotNull] Func<string, string, Result<Unit>> unzipToDirectory, [NotNull] Func<DateTime> getNowDateTime)
    {
        return new(
            concierge,
            computeFileChecksum,
            unzipToDirectory,
            getNowDateTime,
            () => (concierge.RemoteDevice.CurrentTargetSystem.MetricsColdStorageDownload),
            () => (concierge.RemoteDevice.CurrentTargetSystem.MetricsColdStorageClear),
            concierge.Export.MetricsToExcel, 
            ColdMetricsDb.FileExtension,
            XlFileExtension,
            $"Metrics to MS Excel ({XlFileExtension})");
    }
    
    public static LiveDownloadColdData SqlMetrics([NotNull] ILightConcierge concierge,
        [NotNull] Func<string, string> computeFileChecksum,
        [NotNull] Func<string, string, Result<Unit>> unzipToDirectory, [NotNull] Func<DateTime> getNowDateTime)
    {
        return new(
            concierge,
            computeFileChecksum,
            unzipToDirectory,
            getNowDateTime,
            () => (concierge.RemoteDevice.CurrentTargetSystem.MetricsColdStorageDownload),
            () => (concierge.RemoteDevice.CurrentTargetSystem.MetricsColdStorageClear),
            concierge.Export.MetricsToSqlite, 
            ColdMetricsDb.FileExtension,
            SqlFileExtension,
            $"Metrics to SQLite ({SqlFileExtension})");
    }
    
    public static LiveDownloadColdData XlRecords([NotNull] ILightConcierge concierge,
        [NotNull] Func<string, string> computeFileChecksum,
        [NotNull] Func<string, string, Result<Unit>> unzipToDirectory, [NotNull] Func<DateTime> getNowDateTime)
    {
        return new(
            concierge,
            computeFileChecksum,
            unzipToDirectory,
            getNowDateTime,
            () => concierge.RemoteDevice.CurrentTargetSystem.RecordsColdStorageDownload,
            () =>concierge.RemoteDevice.CurrentTargetSystem.RecordsColdStorageClear,
            concierge.Export.RecordsToExcel, 
            ColdRecordsDb.FileExtension,
            XlFileExtension,
            $"Records to MS Excel ({XlFileExtension})");
    }
    
    public LiveDownloadColdData(
        [NotNull] ILightConcierge concierge,
        [NotNull] Func<string, string> computeFileChecksum,
        [NotNull] Func<string, string, Result<Unit>> unzipToDirectory,
        [NotNull] Func<DateTime> getNowDateTime,
        [NotNull] Func<ITargetSystemCapability> downloadCapability,
        [NotNull] Func<ITargetSystemCapability> clearCapability,
        [NotNull] Func<string, string, IAction> exporterFactory,
        [NotNull] string fileExtension,
        [NotNull] string outputFileExtension,
        [NotNull] string menuText
        ) 
        : base(
        concierge, menuText,
        concierge.RemoteDevice.TargetSystem,
        ts => concierge.RemoteDevice.CurrentTargetSystem !=null && downloadCapability().IsAvailable && clearCapability().IsAvailable,
        async (vm, ts) =>
        {
            var folder = await vm.Concierge.User.OpenFolder(new IUser.FileSelection
            {
                Title = "Select destination folder",
                Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            });

            if (folder.Choice != IUser.ChoiceType.Ok)
            {
                vm.Concierge.Console.WriteLine("Output folder selection: Canceled by user");
                return;
            }

            vm.Concierge.Console.WriteLine("Starting download data");

            var downloadedFiles = await DownloadData(folder.Path, downloadCapability(), progress => vm.Concierge.Console.WriteLine(progress));
            if (downloadedFiles.Length is 0)
            {
                vm.Concierge.Console.WriteLine("Download: No file downloaded");
                return;
            }

            var filesWithChecksumError = GetFilesWithChecksumError(downloadedFiles, computeFileChecksum);
            if (filesWithChecksumError.Length > 0)
            {
                var msg = filesWithChecksumError.Aggregate("Integrity check has failed for:", (current, info)
                    => current + $"{Environment.NewLine}{info.fileFullPath} : expected={info.expected}, computed={info.computed}");

                vm.Concierge.Console.WriteLine(msg);
                return;
            }

            vm.Concierge.Console.WriteLine($"Download of {downloadedFiles.Length} files completed");
            

            vm.Concierge.Console.WriteLine("Unzip files download in progress");
            var unzipError = UnzipLocalMetricsSourceFiles(
                folder.Path,
                vm.Concierge.FileSystemService,
                unzipToDirectory,
                vm.Concierge.RuntimeFlags.RemoveLocalColdStorageDownloaded,
                progress => vm.Concierge.Console.WriteLine(progress),
                $"*{fileExtension}.zip"
                );

            if (unzipError.IsSuccess)
                vm.Concierge.Console.WriteLine("Unzip files download completed");

            var now = getNowDateTime().ToString("yyyy-MM-dd");

            var outputFileName = $"{now}_{fileExtension[1..]}{outputFileExtension}";
            var outputFileFullPath = Path.Combine(folder.Path, outputFileName);
            vm.Concierge.Console.WriteLine($"Export to {outputFileFullPath} in progress");
            var exporterResult = await Task.Run(() => exporterFactory(folder.Path, outputFileFullPath).Execute());
            if (exporterResult.IsSuccess)
            {
                vm.Concierge.Console.WriteLine($"Export to {outputFileFullPath} completed");
                vm.Concierge.Console.WriteLine("Clear remote cold storage in progress");
                await clearCapability().Execute().AndWriteResultToConsole();
                vm.Concierge.Console.WriteLine("Clear remote cold storage completed");
            }
            else
                vm.Concierge.Console.WriteLine(exporterResult.Error.Message);

            if (unzipError.IsError)
                vm.Concierge.Console.WriteLine(unzipError.Error.Message);
        })
    {
    }

    private static Result<Unit> UnzipLocalMetricsSourceFiles([NotNull] string workingDirectory, [NotNull] IFileSystemService fileService,
        [NotNull] Func<string, string, Result<Unit>> unzipToDirectory, bool deleteFileAfterUnzip, [NotNull] Action<string> progress, [NotNull]string fileExtension)
    {
        var files = fileService.DirectoryGetFiles(workingDirectory, fileExtension);
        var files_count = files.Length;
        var errorCounter = 0;

        for (var i = 0; i < files_count; ++i)
        {
            var file = files[i];
            var fileName = Path.GetFileName(file);
            var progressPrefix = $"[{i + 1}/{files_count}]";
            var result = unzipToDirectory(file, workingDirectory);
            if (result.IsError)
            {
                ++errorCounter;
                progress($"{progressPrefix} {fileName}: ERROR {result.Error.Message}");
                continue;
            }

            if (deleteFileAfterUnzip)
            {
                fileService.DeleteFile(file);
                progress($"{progressPrefix} {fileName}: unzipped & removed successfully");
            }
            else
                progress($"{progressPrefix} {fileName}: unzipped successfully");
        }

        return errorCounter > 0
            ? Result<Unit>.Create(new Error("Unzip", " ERROR: Some files failed"))
            : default(Unit);
    }

    [NotNull]
    private static async Task<(string filePath, string checksum)[]> DownloadData(
        [NotNull] string destinationFolder, 
        [NotNull] ITargetSystemCapability downloadCapability,
        [NotNull] Action<string> progress)
    {
        var downloadedFiles = new List<(string filePath, string checksum)>();
        await foreach (var (count, length, name, checksum) in downloadCapability.Execute().AndSaveManyTo(destinationFolder))
        {
            progress($"[{count}/{length}] {name} into {destinationFolder}");
            var downloadedFileFullPath = Path.Combine(destinationFolder, name);
            downloadedFiles.Add((downloadedFileFullPath, checksum));
        }

        return downloadedFiles.ToArray();
    }

    [NotNull]
    private static (string fileFullPath, string expected, string computed)[] GetFilesWithChecksumError(
        [NotNull] (string fileFullPath, string checksumExpected)[] files, [NotNull] Func<string, string> computeFileChecksum)
    {
        var result = new List<(string fileFullPath, string expected, string computed )>();
        foreach (var file in files)
        {
            var checksumComputed = computeFileChecksum(file.fileFullPath);
            if (!string.Equals(file.checksumExpected, checksumComputed, StringComparison.OrdinalIgnoreCase))
                result.Add((file.fileFullPath, file.checksumExpected, checksumComputed));
        }

        return result.ToArray();
    }
}