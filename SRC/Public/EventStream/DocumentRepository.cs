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
    public class DocumentRepository<TStreamId, TDocument, TView>: IDocumentRepository<TStreamId, TView> where TStreamId : IEquatable<TStreamId> where TView : IEntity<TStreamId>, new() where TDocument: Document<TStreamId>, new()
    {
        /// <summary>
        /// SQL function that concatenates a string group [GROUP_CONCAT() || STRING_AGG()]
        /// </summary>
        protected virtual string GroupConcat(string quotedColumn, string quotedSeparator) => $"GROUP_CONCAT({quotedColumn}, {quotedSeparator})";

        /// <summary>
        /// SQL function that extracts a value from a JSON.
        /// </summary>
        protected virtual string JsonExtract(string quotedColumn, string quotedJsonPath) => $"JSON_EXTRACT({quotedColumn}, {quotedJsonPath})";

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

        /// <inheritdoc/>
        public virtual async Task InsertOrUpdate(TView view, CancellationToken cancellation)
        {
            if (view is null)
                throw new ArgumentNullException(nameof(view));

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

        /// <inheritdoc/>
        public virtual async Task<IList<TView>> QueryBySimpleCondition<TProperty>(string jsonPath, Expression<Func<TProperty, bool>> predicate, CancellationToken cancellation) where TProperty : IEquatable<TProperty>
        {
            if (jsonPath is null)
                throw new ArgumentNullException(nameof(jsonPath));

            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            IOrmLiteDialectProvider dialectProvider = Connection.GetDialectProvider();

            string dataColumn = typeof(TDocument)
                .GetModelMetadata()
                .FieldDefinitions
                .Single(f => f.PropertyInfo.Name == nameof(Document<TStreamId>.Payload))
                .GetQuotedName(dialectProvider);

            Expression<Func<TDocument, bool>> where = Expression.Lambda<Func<TDocument, bool>>
            (
                new ParameterReplacerVisitor
                (
                    ((Expression<Action>) (() => Sql.Custom<TProperty>(JsonExtract(dataColumn, dialectProvider.GetQuotedValue(jsonPath))))).Body
                ).Visit(predicate.Body), 
                Expression.Parameter(typeof(TDocument), "_")
            );

            string 
                sql = Connection
                    .From<TDocument>()
                    //
                    // 1) Ide elmeletileg jo lenne a "SELECT JSON_ARRAY(Payload)" is viszont az SQLite alatt rossz
                    //    eredmenyt ad (Payload-ot JSON karakterlanckent adja vissza), MySQL alatt megy.
                    // 2) Ezert kiszolgalo oldalon "kezzel" rakjuk ossze a JSON string-et, viszont itt meg megszopat az
                    //    OrmLite mivel a ".Select(doc => '[' + Sql.Custom('...') + ']')" rossz kifejezest eredmenyez
                    //    (https://github.com/ServiceStack/ServiceStack.OrmLite#querying-with-select)
                    //

                    .Select(doc => Sql.Custom(dialectProvider.GetQuotedValue("[")) + (Sql.Custom(GroupConcat(dataColumn, dialectProvider.GetQuotedValue(","))) ?? "") + Sql.Custom(dialectProvider.GetQuotedValue("]")))
                    .Where(doc => doc.Type.StartsWith(typeof(TView).FullName))
                    .And(where)
                    .ToMergedParamsSelectStatement(),
                payload = await Connection.ScalarAsync<string>(sql, token: cancellation);

            return JsonSerializer.Deserialize<TView[]>(payload)!;
        }
    }
}
