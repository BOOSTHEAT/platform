using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DynamicData.Kernel;
using ImpliciX.Designer.Dialogs;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer;

public class User : IUser
{
  public readonly MessageBoxes MessageBoxes;
  private IStorageProvider _storageProvider;

  public User(string appName)
  {
    AppName = appName;
    MessageBoxes = new MessageBoxes();
  }

  public string ConsoleFolderPath => Path.GetTempPath();
  public bool IsConsoleWrittenToFile => true;
  public string AppName { get; }

  public Task<IUser.ChoiceType> Show(IUser.Box box) => MessageBoxes.Show(box);

  public Task<(IUser.ChoiceType, string)> EnterPassword(IUser.Box box) => MessageBoxes.EnterPassword(box);

  public async Task<(IUser.ChoiceType, string)> OpenFolder(IUser.FileSelection selection)
  {
    var option = new FolderPickerOpenOptions();
    option.Title = selection.Title;
    option.SuggestedStartLocation = await _storageProvider.TryGetFolderFromPathAsync(new Uri(selection.Directory));
    var folders = await _storageProvider.OpenFolderPickerAsync(option);
    return (folders.Count == 0 || folders.Count > 1)
      ? (IUser.ChoiceType.Cancel, "")
      : (IUser.ChoiceType.Ok, FileNameToString(folders[0]));
  }

  public async Task<(IUser.ChoiceType, string[])> OpenFile(IUser.FileSelection selection)
  {
    var option = new FilePickerOpenOptions();
    option.Title = selection.Title;
    option.AllowMultiple = selection.AllowMultiple;
    if (selection?.Directory != null)
      option.SuggestedStartLocation = await _storageProvider.TryGetFolderFromPathAsync(new Uri(selection.Directory));
    IReadOnlyList<FilePickerFileType> filter =
        selection.Filters.ToDictionary(selectionFilter =>
              new FilePickerFileType(selectionFilter.Name)
            ,
            selectionFilter =>
              selectionFilter
          ).ToDictionary(pair => pair.Key
            ,
            pair =>
              pair.Value.Extensions.Select(extension => "*." + extension
              ).AsList()
          ).ToDictionary(pair => pair.Key
            ,
            pair =>
              pair.Key.Patterns = pair.Value
          ).Select(pair => pair.Key
          )
          .AsArray()
      ;
    option.FileTypeFilter = filter;
    var files = await _storageProvider.OpenFilePickerAsync(option);
    var storesName = files.Select(
      f =>
        FileNameToString(f)
    ).ToArray();
    var res = (files.Count == 0 ? IUser.ChoiceType.Cancel : IUser.ChoiceType.Ok, storesName.ToArray()
      );
    return res;
  }

  public async Task<(IUser.ChoiceType, string)> SaveFile(IUser.FileSelection selection)
  {
    string extension = null;
    if (selection.Filters.Count > 0)
    {
      var extensions = selection.Filters.First().Extensions;
      if (extensions.Count > 0)
      {
        extension = extensions.First();
      }
    }

    IStorageFolder suggestedStartLocation = null;
    if (selection.Directory != null)
    {
      suggestedStartLocation =
        _storageProvider.TryGetFolderFromPathAsync(new Uri("file://" + selection.Directory)).Result;
    }

    {
    }
    var option = new FilePickerSaveOptions
    {
      Title = selection.Title,
      DefaultExtension = extension,
      SuggestedFileName = selection.InitialFileName,
      SuggestedStartLocation = suggestedStartLocation
    };
    var file = await _storageProvider.SaveFilePickerAsync(option);
    return (file == null ? IUser.ChoiceType.Cancel : IUser.ChoiceType.Ok, FileNameToString(file));
  }

  private static string FileNameToString(IStorageItem file)
  {
    Console.WriteLine("FileNameToString " + file.Path);
    var res = file.Path.LocalPath;
    Console.WriteLine(" = " + res);
    return res;
  }

  public void RegisterOn(TopLevel topLevel)
  {
    _storageProvider = topLevel.StorageProvider;
    MessageBoxes.RegisterOn(topLevel);
  }
}
