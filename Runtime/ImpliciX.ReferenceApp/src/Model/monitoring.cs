using ImpliciX.Language.Model;
using ImpliciX.ReferenceApp.Model.Tree;

namespace ImpliciX.ReferenceApp.Model;

public class monitoring : RootModelNode
{
    public static monitoring Self { get; }
    public static heat_counter heating { get; }
    public static heat_counter dhw { get;  }
    public static product product { get; }
    public static pulse_counter pulse_counter { get; }
    public static modboss modboss { get; }
    static monitoring()
    {
        Self = new monitoring();
        pulse_counter = new pulse_counter(nameof(pulse_counter), Self);
        product = new product(nameof(product), Self);
        heating = new heat_counter(nameof(heating), Self);
        dhw = new heat_counter(nameof(dhw), Self);
        modboss = new modboss(nameof(modboss), Self);
    }


    private monitoring() : base(nameof(monitoring))
    {
    }
}