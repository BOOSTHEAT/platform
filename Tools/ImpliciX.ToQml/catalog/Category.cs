using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog;

internal sealed class Category
{
  public CategoryName Name { get; }
  public (GuiNode, ItemBase item)[] Items { get; }
  public Dictionary<GuiNode, string> Titles { get; }

  private Category(CategoryName name, ItemBase[] items)
  {
    Name = name;
    Items = items.Prepend(new StartupScreen())
      .Select(item => (CreateGuiNode(name, item.GetType().Name), item))
      .ToArray();

    Titles = Items.ToDictionary(x => x.Item1, x => x.item.Title);
  }

  public static Dictionary<string, Category> Load(Dictionary<CategoryName, ItemBase[]> itemsInCatalog)
  {
    var categories = new Dictionary<string, Category>
    {
      ["Select category..."] = new (CategoryName.None, Array.Empty<ItemBase>())
    };

    foreach (var item in itemsInCatalog)
      categories.Add(item.Key.ToString(), new Category(item.Key, item.Value));

    return categories;
  }

  public static GuiNode CreateGuiNode(CategoryName categoryName, string itemTypeName) => new (RootNode, $"screen_{categoryName}{itemTypeName}");
  public static GuiNode CreateGuiNode(string name) => new (RootNode, name);
  private static readonly RootModelNode RootNode = new ("root");
}

public enum CategoryName
{
  None,
  Misc,
  Inputs,
  Charts
}