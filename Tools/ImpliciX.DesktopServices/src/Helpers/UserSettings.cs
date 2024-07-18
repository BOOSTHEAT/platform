using System.Configuration;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices.Helpers;

internal class UserSettings
{
  public static void Set(string name, string value)
  {
    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    var settings = configFile.AppSettings.Settings;
    if (settings[name] == null)
      settings.Add(name, value);
    else
      settings[name].Value = value;
    configFile.Save(ConfigurationSaveMode.Modified);
  }

  public static void Clear(string name)
  {
    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    var settings = configFile.AppSettings.Settings;
    settings.Remove(name);
    configFile.Save(ConfigurationSaveMode.Modified);
  }

  [CanBeNull]
  public static string Read(string name)
  {
    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    return configFile.AppSettings.Settings[name]?.Value;
  }
}