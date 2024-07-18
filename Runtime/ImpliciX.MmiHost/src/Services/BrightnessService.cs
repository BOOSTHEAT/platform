using System;
using System.IO;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.MmiHost.Services
{
    public static class BrightnessService
    {
        public static ushort ComputeBrightNess(Percentage percentage) => (ushort)(250 * percentage.ToFloat() + 5);

        public static void SetBrightness(Percentage percentage, string backlightSysClassPath)
        {
            SetBrightness(ComputeBrightNess(percentage), backlightSysClassPath);
        }

        private static void SetBrightness(ushort brightness, string backlightSysClassPath)
        {
            try
            {
                File.WriteAllText(backlightSysClassPath, brightness.ToString());
            }
            catch (Exception e)
            {
                Log.Warning($"Unable to write file {backlightSysClassPath}: {e.Message}");
            }
        }
    }
}