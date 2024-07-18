using System;
using System.IO;
using System.Text;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

public class TranslationTests
{
    [Test]
    public void TranslationDataFromCsvToJs()
    {
        var expectedJs = new SourceCodeGenerator()
            .Open("'en':").Append("'foo': \"dog\",", "'bar': \"cat\",", "'qix': \"Hello\\nWorld\",").Close("},")
            .Open("'fr':").Append("'foo': \"chien\",", "'bar': \"chat\",", "'qix': \"Bonjour\\nle monde\",").Close("},").Result;
        var translations = CreateTranslations();
        var actualJs = new SourceCodeGenerator().CreateTranslationDictionary(translations).Result;
        Assert.That(actualJs, Is.EqualTo(expectedJs));
    }

    private static Translations CreateTranslations()
    {
        var csvInputText = new SourceCodeGenerator()
            .Append("key,en,fr")
            .Append("foo,dog,chien")
            .Append("bar,cat,chat")
            .Append("qix,\"Hello\nWorld\",\"Bonjour\nle monde\"").Result;
        return CreateTranslations(csvInputText);
    }

    private static Translations CreateTranslations(string csvInputText)
    {
        var csvInput = new MemoryStream(Encoding.UTF8.GetBytes(csvInputText));
        var translations = new Translations(csvInput);
        return translations;
    }
    
    [Test]
    public void CheckNoMissingFeedKey()
    {
        var translations = CreateTranslations();
        var feeds = new[]
        {
            Const.IsTranslate("foo"),
            Const.IsTranslate("bar"),
            Const.IsTranslate("qix"),
        };
        var result = translations.Check(feeds);
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void CheckMissingFeedKey()
    {
        var translations = CreateTranslations();
        var feeds = new[]
        {
            Const.IsTranslate("foo"),
            Const.IsTranslate("fizz"),
            Const.IsTranslate("buzz"),
        };
        var result = translations.Check(feeds);
        Assert.That(result, Is.EqualTo(new []
        {
            "Missing 'fizz' translation key",
            "Missing 'buzz' translation key"
        }));
    }
    
    [Test]
    public void CheckNoMissingDropDownEntry()
    {
        var translations = CreateTranslations(new SourceCodeGenerator()
            .Append("key,en,fr")
            .Append("DropDownChoices.Foo,dog,chien")
            .Append("DropDownChoices.Bar,cat,chat")
            .Append("DropDownChoices.Qix,cow,vache").Result);
        var widgets = new[]
        {
            CreateDropDown<DropDownChoices>()
        };
        var result = translations.Check(widgets);
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void CheckMissingDropDownEntry()
    {
        var translations = CreateTranslations();
        var widgets = new[]
        {
            CreateDropDown<DropDownChoices>()
        };
        var result = translations.Check(widgets);
        Assert.That(result, Is.EqualTo(new []
        {
            "Missing 'DropDownChoices.Foo' translation key",
            "Missing 'DropDownChoices.Bar' translation key",
            "Missing 'DropDownChoices.Qix' translation key"
        }));
    }
    
    enum DropDownChoices
    {
        Foo,
        Bar,
        Qix,
    }

    private static Widget CreateDropDown<T>() => new DropDownList<T>(PropertyUrn<T>.Build("whatever")).CreateWidget();

    [Test]
    public void CheckNoMissingTextBoxEntry()
    {
        var translations = CreateTranslations();
        var widgets = new[]
        {
            CreateTextBox("foo"),
            CreateTextBox("bar"),
            CreateTextBox("qix"),
        };
        var result = translations.Check(widgets);
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void CheckMissingTextBoxEntry()
    {
        var translations = CreateTranslations();
        var feeds = new[]
        {
            CreateTextBox("foo"),
            CreateTextBox("fizz"),
            CreateTextBox("buzz"),
        };
        var result = translations.Check(feeds);
        Assert.That(result, Is.EqualTo(new []
        {
            "Missing 'fizz' translation key",
            "Missing 'buzz' translation key"
        }));
    }

    private static Widget CreateTextBox(string name) => new Input(PropertyUrn<Literal>.Build(name)).CreateWidget();

    [Test]
    public void TranslationDataIsOptional()
    {
        var expectedJs = new SourceCodeGenerator().Result;
        var actualJs = new SourceCodeGenerator().CreateTranslationDictionary(new Translations(null)).Result;
        Assert.That(actualJs, Is.EqualTo(expectedJs));
    }

    [Test]
    public void GenerateLocaleList_ShouldGenerateCorrectQmlCode()
    {
        var qmlCode = new SourceCodeGenerator().CreateLocaleList().Result;
        var enumValues = Enum.GetNames(typeof(Locale));

        foreach (var value in enumValues)
        {
            StringAssert.Contains($"'{value.Replace("__","_")}',", qmlCode);
        }

        StringAssert.StartsWith("function localeList() {\n  return [", qmlCode);
        StringAssert.EndsWith("];\n}\n", qmlCode);
    }
    
    [Test]
    public void GenerateTimezoneList_ShouldGenerateCorrectQmlCode()
    {
        var qmlCode = new SourceCodeGenerator().CreateTimezoneList().Result;
        var enumValues = Enum.GetNames(typeof(ImpliciX.Language.Model.TimeZone));

        foreach (var value in enumValues)
        {
            StringAssert.Contains($"'{value}',", qmlCode);
        }

        StringAssert.StartsWith("function timezoneList() {\n  return [", qmlCode);
        StringAssert.EndsWith("];\n}\n", qmlCode);
    }
}