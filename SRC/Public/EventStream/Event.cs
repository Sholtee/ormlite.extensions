/********************************************************************************
* Event.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using ServiceStack.DataAnnotations;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CA1716 // Identifiers should not match keywords
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// Describes a generic event
    /// </summary>
    /// <typeparam name="TStreamId">The type of stream id. If you need human readable id (for e.g. in case of invoices) it should be <see cref="string"/>, in any other cases it should be <see cref="Guid"/>.</typeparam>
    [CompositeIndex(nameof(StreamId), nameof(CreatedAtUtc), Unique = true)]
    public abstract class Event<TStreamId> where TStreamId : IEquatable<TStreamId>
    {
        /// <summary>
        /// The unique id of the event
        /// </summary>
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        /// <summary>
        /// The unique id of the underlying stream.
        /// </summary>
        [Required]    
        public TStreamId StreamId { get; set; }

        /// <summary>
        /// The creation time.
        /// </summary>
        public long CreatedAtUtc { get; set; }

        /// <summary>
        /// The type of the payload.
        /// </summary>
        [Required, StringLength(512)]
        public string Type { get; set; }

        /// <summary>
        /// The serialized payload.
        /// </summary>
        [Required, StringLength(StringLengthAttribute.MaxText)]
        public string Payload { get; set; }
    }
}
