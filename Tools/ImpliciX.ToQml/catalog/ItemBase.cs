using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Catalog.CustomWidgets;

namespace ImpliciX.ToQml.Catalog;

public abstract class ItemBase : Screens
{
  protected RootModelNode Root => new ("");
  public abstract string Title { get; }
  public virtual IEnumerable<ModelNode> MeasureInputs => Enumerable.Empty<ModelNode>();
  public virtual IEnumerable<Urn> PropertyInputs => Enumerable.Empty<Urn>();
  public virtual IEnumerable<Urn> TimeSeriesInputs => Enumerable.Empty<Urn>();
  public virtual IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>();
  public virtual string? UserInfoMessage => null;
  public abstract Block Display { get; }

  public AlignedBlock[] MakeScreen(Block selector)
  {
    var alignedBlocks = new List<AlignedBlock>()
    {
      At.Origin.Put(Display),
      At.Top(0).Right(0).Put(Column.Layout(
        MeasureInputs.Select(MeasureSimulator.DefineBlock)
          .Concat(PropertyInputs.Select(PropertySimulator.DefineBlock))
          .Concat(TimeSeriesInputs.Select(TimeSeriesSimulator.DefineBlock))
          .Prepend(selector).ToArray()
      ))
    };
    
    if (!string.IsNullOrEmpty(UserInfoMessage))
    {
      alignedBlocks.Add(At.Bottom(0).Left(0)
        .Put(Label(UserInfoMessage).Width(CatalogGui.ScreenWidth)));
    }

    return alignedBlocks.ToArray();
  }
}