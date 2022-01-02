/********************************************************************************
* DocumentRepository.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// Represents the base class of document repositories.
    /// </summary>
    public class DocumentRepository<TStreamId, TDocument, TView> where TStreamId : IEquatable<TStreamId> where TView : IEntity<TStreamId>, new() where TDocument: Document<TStreamId, TView>, new()
    {
        /// <summary>
        /// The database connection.
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Creates a new <see cref="DocumentRepository{TStreamId, TDocument, TView}"/> instance.
        /// </summary>
        public DocumentRepository(IDbConnection connection) => Connection = connection ?? throw new ArgumentNullException(nameof(connection));

        private sealed class ParameterReplacerVisitor: ExpressionVisitor
        {
            private readonly Expression FSubstitute;

            public ParameterReplacerVisitor(Expression substitute) => FSubstitute = substitute;

            protected override Expression VisitParameter(ParameterExpression node) => FSubstitute;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual async Task InsertOrUpdate(TView view, CancellationToken cancellation = default)
        {
            TDocument entity = new()
            {
                StreamId = view.StreamId,
                Payload = JsonSerializer.Serialize(view),
                Type = typeof(TView).AssemblyQualifiedName,
                LastModifiedUtc = DateTime.UtcNow.Ticks
            };

            if (await Connection.UpdateAsync(entity, token: cancellation) is 0)
                await Connection.InsertAsync(entity, token: cancellation);
        }

        /// <summary>
        /// Queries views.
        /// </summary>
        public virtual async Task<IList<TView>> QueryBySimpleCondition<TProperty>(string jsonPath, Expression<Func<TProperty, bool>> predicate, CancellationToken cancellation = default) where TProperty : IEquatable<TProperty>
        {
            if (jsonPath is null)
                throw new ArgumentNullException(nameof(jsonPath));

            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            IOrmLiteDialectProvider dialectProvider = Connection.GetDialectProvider();

            string dataColumn = typeof(TDocument)
                .GetModelMetadata()
                .FieldDefinitions
                .Single(f => f.PropertyInfo.Name == nameof(Document<TStreamId, TView>.Payload))
                .GetQuotedName(dialectProvider);

            jsonPath = dialectProvider.GetQuotedValue(jsonPath);

            Expression<Action> extract = () => Sql.Custom<TProperty>($"JSON_EXTRACT({dataColumn}, {jsonPath})");

            Expression<Func<TDocument, bool>> where = Expression.Lambda<Func<TDocument, bool>>(new ParameterReplacerVisitor(extract.Body).Visit(predicate.Body), Expression.Parameter(typeof(TDocument), "_"));

            string sql = Connection.From<TDocument>()
                //
                // A lentinek mukodnie kene (https://github.com/ServiceStack/ServiceStack.OrmLite#querying-with-select) gyakorlatilag hibas kimenet a vege
                //

                //.Select(doc => "[ " + Sql.Custom($"GROUP_CONCAT({dataColumn}, {dialectProvider.GetQuotedValue(",")})") + " ]")
                .Select(doc => Sql.Custom($"GROUP_CONCAT({dataColumn}, {dialectProvider.GetQuotedValue(",")})"))
                .Where(where)
                .ToMergedParamsSelectStatement();

            string payload = await Connection.ScalarAsync<string>(sql, token: cancellation);
            return JsonSerializer.Deserialize<TView[]>($"[{payload}]")!;
        }
    }
}
