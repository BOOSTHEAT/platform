using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects;

namespace ImpliciX.RuntimeFoundations.Tests.FactoryTests.Elements
{
    public class local_private_node:PrivateModelNode
    {
        public local_private_node(ModelNode parent) : base(nameof(local_private_node), parent)
        {
            
        }
        public PropertyUrn<Temperature> my_secret_temp => PropertyUrn<Temperature>.Build(Urn, nameof(my_secret_temp));
        
        public CommandNode<DummyValueObject> _dummy_cmd => 
            CommandNode<DummyValueObject>.Create("DUMMY_CMD",this);
    }
}