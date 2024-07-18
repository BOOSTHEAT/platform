using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree
{
    public class processing : SubSystemNode
    {
        public processing(string name, ModelNode parent) : base(name, parent)
        {
            addition = VersionSettingUrn<FunctionDefinition>.Build(Urn, nameof(addition));
            presence = VersionSettingUrn<Presence>.Build(Urn, nameof(presence));
        }

        public VersionSettingUrn<FunctionDefinition> addition { get; }
        public VersionSettingUrn<Presence> presence { get; }
    }
}
