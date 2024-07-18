using System.Collections.Generic;
using System.Linq;

namespace ImpliciX.Designer.ViewModels
{
  public class NamedTree
  {
    public NamedTree(NamedModel item)
    {
        Parent = item;
        Children = new NamedTree[]{};
    }
    public NamedTree(NamedModel parent, IEnumerable<NamedModel> others)
    {
      Parent = parent;
      var children = others.OrderBy(c => c.Name).Aggregate(new List<NamedModel>(), (take, item) =>
      {
        if (!take.Any(c => item.Name.StartsWith(c.Name)))
          take.Add(item);
        return take;
      });
      Children = children.Select(c => CreateNode(c, others)).ToArray();
      foreach (var child in Children)
        child.Parent.Parent = parent;
    }

    public NamedTree(NamedModel parent, params NamedTree[] subtrees)
    {
      Parent = parent;
      Children = subtrees;
    }

    private static NamedTree CreateNode(NamedModel node, IEnumerable<NamedModel> others)
    {
      var children = others.Where(o => o.Name != node.Name && o.Name.StartsWith(node.Name)).ToArray();
      if(children.Any())
        return new NamedTree(node, children);
      else
        return new NamedTreeLeaf(node);
    }

    public NamedModel Parent { get; private set; }
    public IEnumerable<NamedTree> Children { get; private set; }
  }
    public class NamedTreeLeaf : NamedTree
    {
      public NamedTreeLeaf(NamedModel item) : base(item)
      {
          
      }
    }
}