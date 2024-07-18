using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Structures;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class TreeTests
    {
        [Test]
        public void find_node()
        {
            var results = ExampleTree.Find((n) => n.StartsWith("B") && n.Length==2);
            Check.That(results).ContainsExactly("B*", "BB");
        }
        
        [TestCase("/", true)]
        [TestCase("A", true)]
        [TestCase("Schtroumph", false)]
        public void get_tree_node(string node, bool expected)
        {
            var treeNode = ExampleTree.GetTreeNode(node);
            Check.That(treeNode.IsSome).IsEqualTo(expected);
        }
        
        [Test]
        public void find_subnodes_of_node()
        {
            var results = ExampleTree.SubNodesOf("B*",(s) => s.EndsWith("*"));
            Check.That(results).ContainsExactly("BA*", "BAA*");
        }

        [TestCase("/",0)]
        [TestCase("A",1)]
        [TestCase("B*",2)]
        [TestCase("BAA*",4)]
        [TestCase("Schtroumpf",-1)]
        public void compute_depth_of_node(string node, int expected)
        {
            var depth = ExampleTree.DepthOf(node);
            Check.That(depth).IsEqualTo(expected);
        }

        [Test]
        public void ancestors_simple_tree()
        {
            var ancestors = SimpleTree.AncestorsOf("A");
            Check.That(ancestors).IsEmpty();
        }
        
        [TestCase("A",new string[]{"/"})]
        [TestCase("B*",new string[]{"A","/"})]
        [TestCase("BAA*",new string[]{"BA*","B*","A","/"})]
        [TestCase("Schtroumpf",new string[]{})]
        public void ancestors_of_node(string node, string[] expected)
        {
            var ancestors = ExampleTree.AncestorsOf(node);
            Check.That(ancestors).ContainsExactly(expected);
        }

        [TestCase("BAA*",(uint)5,new string[]{})]
        [TestCase("BAA*",(uint)3,new string[]{"BA*"})]
        [TestCase("BAA*",(uint)2,new string[]{"BA*","B*"})]
        [TestCase("BAA*",(uint)0,new string[]{"BA*","B*","A","/"})]
        public void ancestors_with_depth_limit(string node, uint depthLimit, string[] expected)
        {
            var ancestors = ExampleTree.AncestorsOf(node,depthLimit);
            Check.That(ancestors).ContainsExactly(expected);
        }

        [TestCase("B*",true)]
        [TestCase("A",false)]
        [TestCase("BAA*",true)]
        
        public void has_siblings(string node, bool expected)
        {
            var result = ExampleTree.HasSiblings(node);
            Check.That(result).IsEqualTo(expected);


        }

        [Test]
        public void traverse_test()
        {
            var visitedNodes = new List<string>();
            ExampleTree.Traverse(n=>visitedNodes.Add(n.Data.GetValueOrDefault("")));
            Check.That(visitedNodes).ContainsExactly("/","A","B*","BA*","BAA*","BAB","BB","C");
        }

        [Test]
        public void find_first_common_ancestor()
        {
            var result = ExampleTree.FirstCommonAncestor("BAA*", "BB");
            Check.That(result.Data).IsEqualTo(Option<string>.Some("B*"));
        }

        [SetUp]
        public void Init()
        {
            var root = new TreeNode<string>("/");
            {
                var A = root.AddChild("A");
                {
                    var B = A.AddChild("B*");
                    {
                        var BA = B.AddChild("BA*");
                        {
                            var BAA = BA.AddChild("BAA*");
                            var BAB = BA.AddChild("BAB");
                        }
                        var BB = B.AddChild("BB");
                    }
                    var C = A.AddChild("C");
                }
            }
            ExampleTree = new Tree<string>(root);

            var r = new TreeNode<string>();
            {
                r.AddChild("A");
                r.AddChild("B");
            }
            SimpleTree = new Tree<string>(r);
            
        }

        public Tree<string> SimpleTree { get; set; }

        public Tree<string> ExampleTree { get; set; }
    }
}