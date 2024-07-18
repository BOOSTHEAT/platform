using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Structures
{
    public class TreeBuilder<T>
    {
        private static TreeNode<T> AddToRootNode(TreeNode<T> rootNode, TreeNode<T> node)
        {
            rootNode.AddChild(node);
            return rootNode;
        }
        
        public static Tree<T> Build<U>(IEnumerable<U> flatData,Func<U,Option<T>> nodeSelector, Func<U,Option<T>> parentSelector) where U:IEquatable<U>
        {
            var hierarchicalList = flatData.Select(item => new TreeNode<T>(nodeSelector(item))).ToList();
            foreach (var item in flatData)
            {
                var parentNode = parentSelector(item);
                if (parentNode.IsSome)
                {
                    var parentTreeNode = hierarchicalList.SingleOrDefault(c => c.Data.Equals(parentNode));
                    var childTreeNode = hierarchicalList.SingleOrDefault(c => c.Data.Equals(nodeSelector(item)));
                    parentTreeNode.AddChild(childTreeNode);
                }
            }

            var root = hierarchicalList
                .Where(c => c.IsRoot)
                .Aggregate(new TreeNode<T>(Option<T>.None()), AddToRootNode);
            return new Tree<T>(root);
        }
    }
}