using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Widgets;

namespace ImpliciX.ToQml.Catalog.CustomWidgets;

internal sealed class ScreenSelector
{
  public static void AddTo(QmlRenderer qmlRenderer) => qmlRenderer.AddRenderer<W>(new R());

  public class B : Block
  {
    private readonly Dictionary<string, Category> _categories;

    public B(Dictionary<string, Category> categories)
    {
      _categories = categories;
    }

    public override Widget CreateWidget() =>
      new W
      {
        Categories = _categories,
      };
  }

  public class W : Widget
  {
    public Dictionary<string, Category> Categories;
  }

  public static W D(Widget w) => (W) w;

  public class R : IRenderWidget
  {
    public void Render(WidgetRenderingContext context)
    {
      static string JsArray(IEnumerable<string> items) => $"[{string.Join(',', items)}]";
      static string JsDictionary(IEnumerable<(string, string)> items) => $"{{{string.Join(',', items.Select(x => $"'{x.Item1}':{x.Item2}"))}}}";
      var categories = ((W) context.Widget).Categories;
      var categoryFromPath =
        from category in categories
        from title in category.Value.Titles
        select (title.Key.Urn.Value, $"'{category.Key}'");

      var screensForCategory = categories.Values
        .Select(c => (
            c.Name.ToString(),
            JsArray(c.Titles.Select(x => $"{{ path: '{x.Key.Urn.Value}', title: qsTr('{x.Value}') }}"))
          ));

      var defaultValuesFromPath =
        from category in ((W) context.Widget).Categories
        from item in category.Value.Items
        let path = item.Item1.Urn.Value
        let defaultValues = item.item.DefaultValues.Select(kv => (kv.Key.Value, kv.Value))
        select (path, JsDictionary(defaultValues));

      context.Code
        .Open(context.Prefix + " ScreenSelection")
        .Append("id: screenSelector")
        .Append("runtime: root.runtime")
        .Append($"categories: {JsArray(categories.Keys.Select(c => $"qsTr('{c}')"))}")
        .Append($"categoryFromPath: {JsDictionary(categoryFromPath)}")
        .Append($"screensForCategory: {JsDictionary(screensForCategory)}")
        .Append($"defaultValues: {JsDictionary(defaultValuesFromPath)}")
        .Close();
    }

    public IEnumerable<Feed> FindFeeds(Widget widget) => Enumerable.Empty<Feed>();
  }
}