using System;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

public class ApplicationTests
{
  [TestCase("implicix", "Keyboards/QtQuick/VirtualKeyboard/Layout/implicix")]
  [TestCase("implicax", "Keyboards/QtQuick/VirtualKeyboard/Layout/implicax")]
  [TestCase("implikix", "LocalKeyboards/QtQuick/VirtualKeyboard/Layout/implikix")]
  [TestCase("implicox", "LocalKeyboards/QtQuick/VirtualKeyboard/Layout/implicox")]
  [TestCase("implicux", "LocalKeyboards/QtQuick/VirtualKeyboard/Layout/implicux")]
  public void FindLayoutPath(string requestedLayout, string expectedLayoutPath)
  {
    Assert.That(QmlApplication.ChooseKeyboardLayoutPath(requestedLayout,Resources), Is.EqualTo(expectedLayoutPath));
  }
  
  [TestCase("foo")]
  public void CannotFindLayoutPath(string requestedLayout)
  {
    Assert.Throws<Exception>(
      () => QmlApplication.ChooseKeyboardLayoutPath(requestedLayout, Resources)
    );
  }

  public static string[] Resources = new[]
  {
    "LocalKeyboards/QtQuick/VirtualKeyboard/Layout/implicux/fr_FR/main.qml",
    "Keyboards/QtQuick/VirtualKeyboard/Layout/implicix/fr_CA/main.qml",
    "Keyboards/QtQuick/VirtualKeyboard/Layout/implicox/fr_BE/main.qml",
    "Keyboards/QtQuick/VirtualKeyboard/Layout/implicux/fr_NW/main.qml",
    "LocalKeyboards/QtQuick/VirtualKeyboard/Layout/implikix/fr_US/main.qml",
    "LocalKeyboards/QtQuick/VirtualKeyboard/Layout/implicox/fr_CH/main.qml",
    "Keyboards/QtQuick/VirtualKeyboard/Layout/implicax/fr_CA/main.qml",
  };
}