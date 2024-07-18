using System.Reflection;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Catalog.CustomWidgets;
using ImpliciX.ToQml.Catalog.Items.Inputs;
using TimeZone = ImpliciX.Language.Model.TimeZone;

namespace ImpliciX.ToQml.Catalog;

internal sealed class CatalogGui : Screens
{
  public const int ScreenHeight = 480;
  public const int ScreenWidth = 1100;

  public static GUI Create(Dictionary<CategoryName, ItemBase[]> itemsInCatalog)
  {
    var categories = Category.Load(itemsInCatalog);
    var selector = new ScreenSelector.B(categories);
    var screens = categories.Values
      .SelectMany(category => category.Items)
      .ToDictionary(x => x.Item1, x => x.item.MakeScreen(selector));

    var hSwipe = Category.CreateGuiNode("hSwipe");
    var hSwipeScreens = categories.First(c => c.Key == "Misc").Value.Items.Select(x => x.Item1).ToArray();

    var gui = GUI
      .Assets(Assembly.GetExecutingAssembly())
      .StartWith(screens.First().Key)
      .ScreenSize(ScreenWidth, ScreenHeight)
      .Translations("assets.translations.csv")
      .Locale(PropertyUrn<Locale>.Build("general", "locale"))
      .TimeZone(PropertyUrn<TimeZone>.Build("general", "timezone"))
      .VirtualKeyboard("implikix:implikix")
      .ScreenSaver(TimeSpan.FromSeconds(30), ScreenSaver.Node)
      .Screen(ScreenSaver.Node, ScreenSaver.Blocks)
      .WhenNotConnected(WaitingConnectionScreen.Node)
      .Screen(WaitingConnectionScreen.Node, WaitingConnectionScreen.Blocks)
      .Screen(ScreenBlockBehaviors.Node, ScreenBlockBehaviors.Blocks)
      .Screen(ScreenGoBackToBlockBehaviors.Node, ScreenGoBackToBlockBehaviors.Blocks)
      .Group.HorizontalSwipe(hSwipe, hSwipeScreens[0], hSwipeScreens[1], hSwipeScreens.Skip(2).ToArray());

    return screens.Aggregate(gui, (g, x) => g.Screen(x.Key, x.Value));
  }
}