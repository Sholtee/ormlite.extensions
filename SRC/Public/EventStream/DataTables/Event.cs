/********************************************************************************
* Event.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using ServiceStack.DataAnnotations;

#pragma warning disable CA1716 // Identifiers should not match keywords

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// Describes a generic event
    /// </summary>
    /// <typeparam name="TStreamId">The type of stream id. If you need human readable id (for e.g. in case of invoices) it should be <see cref="string"/>, in any other cases it should be <see cref="Guid"/>.</typeparam>
    public abstract class Event<TStreamId>: SerializedData<TStreamId> where TStreamId : IEquatable<TStreamId>
    {
        /// <summary>
        /// The unique id of the event
        /// </summary>
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        /// <summary>
        /// The creation time.
        /// </summary>
        [Required]
        public long CreatedAtUtc { get; set; }
    }
}
