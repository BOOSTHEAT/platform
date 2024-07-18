using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Structures;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class TreeBuilderTests
    {

        [Test]
        public void build_dummy_tree()
        {
            var flatData = FlatDummies();
            var tree = TreeBuilder<string>.Build(
                flatData, d => d.Name,
                d => d.Parent);
            var inspections = new List<string>();
            tree.Traverse((t)=>inspections.Add(InspectTreeNode(t)));
            var expected = new List<string>()
            {
                "/ -> / -> [B;C]",
                "/ -> B -> [BA;BB]",
                "B -> BA -> [BAA]",
                "BA -> BAA -> [BAAA]",
                "BAA -> BAAA -> []",
                "B -> BB -> []",
                "/ -> C -> []"
            };
            Check.That(inspections).ContainsExactly(expected);
        }

        [Test]
        public void build_tree_of_structured_data()
        {
            var flatData = FlatFoos();
            var tree = TreeBuilder<Foo>.Build(
                flatData, f => f,
                f => f.Parent);
            var inspections = new List<string>();
            tree.Traverse((t)=>inspections.Add(InspectTreeNode(t)));
            var expected = new List<string>()
            {
                "/ -> / -> [A;B]",
                "/ -> A -> [AA;AB]",
                "A -> AA -> []",
                "A -> AB -> [ABA]",
                "AB -> ABA -> []",
                "/ -> B -> [BA]",
                "B -> BA -> []"
            };
            Check.That(inspections).ContainsExactly(expected);
        }

        public static string InspectTreeNode<T>(TreeNode<T> node)
        {
            var children = node.Children.Select(c => PrintOrDefault(c.Data,"o"));
            var childrenStr = children.Any()? children.Aggregate((m,n)=>$"{m};{n}"):"";
            var parent = PrintOrDefault(node.Parent?.GetValue()?.Data,"/");
            var n = PrintOrDefault(node.Data,"/");
            return $"{parent} -> {n} -> [{childrenStr}]";
        }

        public static string PrintOrDefault<T>(Option<T> data, string defaultValue)
        {
            if (data == null) return defaultValue;
            return data.Match(() => defaultValue, s => s.ToString());
        }
        public Dummy[] FlatDummies()
        {
            var C = new Dummy("C",default(string));
            var B = new Dummy("B",default(string));
            var BA = new Dummy("BA","B");
            var BAA = new Dummy("BAA","BA");
            var BAAA = new Dummy("BAAA","BAA");
            var BB = new Dummy("BB","B");
            return new[] {BAA, B, BA, BB, BAAA, C};
        }

        public Foo[] FlatFoos()
        {
            var A = new Foo("A", Option<Foo>.None());
            var AA = new Foo("AA", A);
            var AB = new Foo("AB", A);
            var ABA = new Foo("ABA", AB);
            var B = new Foo("B", Option<Foo>.None());
            var BA = new Foo("BA", B);
            return new[] {BA, AA, A, ABA, B, AB};
        }
        
        
    }

    public class Foo : IEquatable<Foo>
    {
        public string Name { get; }
        public Option<Foo> Parent { get; }

        public Foo(string name, Option<Foo> parent)
        {
            Name = name;
            Parent = parent;
        }

        public bool Equals(Foo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Equals(Parent, other.Parent);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Foo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Parent);
        }

        public override string ToString()
        {
            return Name;
        }
    }
    public class Dummy : IEquatable<Dummy>
    {
        public string Name { get; }
        public Option<string> Parent { get; }

        public Dummy(string name, string parent)
        {
            Name = name;
            Parent = parent.ToOption();
        }

        public bool Equals(Dummy other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Equals(Parent, other.Parent);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Dummy) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Parent);
        }
    }
}