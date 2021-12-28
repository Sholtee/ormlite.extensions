/********************************************************************************
* EventRepository.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    using Properties;

    /// <summary>
    /// Represents the base class of event repositories.
    /// </summary>
    #pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
    public class EventRepository<TStreamId, TView> where TView : IAggregate<TStreamId>
    #pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        private static readonly object FLock = new();

        private static Action<TView, Event<TStreamId>>? FApplyFn;

        internal static Action<TView, Event<TStreamId>> ApplyFn
        {
            get
            {
                if (FApplyFn is null)
                    lock (FLock)
                        #pragma warning disable CA1508 // Avoid dead conditional code
                        if (FApplyFn is null)
                        #pragma warning restore CA1508
                            FApplyFn = GenerateApplyFn();
                return FApplyFn;
            }
        }

        //
        // (view, evt) =>
        // {
        //   switch (evt.Type)
        //   {
        //     case "xXx":
        //     {
        //       view.Apply(JsonSerializer.Deserialize<TxXx>(evt.Payload));
        //       break;
        //     }
        //     ...
        //     default:
        //       throw new InvalidOperationEception();
        //   }
        // }
        //

        private static Action<TView, Event<TStreamId>> GenerateApplyFn()
        {
            ParameterExpression
                view = Expression.Parameter(typeof(TView), nameof(view)),
                evt  = Expression.Parameter(typeof(Event<TStreamId>), nameof(evt));

            return Expression.Lambda<Action<TView, Event<TStreamId>>>
            (
                Expression.Switch
                (
                    Expression.Property(evt, nameof(Event<TStreamId>.Type)),
                    Expression.Throw
                    (
                        Expression.Invoke
                        (
                            Expression.Constant
                            (
                                (Func<Event<TStreamId>, InvalidOperationException>) GetUnknownEventError
                            ), 
                            evt
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

            static InvalidOperationException GetUnknownEventError(Event<TStreamId> evt) => new(string.Format(Resources.Culture, Resources.UNKNOWN_EVENT, evt.Type));

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
                        Expression.Convert
                        (
                            Expression.Invoke
                            (
                                Expression.Constant((Func<string, Type, JsonSerializerOptions, object?>) JsonSerializer.Deserialize),
                                Expression.Property(evt, nameof(Event<TStreamId>.Payload)),
                                Expression.Constant(parameterType),
                                Expression.Constant(null, typeof(JsonSerializerOptions))
                            ),
                            parameterType
                        )
                    ),
                    Expression.Constant(parameterType.FullName)
                );
            }
        }
    }
}
