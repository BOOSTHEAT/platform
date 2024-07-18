using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ImpliciX.Language.Model;

namespace ImpliciX.Linker;

public static class OptionsExtensions
{
  public static TOption Required<TOption>(
    this TOption option)
    where TOption : Option
  {
    option.IsRequired = true;
    return option;
  }
  
  public static TOption DefaultValue<TOption>(
    this TOption option, object defaultValue)
    where TOption : Option
  {
    option.SetDefaultValue(defaultValue);
    return option;
  }
  
  public static TOption CodeIdentifier<TOption>(
    this TOption option)
    where TOption : IOption
  {
    ((Argument)option.Argument).AddValidator(
      x => Regex.IsMatch(x.Tokens.First().Value,"^[A-Za-z]+(\\.[A-Za-z0-9]+)*$") ? null : "Invalid Identifier"
    );
    return option;
  }
  
  public static TOption VersionNumberOnly<TOption>(
    this TOption option)
    where TOption : IOption
  {
    ((Argument)option.Argument).AddValidator(
      a =>
        a.Tokens
          .Select(t => t.Value)
          .Where(VersionNumberIsInvalid)
          .Select(x => $"Version number {x} is not valid.")
          .FirstOrDefault());
    return option;
  }

  public static bool VersionNumberIsInvalid(string value) => SoftwareVersion.IsInvalid(value);
  
  public static TOption InvalidWhen<TOption>(
    this TOption option, Func<string,bool> invalidator)
    where TOption : IOption
  {
    ((Argument)option.Argument).AddValidator(
      a =>
        a.Tokens
          .Select(t => t.Value)
          .Where(invalidator)
          .Select(t => $"Invalid {typeof(TOption).Name} {t}")
          .FirstOrDefault());
    return option;
  }
  
  public static TOption NonExistingOnly<TOption>(
    this TOption option)
    where TOption : IOption
  {
    ((Argument)option.Argument).AddValidator(
      a =>
        a.Tokens
          .Select(t => t.Value)
          .Where(File.Exists)
          .Select(f => $"File {f} already exists.")
          .FirstOrDefault());

    return option;
  }
}