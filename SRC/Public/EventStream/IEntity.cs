/********************************************************************************
* IEntity.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// The base interface of complex entities.
    /// </summary>
    public interface IEntity<TStreamId>
    {
        /// <summary>
        /// The id of stream that describes the current entity.
        /// </summary>
        public TStreamId StreamId { get; init; }
    }
}
