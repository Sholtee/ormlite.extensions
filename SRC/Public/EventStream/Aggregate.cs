/********************************************************************************
* Aggregate.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// The base interface of materialized views.
    /// </summary>
    public interface IAggregate<TStreamId>
    {
        /// <summary>
        /// The id of stream from which this view has been created.
        /// </summary>
        public TStreamId StreamId { get; set; }
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1>: IAggregate<TStreamId> where TEvent1 : class
    {
        /// <summary>
        /// The id of stream from which this view has been created.
        /// </summary>
        public TStreamId StreamId { get; set; }

        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent1"/> specific mutations.
        /// </summary>
        public abstract void Apply(TEvent1 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2>: Aggregate<TStreamId, TEvent1> where TEvent1: class where TEvent2 : class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent2"/> specific mutations.
        /// </summary>
        public abstract void Apply(TEvent2 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2, TEvent3> : Aggregate<TStreamId, TEvent1, TEvent2> where TEvent1 : class where TEvent2 : class where TEvent3 : class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent3"/> specific mutations.
        /// </summary>
        public abstract void Apply(TEvent3 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2, TEvent3, TEvent4> : Aggregate<TStreamId, TEvent1, TEvent2, TEvent3> where TEvent1 : class where TEvent2 : class where TEvent3 : class where TEvent4 : class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent4"/> specific mutations.
        /// </summary>
        public abstract void Apply(TEvent4 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> : Aggregate<TStreamId, TEvent1, TEvent2, TEvent3, TEvent4> where TEvent1 : class where TEvent2 : class where TEvent3 : class where TEvent4 : class where TEvent5 : class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent5"/> specific mutations.
        /// </summary>
        public abstract void Apply(TEvent5 evt);
    }

    /// <summary>
    /// The base class of materialized views.
    /// </summary>
    public abstract class Aggregate<TStreamId, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> : Aggregate<TStreamId, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> where TEvent1 : class where TEvent2 : class where TEvent3 : class where TEvent4 : class where TEvent5 : class where TEvent6 : class
    {
        /// <summary>
        /// When implemented in the derived class it does the <typeparamref name="TEvent6"/> specific mutations.
        /// </summary>
        public abstract void Apply(TEvent6 evt);
    }
}
