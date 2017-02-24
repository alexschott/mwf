using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SQLite.Net;
using SQLite.Net.Async;

namespace MWF.Mobile.Core.Database
{

    public interface IAsyncConnection
    {
        Task RunInTransactionAsync(Action<IConnection> action, CancellationToken cancellationToken = default(CancellationToken));
        Task<IList<object>> QueryAsync(CancellationToken cancellationToken, TableMapping map, string sql, params object[] args);
        Task<TableMapping> GetMappingAsync(Type type);
        Task<CreateTablesResult> CreateTableAsync<T>(CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        IAsyncTableQuery<T> Table<T>() where T : class;
        Task<int> ExecuteAsync(string query, params object[] args);
    }

}
