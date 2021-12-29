/********************************************************************************
* EventRepository.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    using Properties;

    /// <summary>
    /// Represents the base class of event repositories.
    /// </summary>
    public class EventRepository<TStreamId, TEvent, TView> where TEvent: Event<TStreamId>, new() where TStreamId: IEquatable<TStreamId> where TView : IEntity<TStreamId>, new()
    {
        #region Private
        private static readonly object FLock = new();

        private static Action<TView, object>? FCallApply;

        private static InvalidOperationException GetUnknownEventError(string evtType) => new(string.Format(Resources.Culture, Resources.UNKNOWN_EVENT, evtType));

        internal static void Apply(TView view, TEvent evt)
        {
            Type? eventType = Type.GetType(evt.Type, throwOnError: false);
            if (eventType is null)
                throw GetUnknownEventError(evt.Type);

            object? realEvent = JsonSerializer.Deserialize(evt.Payload, eventType);
            if (realEvent is null)
                throw new InvalidOperationException(Resources.NULL_EVENT);

            Apply(view, realEvent);
        }

        internal static void Apply(TView view, object evt)
        {
            if (FCallApply is null)
                lock (FLock)
#pragma warning disable CA1508 // Avoid dead conditional code
                    if (FCallApply is null)
#pragma warning restore CA1508
                        FCallApply = GenerateApplyFn();
            FCallApply(view, evt);
        }

        //
        // (view, evt) =>
        // {
        //   switch (evt.GetType())
        //   {
        //     case typeof(TxXx):
        //     {
        //       view.Apply(evt);
        //       break;
        //     }
        //     ...
        //     default:
        //       throw new InvalidOperationEception();
        //   }
        // }
        //

        private static Action<TView, object> GenerateApplyFn()
        {
            ParameterExpression
                view = Expression.Parameter(typeof(TView), nameof(view)),
                evt  = Expression.Parameter(typeof(object), nameof(evt));

            MethodCallExpression
                getType = Expression.Call(evt, nameof(GetType), Array.Empty<Type>());

            return Expression.Lambda<Action<TView, object>>
            (
                Expression.Switch
                (
                    getType,
                    Expression.Throw
                    (
                        Expression.Invoke
                        (
                            Expression.Constant
                            (
                                (Func<string, InvalidOperationException>) GetUnknownEventError
                            ),
                            Expression.Property(getType, nameof(Type.AssemblyQualifiedName))
                        )
                    ),
                    typeof(TView)
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                        .Where(m => m.Name == "Apply" && m.GetParameters().Length == 1)
                        .Select(CreateCase)
                        .ToArray()
                ),
                view,
                evt
            ).Compile();

            SwitchCase CreateCase(MethodInfo apply)
            {
                Type parameterType = apply
                    .GetParameters()
                    .Single()
                    .ParameterType;

                return Expression.SwitchCase
                (
                    Expression.Call
                    (
                        view,
                        apply,
                        Expression.Convert(evt, parameterType)
                    ),
                    Expression.Constant(parameterType)
                );
            }
        }
        #endregion

        /// <summary>
        /// Materializes views.
        /// </summary>
        protected virtual IList<TView> MaterializeViews(IEnumerable<TEvent> events, CancellationToken cancellation) => events.GroupBy(evt => evt.StreamId).Select(evtGrp =>
        {
            cancellation.ThrowIfCancellationRequested();

            TView view = new()
            {
                StreamId = evtGrp.Key
            };

            foreach (TEvent evt in evtGrp.OrderBy(evt => evt.CreatedAtUtc))
            {
                Apply(view, evt);
            }

            return view;
        }).ToList();

        /// <summary>
        /// The database connection.
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Creates a new <see cref="EventRepository{TStreamId, TEvent, TView}"/> instance.
        /// </summary>
        public EventRepository(IDbConnection connection) => Connection = connection ?? throw new ArgumentNullException(nameof(connection));

        /// <summary>
        /// Returns the materialized views identified by their primary keys.
        /// </summary>
        public async Task<IList<TView>> QueryViewsByStreamId(CancellationToken cancellation, params TStreamId[] ids) => MaterializeViews(await Connection.SelectAsync<TEvent>(evt => Sql.In(evt.StreamId, ids), cancellation), cancellation);

        /// <summary>
        /// Returns the materialized views.
        /// </summary>
        public async Task<IList<TView>> QueryViews(CancellationToken cancellation) => MaterializeViews(await Connection.SelectAsync<TEvent>(cancellation), cancellation);

        /// <summary>
        /// Inserts a new event into the repository.
        /// </summary>
        /// <returns>The modified entity.</returns>
        public async Task<TView> CreateEvent(TStreamId streamId, object evt, CancellationToken cancellation)
        {
            if (evt is null)
                throw new ArgumentNullException(nameof(evt));

            TView view = (await QueryViewsByStreamId(cancellation, streamId)).SingleOrDefault() ?? new TView { StreamId = streamId };
            Apply(view, evt); // may throw

            await Connection.InsertAsync(new TEvent 
            {
                CreatedAtUtc = DateTime.UtcNow.Ticks,
                StreamId     = streamId,
                Type         = evt.GetType().AssemblyQualifiedName,
                Payload      = JsonSerializer.Serialize(evt)
            }, token: cancellation);

            return view;
        }
    }
}
