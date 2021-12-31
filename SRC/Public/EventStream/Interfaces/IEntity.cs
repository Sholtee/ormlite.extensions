/********************************************************************************
* IEntity.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// The base interface of complex entities.
    /// </summary>
    public interface IEntity<TStreamId> where TStreamId : IEquatable<TStreamId>
    {
        /// <summary>
        /// The id of stream that describes the current entity.
        /// </summary>
        public TStreamId StreamId { get; init; }
    }
}
