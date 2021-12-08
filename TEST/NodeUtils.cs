/********************************************************************************
* NodeUtils.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace Solti.Utils.OrmLite.Extensions.Tests
{
    using Properties;

    using static Internals.NodeUtils;

    [TestFixture]
    public class NodeUtilsTests
    {
        private class OrderAttribute : Attribute 
        {
            public int Value { get; set; }
        }

        [Order(Value = 0)]
        private class Node0
        {
            public int Irrelevant { get; }
        }

        [Order(Value = 0)]
        private class Node1 
        { 
        }

        [Order(Value = 1)]
        private class Node2_ReferencingNode1 
        {
            [References(typeof(Node1))]
            public int Node1 { get; }
        }

        [Order(Value = 2)]
        private class Node3_ReferencingNode1AndNode2
        {
            [References(typeof(Node1))]
            public int Node1 { get; }

            [References(typeof(Node2_ReferencingNode1))]
            public int Node2 { get; }

            public int Irrelevant { get; }
        }

        public static IEnumerable<ICollection<Type>> Nodes 
        {
            get 
            {
                yield return new[] { typeof(Node0), typeof(Node1) };
                yield return new[] { typeof(Node1), typeof(Node1) };

                yield return new[] { typeof(Node1), typeof(Node2_ReferencingNode1) };
                yield return new[] { typeof(Node2_ReferencingNode1), typeof(Node1)};

                yield return new[] { typeof(Node1), typeof(Node2_ReferencingNode1), typeof(Node3_ReferencingNode1AndNode2) };
                yield return new[] { typeof(Node2_ReferencingNode1), typeof(Node1), typeof(Node3_ReferencingNode1AndNode2) };
                yield return new[] { typeof(Node2_ReferencingNode1), typeof(Node3_ReferencingNode1AndNode2), typeof(Node1) };
                yield return new[] { typeof(Node1), typeof(Node3_ReferencingNode1AndNode2), typeof(Node2_ReferencingNode1) };
                yield return new[] { typeof(Node3_ReferencingNode1AndNode2), typeof(Node1), typeof(Node2_ReferencingNode1) };
                yield return new[] { typeof(Node3_ReferencingNode1AndNode2), typeof(Node2_ReferencingNode1), typeof(Node1) };
            }
        }

        [TestCaseSource(nameof(Nodes))]
        public void Flatten_ShouldSort(ICollection<Type> nodes) 
        {
            IReadOnlyList<Type> result = Flatten(nodes);
            Assert.That(result.SequenceEqual(nodes.Distinct().OrderBy(node => node.GetCustomAttribute<OrderAttribute>().Value)));
        }

        [Test]
        public void Flatten_ShouldThrowOnUnregisteredNode() => Assert.Throws<InvalidOperationException>(() => Flatten(new[] { typeof(Node2_ReferencingNode1) }), Resources.UNKNOWN_NODE);

        private class SelfReferencingNode 
        {
            [References(typeof(SelfReferencingNode))]
            public int Self { get; }

            public int Irrelevant { get; }
        }

        public class SelfReferencingNode1 
        {
            [References(typeof(SelfReferencingNode2))]
            public int Node2 { get; }
        }

        public class SelfReferencingNode2
        {
            public int Irrelevant { get; }

            [References(typeof(SelfReferencingNode1))]
            public int Node1 { get; }
        }

        public static IEnumerable<ICollection<Type>> CircularNodes
        {
            get 
            {
                yield return new[] { typeof(Node0), typeof(SelfReferencingNode) };

                yield return new[] { typeof(SelfReferencingNode1), typeof(SelfReferencingNode2) };
                yield return new[] { typeof(SelfReferencingNode2), typeof(SelfReferencingNode1) };
            }
        }

        [TestCaseSource(nameof(CircularNodes))]
        public void Flatten_ShouldThrowOnCircularReference(ICollection<Type> nodes) => Assert.Throws<InvalidOperationException>(() => Flatten(nodes), Resources.CIRCULAR_REFERENCE);
    }
}
