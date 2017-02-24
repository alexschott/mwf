using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Database
{

    /// <summary>
    /// Wrapper class around SQLite.Net.Async.AsyncTableQuery<T>, allowing implementation of an interface to make it testable.
    /// </summary>
    /// <remarks>Other methods of the underlying AsyncTableQuery may need to be added to this class and the IAsyncTableQuery interface - only the ones currently used have been added so far.</remarks>
    public class AsyncTableQuery<T> : IAsyncTableQuery<T>
        where T : class
    {

        private readonly SQLite.Net.Async.AsyncTableQuery<T> _baseQuery;

        public AsyncTableQuery(SQLite.Net.Async.AsyncTableQuery<T> baseQuery)
        {
            _baseQuery = baseQuery;
        }

        public IAsyncTableQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            return new AsyncTableQuery<T>(_baseQuery.Where(predExpr));
        }

        public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _baseQuery.ToListAsync();
        }

    }

}
