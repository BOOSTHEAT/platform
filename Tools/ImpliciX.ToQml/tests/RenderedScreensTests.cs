using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ImpliciX.Language.GUI;
using NUnit.Framework;
using DateTime = System.DateTime;

namespace ImpliciX.ToQml.Tests;

public class RenderedScreensTests
{
  [Test]
  public void NoNullFeeds()
  {
    var copyrightManager = new CopyrightManager("app", DateTime.Now.Year);
    var folder = new DirectoryInfo(Path.GetTempPath());
    var rendering = new QmlRenderer(folder, copyrightManager);
    var gui = (new GUI())
      .Assets(Assembly.GetExecutingAssembly());

    var sut = new RenderedScreens(gui.ToSemanticModel(), rendering, Array.Empty<string>());
    
    Assert.That(sut.Feeds.Where(f => f==null), Is.Empty);
  }
}