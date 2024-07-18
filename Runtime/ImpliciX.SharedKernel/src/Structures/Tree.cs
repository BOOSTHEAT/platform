using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Structures;

public class Tree<T>
{
    private readonly TreeNode<T> _rootNode;
    private TreeNode<T>[] _flattenNodes;

    public Tree(TreeNode<T> rootNode)
    {
        _rootNode = rootNode;
        _flattenNodes = rootNode.Flatten().ToArray();
    }
    public IEnumerable<T> Find(Func<T, bool> predicate)
    {
        return _flattenNodes
            .Where(n => n.Data.IsSome && predicate(n.Data.GetValue()))
            .Select(n => n.Data.GetValue());
    }
    public IEnumerable<T> SubNodesOf(T node, Func<T, bool> predicate = null)
    {
        predicate ??= (_) => true;
        var treeNode = _flattenNodes.SingleOrDefault(n => n.Data.Equals(node.ToOption()));
        return treeNode
            .SubNodes(predicate)
            .Select(s => s.Data.GetValue());

    }

    public TreeNode<T> FirstCommonAncestor(T data1,T data2)
    {
        var a1 = AncestorsOf(data1);
        var a2 = AncestorsOf(data2);
        var intersections = a1.Intersect(a2).ToList();

        return !intersections.Any() ? _rootNode : GetTreeNode(intersections.First()).GetValue();
    }

    public Option<TreeNode<T>> GetTreeNode(T data)
    {
        return _flattenNodes.SingleOrDefault(n => n.Data.Equals(data.ToOption())).ToOption();
    }

    public bool HasSiblings(T node)
    {
        return (from treeNode in _flattenNodes.SingleOrDefault(n => n.Data.Equals(node.ToOption())).ToOption()
                from parent in treeNode.Parent
                select parent.Children.Count > 1)
            .GetValueOrDefault(false);   
    }
        
    public int DepthOf(T nodeData)
    {
        return GetTreeNode(nodeData).Match(() => -1, _ => AncestorsOf(nodeData).Count());
    }
    public IEnumerable<T> AncestorsOf(T node)
    {
        var treeNode = _flattenNodes.SingleOrDefault(n => n.Data.Equals(node.ToOption()));
        if (treeNode == null) yield break;
        while (!treeNode.IsRoot)
        {
            var compute = from parent in treeNode.Parent
                let nextNode = parent
                from ancestor in parent.Data
                select (ancestor, nextNode);

            if (compute.IsNone) yield break;
                
            yield return compute.GetValue().ancestor;
            treeNode = compute.GetValue().nextNode;
        }
    }
    public IEnumerable<T> AncestorsOf(T node, uint depthLimit)
    {
        var nodeDepth = DepthOf(node);
        return AncestorsOf(node).Take(nodeDepth - (int)depthLimit);
    }
    public void Traverse(Action<TreeNode<T>> visit)
    {
        _rootNode.Traverse(visit);
    }
}