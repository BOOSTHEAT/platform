using System;
using System.Linq;
using Autopsy.ILSpy;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SharedKernel.DocTools
{
  public class TransitionViewModel
  {
    private TransitionViewModel(string text)
    {
      Text = text;
    }

    public static TransitionViewModel Create<T>(Transition<T, DomainEvent> definition)
    {
      try
      {
        var syntaxTree = Reader.Read(definition.Condition);
        var method = (MethodDeclaration) syntaxTree.Children.First(c => c is MethodDeclaration);
        var ret = (ReturnStatement) method.Body.Last();
        var text = ret.Expression.ToString();
        return new TransitionViewModel(text);
      }
      catch(Exception e)
      {
        return new TransitionViewModel(e.Message);
      }
    }

    static TransitionViewModel()
    {
      Reader = DelegateReader.CreateCachedWithDefaultAssemblyProvider(false);
    }

    private static readonly IDelegateReader Reader;

    public string Text { get; }
  }
}