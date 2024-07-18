using System;
using System.Collections.Generic;
using System.Drawing;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers
{
  public static class StyleRenderer
  {
    public static void Render(this Style style, SourceCodeGenerator code)
    {
      if (style == null)
        return;

      RenderItem(style.FontSize, fontSize => code.Append($"font.pixelSize: {fontSize}"));
      RenderItem(style.FrontColor, frontColor => code.Append($"color: {ToQmlString(frontColor)}"));
      RenderItem(style.FontFamily, fontFamilyStyle =>
      {
        var fontFamily = Families[fontFamilyStyle];
        code.Append($"font.family: {fontFamily.Family}");
        code.Append($"font.weight: {fontFamily.Weight}");
      });
    }

    public static string ToQmlString(Color color) => $"\"#{color.R:X2}{color.G:X2}{color.B:X2}\"";

    private static void RenderItem<T>(T? item, Action<T> renderer) where T : struct
    {
      if (item.HasValue)
        renderer(item.Value);
    }

    public static string AsQtFont(this Style style) =>
      $"Qt.font({{family:{Families[style.FontFamily!.Value].Family},pixelSize:{style.FontSize}}})";

    public static Style Fallback(this Style style, Style other)
    {
      if (style == null)
        return other;

      if (other == null)
        return style;

      return new Style
      {
        FontSize = style.FontSize ?? other.FontSize,
        FrontColor = style.FrontColor ?? other.FrontColor,
        FontFamily = style.FontFamily ?? other.FontFamily
      };
    }

    static StyleRenderer()
    {
      Families = new Dictionary<Style.Family, (string, string)>
      {
        [Style.Family.Light] = ("UiConst.fontLt", "Font.Light"),
        [Style.Family.Regular] = ("UiConst.fontBtr", "Font.Medium"),
        [Style.Family.Medium] = ("UiConst.fontMd", "Font.Medium"),
        [Style.Family.Heavy] = ("UiConst.fontHv", "Font.Black"),
        [Style.Family.ExtraBold] = ("UiConst.fontPnEb", "Font.Black"),
      };
    }

    public static readonly Dictionary<Style.Family, (string Family, string Weight)> Families;
  }
}