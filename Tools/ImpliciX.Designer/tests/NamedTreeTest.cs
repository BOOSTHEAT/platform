using System.Linq;
using ImpliciX.Designer.ViewModels;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests
{
  [TestFixture]
  public class NamedTreeTests
  {
    [Test]
    public void tree_can_have_only_root()
    {
      var tree = new NamedTree(new NamedModel("SingleRoot"));
      Check.That(tree.Parent.Name).IsEqualTo("SingleRoot");
      Check.That(tree.Children).IsEmpty();
    }

    [Test]
    public void tree_root_can_have_child_leaves()
    {
      var tree = new NamedTree(
        new NamedModel("root"),
        new NamedModel[] {
          new NamedModel("foo"),
          new NamedModel("bar"),
          new NamedModel("qix")
        }
      );
      Check.That(tree.Parent.Name).IsEqualTo("root");
      Check.That(tree.Children).CountIs(3);
      Check.That(tree.Children.Select(c => c.Parent.Name)).ContainsExactly("bar","foo","qix");
    }

    [Test]
    public void tree_root_cannot_have_children_starting_with_same_name()
    {
      var tree = new NamedTree(
        new NamedModel("root"),
        new NamedModel[] {
          new NamedModel("foo:fizz"),
          new NamedModel("foo:buzz"),
          new NamedModel("foo"),
          new NamedModel("bar"),
          new NamedModel("qix")
        }
      );
      Check.That(tree.Parent.Name).IsEqualTo("root");
      Check.That(tree.Children).CountIs(3);
      Check.That(tree.Children.Select(c => c.Parent.Name)).ContainsExactly("bar","foo","qix");
    }

    [Test]
    public void children_with_same_starting_name_are_grandchildren()
    {
      var tree = new NamedTree(
        new NamedModel("root"),
        new NamedModel[] {
          new NamedModel("foo:fizz"),
          new NamedModel("foo:buzz"),
          new NamedModel("foo"),
          new NamedModel("bar"),
          new NamedModel("qix")
        }
      );
      var fooTree = tree.Children.ElementAt(1);
      Check.That(fooTree.Parent.Name).IsEqualTo("foo");
      Check.That(fooTree.Children).CountIs(2);
      Check.That(fooTree.Children.Select(c => c.Parent.Name)).ContainsExactly("foo:buzz","foo:fizz");
      Check.That(fooTree.Children.Select(c => c.Parent.DisplayName)).ContainsExactly(":buzz",":fizz");
    }

    [Test]
    public void children_with_same_starting_name_have_special_display_name()
    {
      var tree = new NamedTree(
        new NamedModel("root"),
        new NamedModel[] {
          new NamedModel("foo:fizz"),
          new NamedModel("foo:buzz"),
          new NamedModel("foo"),
          new NamedModel("bar"),
          new NamedModel("qix")
        }
      );
      Check.That(tree.Parent.DisplayName).IsEqualTo("root");
      var fooTree = tree.Children.ElementAt(1);
      Check.That(fooTree.Parent.DisplayName).IsEqualTo("foo");
      Check.That(fooTree.Children.Select(c => c.Parent.DisplayName)).ContainsExactly(":buzz",":fizz");
    }
    
    [Test]
    public void tree_with_subtrees()
    {
      var group1 = new NamedTree(
        new NamedModel("group1"),
        new NamedModel[] {
          new NamedModel("foo:fizz"),
          new NamedModel("foo:buzz"),
          new NamedModel("foo"),
          new NamedModel("bar"),
          new NamedModel("qix")
        }
      );
      var group2 = new NamedTree(
        new NamedModel("group2"),
        new NamedModel[] {
          new NamedModel("foo:fizz"),
          new NamedModel("foo:buzz"),
          new NamedModel("foo"),
          new NamedModel("bar"),
          new NamedModel("qix")
        }
      );
      var tree = new NamedTree(
        new NamedModel("root"),
        group1,
        group2
      );

      Check.That(tree.Parent.Name).IsEqualTo("root");
      Check.That(tree.Children).CountIs(2);
      Check.That(tree.Children.Select(c => c.Parent.Name)).ContainsExactly("group1","group2");
      Check.That(tree.Children.Select(c => c.Parent.DisplayName)).ContainsExactly("group1","group2");
      var group1Tree = tree.Children.First();
      Check.That(group1Tree.Parent.Name).IsEqualTo("group1");
      Check.That(group1Tree.Children).CountIs(3);
      Check.That(group1Tree.Children.Select(c => c.Parent.Name)).ContainsExactly("bar","foo","qix");
      Check.That(group1Tree.Children.Select(c => c.Parent.DisplayName)).ContainsExactly("bar","foo","qix");
      var group1FooTree = group1Tree.Children.ElementAt(1);
      Check.That(group1FooTree.Parent.Name).IsEqualTo("foo");
      Check.That(group1FooTree.Children).CountIs(2);
      Check.That(group1FooTree.Children.Select(c => c.Parent.Name)).ContainsExactly("foo:buzz","foo:fizz");
      Check.That(group1FooTree.Children.Select(c => c.Parent.DisplayName)).ContainsExactly(":buzz",":fizz");

    }

  }
}