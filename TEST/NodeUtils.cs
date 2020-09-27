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
    using Internals;

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
        }


        [Order(Value = 0)]
        private class Node1 
        { 
        }

        [Order(Value = 1)]
        public class Node2_ReferencingNode1 
        {
            [References(typeof(Node1))]
            public int Node1 { get; }
        }

        [Order(Value = 2)]
        public class Node3_ReferencingNode1AndNode2
        {
            [References(typeof(Node1))]
            public int Node1 { get; }

            [References(typeof(Node2_ReferencingNode1))]
            public int Node2 { get; }
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
            ICollection<Type> result = NodeUtils.Flatten(nodes);
            Assert.That(result.SequenceEqual(nodes.Distinct().OrderBy(node => node.GetCustomAttribute<OrderAttribute>().Value)));
        }
    }
}
