/********************************************************************************
* Aggregate.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1>: IEntity<TStreamId> where TStreamId : IEquatable<TStreamId> where TEvent1 : class
    {
        /// <summary>
        /// The id of stream that describes the current entity.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TStreamId StreamId { get; init; }
#pragma warning restore CS8618

        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent1"/> specific mutations.
        /// </summary>
        /// <returns>The new state of the entity.</returns>
        public abstract void Apply(TEvent1 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2>: Aggregate<TStreamId, TEvent1> where TStreamId : IEquatable<TStreamId> where TEvent1: class where TEvent2 : class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent2"/> specific mutations.
        /// </summary>
        /// <returns>The new state of the entity.</returns>
        public abstract void Apply(TEvent2 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2, TEvent3> : Aggregate<TStreamId, TEvent1, TEvent2> where TStreamId : IEquatable<TStreamId> where TEvent1 : class where TEvent2 : class where TEvent3 : class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent3"/> specific mutations.
        /// </summary>
        /// <returns>The new state of the entity.</returns>
        public abstract void Apply(TEvent3 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2, TEvent3, TEvent4> : Aggregate<TStreamId, TEvent1, TEvent2, TEvent3> where TStreamId : IEquatable<TStreamId> where TEvent1 : class where TEvent2 : class where TEvent3 : class where TEvent4: class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent4"/> specific mutations.
        /// </summary>
        /// <returns>The new state of the entity.</returns>
        public abstract void Apply(TEvent4 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> : Aggregate<TStreamId, TEvent1, TEvent2, TEvent3, TEvent4> where TStreamId : IEquatable<TStreamId> where TEvent1 : class where TEvent2 : class where TEvent3 : class where TEvent4 : class where TEvent5: class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent5"/> specific mutations.
        /// </summary>
        /// <returns>The new state of the entity.</returns>
        public abstract void Apply(TEvent5 evt);
    }
}
