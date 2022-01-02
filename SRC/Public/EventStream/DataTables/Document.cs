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
    public abstract class Document<TStreamId, TView>: SerializedData<TStreamId> where TStreamId : IEquatable<TStreamId>
    {
        /// <inheritdoc/>
        [PrimaryKey]
        public override TStreamId StreamId { get; set; }

        /// <summary>
        /// The actual data to store.
        /// </summary>
        [Ignore]
        public virtual TView? Data
        { 
            get => JsonSerializer.Deserialize<TView>(Payload); 
            set => Payload = JsonSerializer.Serialize(value); 
        } 
    }
}
