using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.SharedKernel.Tools;

public static class UrnExtensions
{
  public static Urn FindRoot(this IEnumerable<Urn> urns)
  {
    var cps = urns.Select(urn => Urn.Deconstruct(urn)).ToArray();
    var maxLen = cps.Select(x => x.Length).Min();
    var len = 0;
    while (len < maxLen && cps.Select(x => x[len]).Distinct().Count() == 1)
      len++;
    return Urn.BuildUrn(cps.First().Take(len).ToArray());
  }
  
  public static Urn Plus(this Urn urn, string newPart) => Urn.BuildUrn(urn, newPart);
}