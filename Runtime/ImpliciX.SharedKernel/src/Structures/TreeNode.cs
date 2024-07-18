using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Structures
{
    public class TreeNode<T>
    {
        public Option<T> Data { get; }

        public Option<TreeNode<T>> Parent { get; set; }
        private List<TreeNode<T>> _Children { get; }

        public IReadOnlyList<TreeNode<T>> Children => _Children.AsReadOnly(); 

        public TreeNode()
        {
            _Children = new List<TreeNode<T>>();
            Data = Option<T>.None();
            Parent = Option<TreeNode<T>>.None();
        }

        public TreeNode(Option<T> data) : this()
        {
            Data = data;

        }

        public TreeNode<T> AddChild(T child)
        {
            var childNode = new TreeNode<T>(child) { Parent = this };
            _Children.Add(childNode);
            return childNode;
        }

        public IEnumerable<TreeNode<T>> Flatten()
        {
            return new[] { this }.Concat(_Children.SelectMany(x => x.Flatten()));
        }

        public IEnumerable<TreeNode<T>> SubNodes(Func<T, bool> predicate)
        {
            return _Children
                .Where(c => c.Data.IsSome && predicate(c.Data.GetValue()))
                .Concat(_Children.SelectMany(x => x.SubNodes(predicate)));
        }

        public TreeNode<T> AddChild(TreeNode<T> node)
        {
            _Children.Add(node);
            node.Parent = this;
            return node;
        }

        public bool IsRoot => Parent.IsNone;

        public void Traverse(Action<TreeNode<T>> visit)
        {
            visit(this);
            _Children.ForEach(c=>c.Traverse(visit));
        }
    }
}