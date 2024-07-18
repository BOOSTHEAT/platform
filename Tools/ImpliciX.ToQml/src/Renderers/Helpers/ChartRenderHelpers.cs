namespace ImpliciX.ToQml.Renderers.Helpers;

internal static class ChartRenderHelpers
{
  public static string GetMouseAreaForOnClickedEventRoute(string route)
  {
    return $@"MouseArea {{
    anchors.fill: parent
    onClicked: {route}.clicked()
  }}";
  }
}