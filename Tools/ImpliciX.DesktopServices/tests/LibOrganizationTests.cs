using System.Reflection;

namespace ImpliciX.DesktopServices.Tests;

public class LibOrganizationTests
{
  private static readonly Type RefType = typeof(ILightConcierge);
  private static readonly Assembly RefLib = RefType.Assembly;

  [Test]
  public void AllTypesInInnerNamespacesShallNotBeAccessedFromOutside()
  {
    var innerNamespacesPrefix = $"{RefLib.GetName().Name!}.";
    var innerTypes = RefLib.GetTypes().Where(t => t.Namespace != null && t.Namespace.StartsWith(innerNamespacesPrefix));
    foreach (var innerType in innerTypes)
    {
      Assert.That(!IsVisible(innerType), $"{innerType.FullName} shall not be accessible from outside");
    }
  }

  bool IsVisible(Type t) => t.IsNested ? IsVisible(t.DeclaringType!) && t.IsNestedPublic : t.IsPublic;
}