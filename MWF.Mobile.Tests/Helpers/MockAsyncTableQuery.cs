using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Database;
using System.Linq.Expressions;
using System.Threading;

namespace MWF.Mobile.Tests.Helpers
{
    public class MockAsyncTableQuery<T> : Core.Database.IAsyncTableQuery<T>
        where T : class
    {

        private readonly IEnumerable<T> _list;

        public MockAsyncTableQuery(IEnumerable<T> items)
        {
            _list = items;
        }

        public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(_list.ToList());
        }

        public IAsyncTableQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            return new MockAsyncTableQuery<T>(_list.AsQueryable().Where(predExpr));
        }

    }
}
