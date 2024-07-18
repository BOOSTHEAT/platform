
using ImpliciX.Language.Model;

namespace ImpliciX.PersistentStore.Tests
{
    public class dummy:RootModelNode
    {
        public dummy() : base(nameof(dummy))
        {
        }

        public static CommandNode<NoArg> _clean_version_settings => CommandNode<NoArg>.Create("clean", new dummy());
        public static settings settings => new settings(new dummy());
    }

    public class enumCounters : ModelNode
    {
        public enumCounters(ModelNode parent) : base(nameof(enumCounters), parent)
        {
        }
    }

    public class settings:ModelNode
    {
        public  VersionSettingUrn<Temperature> my_temperature => VersionSettingUrn<Temperature>.Build(Urn, nameof(my_temperature));
        public  VersionSettingUrn<Percentage> my_percentage => VersionSettingUrn<Percentage>.Build(Urn, nameof(my_percentage));

        public  VersionSettingUrn<Duration> my_timeout => VersionSettingUrn<Duration>.Build(Urn, nameof(my_timeout));
        public  VersionSettingUrn<FunctionDefinition> my_function => VersionSettingUrn<FunctionDefinition>.Build(Urn, nameof(my_function));
        public  UserSettingUrn<Duration> user_timeout => UserSettingUrn<Duration>.Build(Urn, nameof(user_timeout));
        public  UserSettingUrn<Duration> other_timeout => UserSettingUrn<Duration>.Build(Urn, nameof(other_timeout));

        public settings(ModelNode parent) : base(nameof(settings), parent)
        {
        }
    }

}