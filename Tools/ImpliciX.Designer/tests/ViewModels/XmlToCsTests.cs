using System.Linq;
using ImpliciX.Designer.ViewModels.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

public class XmlToCsTests
{
  const string MyNamespace = "My.Csharp.Namespace";

  [Test]
  public void ConversionProducesCompilableCs()
  {
    var compilationUnit = ConvertAndParse();
    Assert.That(compilationUnit.Kind(), Is.EqualTo(SyntaxKind.CompilationUnit));
  }

  [Test]
  public void ConversionHasExpectedNamespace()
  {
    var compilationUnit = ConvertAndParse();
    var ns = compilationUnit.ChildNodes().Last();
    Assert.That(ns.Kind(), Is.EqualTo(SyntaxKind.NamespaceDeclaration));
    var nsQualifiedName = ns.ChildNodes().First();
    Assert.That(nsQualifiedName.GetText().ToString(), Is.EqualTo(MyNamespace));
  }

  [Test]
  public void ConversionHasDeclarations()
  {
    var compilationUnit = ConvertAndParse();
    var ns = compilationUnit.ChildNodes().Last();
    var declarations = ns.ChildNodes().Skip(1).ToArray();
    var kinds = declarations.Select(d => d.Kind());
    Assert.That(kinds, Has.All
      .EqualTo(SyntaxKind.ClassDeclaration).Or.EqualTo(SyntaxKind.EnumDeclaration)
    );
  }

  [Test]
  public void ConversionDeclaresValueObjectsAsPropertyUrn()
  {
    var compilationUnit = ConvertAndParse();
    var properties =
      (from node in compilationUnit.DescendantNodes()
        where node.Kind() == SyntaxKind.PropertyDeclaration
        let propertyName = node.ChildTokens().Last().Text
        let propertyType = node.ChildNodes().First()
        where propertyType.Kind() == SyntaxKind.GenericName
        where propertyType.ChildTokens().First().Text == "PropertyUrn"
        let valueType = propertyType.ChildNodes().First().ChildNodes().First().ChildTokens().First().Text
        select (valueType, propertyName)).ToArray();
    Assert.That(properties, Is.EqualTo(new[]
    {
      ("Percentage", "delta_humidity"),
      ("Percentage", "foo"),
      ("Time", "time"),
      ("Mass", "poids"),
      ("Time", "periode"),
    }));
  }

  private static SyntaxNode ConvertAndParse()
  {
    var converter = new XmlToCsConverter();
    var cs = converter.Convert(SomeXml, MyNamespace);
    var syntaxTree = CSharpSyntaxTree.ParseText(cs);
    var compilationUnit = syntaxTree.GetRoot();
    return compilationUnit;
  }

  private const string SomeXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<sheet>
    <root name=""monitoring"">
    
        <item type=""SubSystemX"" itemname=""process"">
           <generic type=""Processing_State""/>
           
        </item>
        <subgroup itemname=""computing"">
            <groupparent parent=""SubSystemX"">
               <generic type=""Computing_State""/>
               
            </groupparent>
            <item type=""Percentage"" itemname=""delta_humidity""/>
            <item type=""VersionSetting"" itemname=""ratio"">
               <generic type=""FunctionDefinition""/>
               
            </item>
            
        </subgroup>
        <item type=""Production"" itemname=""production_initiale""/>
        <item type=""Production"" itemname=""production_finale""/>
        <item type=""Acquisition"" itemname=""acquisitions""/>
        <item type=""Dashboard"" itemname=""dashboard""/>
        <item type=""Screen"" itemname=""swipe""/>
        <item type=""Screen"" itemname=""main_screen""/>
        <item type=""Screen"" itemname=""production_screen""/>
        <item type=""Screen"" itemname=""productivity_screen""/>
        <item type=""Screen"" itemname=""dryer_screen""/>
        <item type=""Screen"" itemname=""cond_screen""/>
        <item type=""Screen"" itemname=""quality_screen""/>
        <subgroup itemname=""burner"">
            <groupparent parent=""Burner"">
              <generic type=""ProdStatus"" />
              <generic type=""BurnerFan"" />
            </groupparent>
            <item type=""Percentage"" itemname=""foo""/>
        </subgroup>
    </root>
    <group typename=""Production"">

        <groupparent parent=""Burner"">
            <generic type=""ProdStatus"" />
            <generic type=""BurnerFan"" />
        </groupparent>

        <item type=""Tonnage"" itemname=""productivite""/>
        <item type=""Time"" itemname=""time""/>
        
    </group>
    <group typename=""BurnerFan"">
        <groupparent parent=""Fan""/>
        
        <item type=""Measure"" itemname=""speed"">
           <generic type=""RotationalSpeed""/>
        </item>
    </group>
    <group typename=""Tonnage"">
    
        <item type=""Mass"" itemname=""poids""/>
        <item type=""Time"" itemname=""periode""/>
        
    </group>
    <group typename=""Acquisition"">
        <groupparent parent=""SubSystemX"">
           <generic type=""Acquisition_State""/>
           
        </groupparent>
        <item type=""Measure"" itemname=""poids_pellet"">
           <generic type=""Mass""/>
           
        </item>
        <item type=""Measure"" itemname=""prod_status"">
           <generic type=""ProdStatus""/>
           
        </item>
        <item type=""Measure"" itemname=""humidity_crusher_output"">
           <generic type=""Percentage""/>
           
        </item>
        <item type=""Measure"" itemname=""humidity_dryer_output"">
           <generic type=""Percentage""/>
           
        </item>
        
    </group>
    <enum type=""ProdStatus"" show_value=""true"">
        <enumvalue name=""Stop"" value=""0""/>
        <enumvalue name=""Run"" value=""1""/>
        
    </enum>
    <group typename=""Dashboard"">
    
        <item type=""Metric"" itemname=""pellets""/>
        <item type=""Metric"" itemname=""drying_efficiency""/>
        <item type=""Metric"" itemname=""production_state""/>
        
    </group>
    
</sheet>
";
}