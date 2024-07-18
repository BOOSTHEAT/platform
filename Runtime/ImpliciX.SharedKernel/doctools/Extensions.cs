using System.Reflection;

namespace ImpliciX.SharedKernel.DocTools
{
  public static class Extensions
  {
    public static T GetPrivatePropertyValue<T>(this object obj, string propertyName)
    {
      var property = obj.GetType().GetProperty(propertyName, BindingFlags.Instance|BindingFlags.NonPublic);
      return (T) property!.GetValue(obj);
    }
    public static T GetPrivateFieldValue<T>(this object obj, string fieldName)
    {
      var field = obj.GetType().GetField(fieldName, BindingFlags.Instance|BindingFlags.NonPublic);
      return (T) field!.GetValue(obj);
    }
  }
}