/********************************************************************************
* ValuesAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.OrmLite.Extensions
{
    /// <summary>
    /// Specifies predefined data-row.
    /// </summary>
    /// <remarks>The row will be inserted during schema creation.</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ValuesAttribute : Attribute
    {
        /// <summary>
        /// The values for this data-row.
        /// </summary>
        public IReadOnlyList<object> Items { get; }

        /// <summary>
        /// Creates a new <see cref="ValuesAttribute"/> instance.
        /// </summary>
        /// <param name="values"></param>
        [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
        public ValuesAttribute(params object[] values) => Items = values;

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/>.
        /// </summary>
        public ValuesAttribute() => throw new NotSupportedException();
    }
}
