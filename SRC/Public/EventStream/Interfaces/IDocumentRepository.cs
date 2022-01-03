/********************************************************************************
* IDocumentRepository.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.OrmLite.Extensions.EventStream
{
    /// <summary>
    /// Represents an abstract document repository.
    /// </summary>
    public interface IDocumentRepository<TStreamId, TView> where TStreamId : IEquatable<TStreamId> where TView : IEntity<TStreamId>
    {
        /// <summary>
        /// Inserts or updates a view.
        /// </summary>
        Task InsertOrUpdate(TView view, CancellationToken cancellation = default);

        /// <summary>
        /// Returns a list of <typeparamref name="TView"/>s based on a <paramref name="predicate"/>. The <paramref name="predicate"/> is called against a property pointed by a <paramref name="jsonPath"/>.
        /// </summary>
        /// <remarks>The <paramref name="jsonPath"/> must point to a simple [Number, String, Enum] property.</remarks>
        Task<IList<TView>> QueryBySimpleCondition<TProperty>(string jsonPath, Expression<Func<TProperty, bool>> predicate, CancellationToken cancellation = default) where TProperty : IEquatable<TProperty>;
    }
}
