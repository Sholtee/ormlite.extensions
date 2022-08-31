/********************************************************************************
* NodeUtils.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ServiceStack.DataAnnotations;

namespace Solti.Utils.OrmLite.Extensions.Internals
{
    using Properties;

    internal class NodeUtils
    {
        /// <summary>
        /// Sorts the given <paramref name="nodes"/> into a linear order where the referenced node appears before referencing node respectively.
        /// </summary>
        public static IReadOnlyList<Type> Flatten(IEnumerable<Type> nodes)
        {
            List<Type> result = new();

            Stack<Type> currentPath = new();

            FlattenCore(nodes);

            return result;

            void FlattenCore(IEnumerable<Type> currentNodes)
            {
                foreach (Type node in currentNodes)
                {
                    //
                    // Ha a csomopontot korabban mar feldolgoztuk akkor nincs dolgunk
                    //

                    if (result.Contains(node))
                        continue;

                    currentPath.Push(node);

                    try
                    {
                        CheckNotCircular();

                        FlattenCore
                        (
                            GetChildren(node)
                        );

                        result.Add(node);
                    }
                    finally
                    {
                        currentPath.Pop();
                    }
                }
            }

            void CheckNotCircular()
            {
                IEnumerable<Type> path = currentPath.Reverse();

                int firstIndex = path
                    .Select(static (item, index) => new
                    {
                        Item  = item,
                        Index = index
                    })
                    .Where(x => x.Item == currentPath.Peek())
                    .FirstOrDefault()?.Index ?? -1;

                if (firstIndex < currentPath.Count - 1)
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.CIRCULAR_REFERENCE, string.Join(" -> ", path.Skip(firstIndex))));
            }

            IEnumerable<Type> GetChildren(Type node)
            {
                foreach (Type? reference in node
                    .GetProperties()
                    .Select(static prop => prop.GetCustomAttribute<ReferencesAttribute>()?.Type))
                {
                    if (reference is null)
                        continue;

                    if (!nodes.Contains(reference))
                        throw new InvalidOperationException(string.Format(Resources.Culture, Resources.UNKNOWN_NODE, node));

                    yield return reference;
                }
            }
        }
    }
}
