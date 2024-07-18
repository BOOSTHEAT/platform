using System;
using System.CommandLine;
using System.IO;
using Avalonia.Controls;
using ImpliciX.Designer.Features;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer
{
    internal static class CommandLine
    {
        public static void Process(IFeatures features)
        {
            var command = new RootCommand
            {
                new Option<FileInfo>(new[] { "-a", "--application" }, "Full path to the .nupkg, .dll or .csproj").ExistingOnly(),
                new Option<string>(new[] { "-c", "--connect" }, "Device name or IP Address"),
                new Option<FileInfo>(new[] { "-p", "--pdf" }, "Full path to pdf output").LegalFilePathsOnly(),
            };
            command.SetHandler((FileInfo application, string deviceToConnect, FileInfo outputPdf) =>
            {
                if (application != null)
                {
                    features.Concierge.Applications.Load(application.FullName);
                }
                if (deviceToConnect != null)
                {
                    features.Window.LiveConnectViewModel.ConnectionString = deviceToConnect;
                    features.Window.AutoConnect = true;
                }
                if (outputPdf != null)
                {
                    Console.WriteLine($"Printing to {outputPdf.FullName}");
                    features.Window.SaveAsPdf(outputPdf.FullName);
                    Environment.Exit(0);
                }
            }, command.Options[0], command.Options[1], command.Options[2]);
            var result = command.Invoke(Environment.GetCommandLineArgs());
            if (result != 0)
                Environment.Exit(result);
        }
    }
}