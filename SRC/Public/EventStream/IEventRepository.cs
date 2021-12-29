/********************************************************************************
* EventRepository.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// Describes an abstract event repository.
    /// </summary>
    public interface IEventRepository<TStreamId, TView> where TStreamId : IEquatable<TStreamId> where TView : IEntity<TStreamId>
    {
        /// <summary>
        /// Inserts a new event into the repository.
        /// </summary>
        /// <returns>The modified materialized view.</returns>
        Task<TView> CreateEvent(TStreamId streamId, object evt, CancellationToken cancellation = default);

        /// <summary>
        /// Returns the materialized views.
        /// </summary>
        Task<IList<TView>> QueryViews(CancellationToken cancellation = default);

        /// <summary>
        /// Returns the materialized views identified by their primary keys.
        /// </summary>
        Task<IList<TView>> QueryViewsByStreamId(CancellationToken cancellation = default, params TStreamId[] ids);
    }
}
