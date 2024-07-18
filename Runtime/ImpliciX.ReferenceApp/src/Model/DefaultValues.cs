using System.Collections.Generic;
using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model;

public static class DefaultValues
{
    private const string Disabled = "Disabled";
    private const string Enabled = "Enabled"; //TODO : Replace by nameof(ImpliciX.Language.Model.Presence.Enabled) ??

    public static readonly IDictionary<Urn, (string Name, string Value)[]> UserSettings = new Dictionary<Urn, (string Name, string Value)[]>
    {
        // devices
        { monitoring.pulse_counter.presence, V(Enabled) }
    };

    public static readonly IDictionary<Urn, (string Name, string Value)[]> VersionSettings = new Dictionary<Urn, (string Name, string Value)[]>
    {
        { system.processing.presence, V(Enabled) },
        { system.processing.addition, F(("a10", "1"), ("a01", "1")) },
    };

    private static (string Name, string Value)[] V(string v) => new[] { ("value", v) };
    private static (string Name, string Value)[] F(params (string Name, string Value)[] a) => a;
}