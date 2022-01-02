/********************************************************************************
* SerializedData.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using ServiceStack.DataAnnotations;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// Defines the base class of data-tables that contain serialized data.
    /// </summary>
    public abstract class SerializedData<TStreamId> where TStreamId : IEquatable<TStreamId>
    {
        /// <summary>
        /// The unique id of the underlying stream.
        /// </summary>
        [Required, Index]
        public virtual TStreamId StreamId { get; set; }

        /// <summary>
        /// The type of the payload.
        /// </summary>
        [Required, StringLength(512)]
        public string Type { get; set; }

        /// <summary>
        /// The serialized payload.
        /// </summary>
        /// <remarks>Since this property is virtual you can apply your own <see cref="CustomFieldAttribute"/> if necessary.</remarks>
        [Required, CustomField("json")]
        public virtual string Payload { get; set; }
    }
}
