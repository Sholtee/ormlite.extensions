/********************************************************************************
* DataTableAttribute.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.OrmLite.Extensions
{
    /// <summary>
    /// Marks a <see cref="Type"/> to be used as a data table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DataTableAttribute: Attribute
    {
    }
}
