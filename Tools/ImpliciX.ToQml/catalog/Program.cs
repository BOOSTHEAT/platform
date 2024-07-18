// See https://aka.ms/new-console-template for more information

using ImpliciX.ToQml;
using ImpliciX.ToQml.Catalog;
using ImpliciX.ToQml.Catalog.CustomWidgets;
using ImpliciX.ToQml.Catalog.Items;
using ImpliciX.ToQml.Catalog.Items.Inputs;

var gui = CatalogGui.Create(new Dictionary<CategoryName, ItemBase[]>
{
  [CategoryName.Misc] = new ItemBase[]
  {
    new ShowLabel(),
    new ShowMeasure(),
    new ShowProperty(),
    new ThermometerAsAnimatedImage(),
    new ThermometerAsMeasureDataDrivenImage(),
    new ThermometerAsPropertyDataDrivenImage(),
    new SwitchOnMeasure(),
    new SwitchOnProperty(),
    new IncrementForBlock()
  },
  [CategoryName.Inputs] = new ItemBase[]
  {
    new OnOffButtonItem(),
    new DropDownListItem(),
    new TextInput(),
    new BlockBehaviorsItem()
  },
  [CategoryName.Charts] = new ItemBase[]
  {
    new BarsChart(),
    new PieChartItem(),
    new StackedTimeBarsItem(),
    new TimeLinesItem(),
    new TimeStackedBarsAndLinesItem(),
    new MultiTimeLinesItem(),
  }
});

const string appName = "CatalogGui";
var copyrightManager = new CopyrightManager(appName, DateTime.Now.Year);
var folder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), appName + "_" + Path.GetRandomFileName()));
folder.Create();
var rendering = new QmlRenderer(folder, copyrightManager);

var runtime = Environment.GetEnvironmentVariable("CATALOGGUI_RUNTIME") ?? "Catalog";

ScreenSelector.AddTo(rendering);
MeasureSimulator.AddTo(rendering);
PropertySimulator.AddTo(rendering);
TimeSeriesSimulator.AddTo(rendering);
QmlApplication.Create(gui.ToSemanticModel(), rendering, runtime, new[] {"assets 1.0"});
Console.WriteLine(folder);