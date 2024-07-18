using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ImpliciX.Api.TcpModbus.Tests;

public class HexagonalArchitectureTests
{
  [Test]
  public void DependenciesToNModbusAreLimitedToInfrastructure()
  {
    var allNModbusTypesUsedByTypesOutsideInfrastructure = typeof(ApiTcpModbusModule)
      .Assembly.GetTypes()
      .Where(t => t.Namespace != null && !t.Namespace.Contains("Infrastructure"))
      .SelectMany(GetAllTypesUsedBy)
      .Where(t => t.FullName!.StartsWith("NModbus."))
      .ToArray();
    Assert.That(allNModbusTypesUsedByTypesOutsideInfrastructure, Is.Empty);
  }

  private IEnumerable<Type> GetAllTypesUsedBy(Type type) =>
    type.GetConstructors().SelectMany(GetTypesUsedBy)
      .Concat(type.GetFields().SelectMany(GetTypesUsedBy))
      .Concat(type.GetMethods().SelectMany(GetTypesUsedBy))
      .Concat(type.GetProperties().SelectMany(GetTypesUsedBy))
      .Concat(type.GetNestedTypes().SelectMany(GetAllTypesUsedBy));

  private IEnumerable<Type> GetTypesUsedBy(ConstructorInfo constructor) =>
    constructor.GetParameters().Select(p => p.ParameterType);

  private IEnumerable<Type> GetTypesUsedBy(FieldInfo field) => new[] { field.FieldType };
  
  private IEnumerable<Type> GetTypesUsedBy(MethodInfo method) =>
    method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType);

  private IEnumerable<Type> GetTypesUsedBy(PropertyInfo property) => new[] { property.PropertyType };
}