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
        /// Returns all the materialized views.
        /// </summary>
        /// <remarks>This method replays all the related events to build the result, therefore it's considered inefficient. To query views use the <see cref="Document{TStreamId}"/> class.</remarks>
        Task<IList<TView>> QueryViews(CancellationToken cancellation = default);

        /// <summary>
        /// Returns the materialized views identified by their primary keys.
        /// </summary>
        /// <remarks>This method replays all the related events to build the result, therefore it's considered inefficient. To query views use the <see cref="Document{TStreamId}"/> class.</remarks>
        Task<IList<TView>> QueryViewsByStreamId(CancellationToken cancellation = default, params TStreamId[] ids);
    }
}
