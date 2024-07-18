using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.ViewModels.Tools;

public class XmlToCsViewModel : ActionMenuViewModel<ILightConcierge>
{
  private XmlToCsConverter _converter;

  public XmlToCsViewModel(ILightConcierge concierge) : base(concierge)
  {
    Text = "Convert ImpliciX .xml file to .cs file...";
    _converter = new XmlToCsConverter();
  }

  public override async void Open()
  {
    try
    {
      var file = await Concierge.User.OpenFile(new IUser.FileSelection
      {
        AllowMultiple = true,
        Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        DefaultExtension = "*.xml"
      });
      if (file.Choice != IUser.ChoiceType.Ok)
        return;
      foreach (var input in file.Paths)
      {
        var output = Path.ChangeExtension(input, ".cs");
        Concierge.Console.WriteLine($"Converting {input} to {output}");
        var xmlCode = await File.ReadAllTextAsync(input);
        var csCode = _converter.Convert(xmlCode, "Model");
        await File.WriteAllTextAsync(output, csCode);
      }
    }
    catch (Exception e)
    {
      await Errors.Display(e);
    }
  }
}

public class XmlToCsConverter
{
  private XslCompiledTransform _xslt;

  public XmlToCsConverter()
  {
    var xsltResourceName = GetType().Namespace + ".xmlToCs.xslt";
    using var xsltStream = Assembly
                             .GetExecutingAssembly()
                             .GetManifestResourceStream(xsltResourceName)
                           ?? throw new Exception($"Missing {xsltResourceName}");
    using var xsltReader = XmlReader.Create(xsltStream!);
    _xslt = new XslCompiledTransform();
    _xslt.Load(xsltReader);
  }

  public string Convert(string xml, string csNamespace)
  {
    using var reader = new StringReader(xml);
    using var xmlReader = XmlReader.Create(reader);
    using var writer = new StringWriter();
    var args = new XsltArgumentList();
    args.AddParam("namespace", "", csNamespace);
    var valueTypes = typeof(ValueObject)
      .Assembly.GetTypes()
      .Where(t => t.GetCustomAttributes<ValueObject>().Any())
      .Select(t => t.Name)
      .ToList();
    valueTypes.Add("Time");
    var valueTypesNavigator = new XElement("root",
      valueTypes.Select(t => (object)new XElement("type", t)).ToArray()
    ).ToXPathNavigable().CreateNavigator()!;
    args.AddParam("valueTypes", "", valueTypesNavigator);
    _xslt.Transform(xmlReader, args, writer);
    return writer.ToString();
  }
}
