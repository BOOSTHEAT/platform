using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices;

public interface IUser
{
  [Flags]
  public enum ChoiceType
  {
    None = 0,
    Ok = 1,
    Cancel = 2,
    Yes = 4,
    No = 8,
    Abort = 16,
    Custom1 = 32,
    Custom2 = 64,
    Custom3 = 128
  }

  public enum Icon
  {
    None,
    Error,
    Info,
    Setting,
    Stop,
    Success,
    Warning
  }

  public string ConsoleFolderPath { get; }
  public bool IsConsoleWrittenToFile { get; }
  public string AppName { get; }
  Task<ChoiceType> Show(Box box);
  Task<(ChoiceType Choice, string Password)> EnterPassword(Box box);
  Task<(ChoiceType Choice, string Path)> OpenFolder(FileSelection selection);
  Task<(ChoiceType Choice, string[] Paths)> OpenFile(FileSelection selection);
  Task<(ChoiceType Choice, string Path)> SaveFile(FileSelection selection);

  public static IEnumerable<Choice> StandardButtons(params ChoiceType[] types)
  {
    return types.Select(t => new Choice {Type = t});
  }

  class Box
  {
    public string Title { get; set; }
    public string Message { get; set; }
    public Icon Icon { get; set; } = Icon.None;
    public IEnumerable<Choice> Buttons { get; set; } = new List<Choice>();
  }

  public class Choice
  {
    public ChoiceType Type { get; set; }
    public string Text { get; set; }
    public bool IsDefault { get; set; }
    public bool IsCancel { get; set; }

    public static IEnumerable<Choice> operator +(Choice a, Choice b)
    {
      return new[] {a, b};
    }

    public static IEnumerable<Choice> operator +(IEnumerable<Choice> a, Choice b)
    {
      return a.Append(b);
    }
  }

  public class FileSelection
  {
    public string Title { get; set; }
    public string Directory { get; set; }
    public List<FileSelectionFilter> Filters { get; set; } = new();
    public string InitialFileName { get; set; }
    public string DefaultExtension { get; set; }
    public bool AllowMultiple { get; set; }
  }

  public class FileSelectionFilter
  {
    public string Name { get; set; }
    public List<string> Extensions { get; set; } = new();
  }
}

public static class UserExtensions
{
  public static bool Is(this IEnumerable<IUser.ChoiceType> a, IUser.ChoiceType b)
    => a.Cast<int>().Sum() == (int)b;

  public static bool Contains(this IEnumerable<IUser.ChoiceType> a, IUser.ChoiceType b)
    => (a.Cast<int>().Sum() & (int)b) != 0;

  public static IUser.Choice With(this IUser.ChoiceType type, string text = null, bool isDefault = false,
    bool isCancel = false)
    => new IUser.Choice {Type = type, Text = text ?? type.ToString(), IsDefault = isDefault, IsCancel = isCancel};
}
