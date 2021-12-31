/********************************************************************************
* Document.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Text.Json;

using ServiceStack.DataAnnotations;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// Base class for stored views
    /// </summary>
    public abstract class Document<TStreamId, TView> where TStreamId : IEquatable<TStreamId>
    {
        /// <summary>
        /// The id of stream.
        /// </summary>
        [Required, Index(Unique = true)]
        public TStreamId StreamId { get; set; }

        /// <summary>
        /// The serialized data.
        /// </summary>
        /// <remarks>Since this property is virtual you can apply your own <see cref="CustomFieldAttribute"/> if necessary.</remarks>
        [Required, CustomField("json")]
        public virtual string SerializedData { get; set; } = "null";

        /// <summary>
        /// The actual data to store.
        /// </summary>
        [Ignore]
        public virtual TView? Data
        { 
            get => JsonSerializer.Deserialize<TView>(SerializedData); 
            set => SerializedData = JsonSerializer.Serialize(value); 
        } 
    }
}
