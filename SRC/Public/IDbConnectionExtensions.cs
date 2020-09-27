/********************************************************************************
* IDbConnectionExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;

namespace Solti.Utils.OrmLite.Extensions
{
    using Internals;

    /// <summary>
    /// Defines several extensions related to <see cref="IDbConnection"/>.
    /// </summary>
    public static class IDbConnectionExtensions
    {
        /// <summary>
        /// Creates a bulked database connection.
        /// </summary>
        public static IBulkedDbConnection CreateBulkedDbConnection(this IDbConnection connection) => new BulkedDbConnection(connection ?? throw new ArgumentNullException(nameof(connection)));
    }
}