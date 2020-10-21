/********************************************************************************
* IBulkedDbConnection.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.OrmLite.Extensions
{
    /// <summary>
    /// Describes a bulked database connection. On bulked connections only write operations are allowed.
    /// </summary>
    public interface IBulkedDbConnection: IDbConnection
    {
        /// <summary>
        /// Executes all the write commands as a statement block against the connection.
        /// </summary>
        /// <returns>Rows affected.</returns>
        int Flush();

        /// <summary>
        /// Executes all the write commands as a statement block against the connection.
        /// </summary>
        /// <returns>Rows affected.</returns>
        Task<int> FlushAsync(CancellationToken cancellation = default);
    }
}
