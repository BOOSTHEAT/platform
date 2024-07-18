using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImpliciX.ToQml
{
  public class SourceCodeGenerator
  {
    public SourceCodeGenerator Append(string text)
    {
      _code.Append(new string(' ', _indent * 2));
      _code.Append(text);
      _code.AppendLine();
      return this;
    }

    public SourceCodeGenerator Append(params object[] texts)
    {
      Append(texts.Select(x => x.ToString()));
      return this;
    }

    public SourceCodeGenerator Append(params string[] texts)
    {
      Append(texts.AsEnumerable());
      return this;
    }

    public SourceCodeGenerator Append(IEnumerable<string> texts)
    {
      foreach (var text in texts)
        Append(text);

      return this;
    }

    public SourceCodeGenerator Append(bool condition, Func<object> getText)
    {
      if (condition)
        Append(getText());

      return this;
    }

    public SourceCodeGenerator Open(string text, string opener = " {")
    {
      Append(text + opener);
      _indent++;
      return this;
    }

    public SourceCodeGenerator Close(string closer = "}")
    {
      _indent--;
      Append(closer);
      return this;
    }
    
    public SourceCodeGenerator ForEach<T>(IEnumerable<T> xs, Action<T, SourceCodeGenerator> action)
    {
      foreach (var x in xs)
        action(x, this);

      return this;
    }
    
    public SourceCodeGenerator If(bool condition, Action<SourceCodeGenerator> ifTrue, Action<SourceCodeGenerator> ifFalse = null)
    {
      if(condition)
        ifTrue(this);
      else
        ifFalse?.Invoke(this);
      return this;
    }

    public string Result => _code.ToString();
    private readonly StringBuilder _code = new StringBuilder();
    private int _indent = 0;
  }
}