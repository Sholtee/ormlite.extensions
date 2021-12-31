﻿/********************************************************************************
* DocumentRepository.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// Represents the base class of document repositories.
    /// </summary>
    public class DocumentRepository<TStreamId, TDocument, TView> where TStreamId : IEquatable<TStreamId> where TView : new() where TDocument: Document<TStreamId, TView>, new()
    {
        /// <summary>
        /// The database connection.
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Creates a new <see cref="DocumentRepository{TStreamId, TDocument, TView}"/> instance.
        /// </summary>
        public DocumentRepository(IDbConnection connection) => Connection = connection ?? throw new ArgumentNullException(nameof(connection));

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
                .Single(f => f.PropertyInfo.Name == nameof(Document<TStreamId, TView>.SerializedData))
                .GetQuotedName(dialectProvider);

            // (BinaryExpression) ((Expression<Func<bool>>) (() => Sql.JsonValue(dataColumn, jsonPath) == "")).Body,
            Expression<Action> extract = () => Sql.Custom<TProperty>($"JSON_EXTRACT({dataColumn}, {dialectProvider.GetQuotedValue(jsonPath)})");

            BinaryExpression        
                original = (BinaryExpression) predicate.Body,
                updated  = original.Update(extract.Body, original.Conversion, original.Right);

            string sql = Connection.From<TDocument>()
                .Select()
                .Where(Expression.Lambda<Func<TDocument, bool>>(updated, Expression.Parameter(typeof(TDocument), "_")))
                .ToMergedParamsSelectStatement();

            return (await Connection.SelectAsync<TDocument>(sql, cancellation))
                .Select(doc => doc.Data!)
                .ToList();
        }
    }
}