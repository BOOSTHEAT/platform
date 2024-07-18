using ReactiveUI;

namespace ImpliciX.Designer.Tests.ViewModels
{
  public class CheckInteraction
  {
    public static Result<TI> Setup<TI, TO>(Interaction<TI, TO> interaction, TO sendBack)
    {
      var result = new Result<TI>();
      interaction.RegisterHandler(x =>
      {
        result.Input = x.Input;
        x.SetOutput(sendBack);
      });
      return result;
    }

    public class Result<T>
    {
      public T Input = default(T);
    }
  }
}