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

    /// <summary>
    /// Wrapper class around SQLiteAsyncConnection, allowing implementation of an interface to make it testable.
    /// </summary>
    /// <remarks>Other methods of SQLiteAsyncConnection may need to be added to this class and the IAsyncConnection interface - only the ones currently used have been added so far.</remarks>
    public class AsyncConnection : SQLiteAsyncConnection, IAsyncConnection
    {

        private readonly Func<SQLiteConnectionWithLock> _sqliteConnectionFunc;
        private readonly TaskCreationOptions _taskCreationOptions;
        private readonly TaskScheduler _taskScheduler;

        public AsyncConnection(Func<SQLiteConnectionWithLock> sqliteConnectionFunc, TaskScheduler taskScheduler = null, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None)
            : base(sqliteConnectionFunc, taskScheduler, taskCreationOptions)
        {
            _sqliteConnectionFunc = sqliteConnectionFunc;
            _taskScheduler = taskScheduler;
            _taskCreationOptions = taskCreationOptions;
        }

        public Task RunInTransactionAsync(Action<IConnection> action, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = base.GetConnection();
                var databaseConnection = new Database.Connection(conn);

                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    conn.BeginTransaction();
                    try
                    {
                        action(databaseConnection);
                        conn.Commit();
                    }
                    catch (Exception)
                    {
                        conn.Rollback();
                        throw;
                    }
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<IList<object>> QueryAsync(CancellationToken cancellationToken, TableMapping map, string sql, params object[] args)
        {
            if (sql == null)
            {
                throw new ArgumentNullException("sql");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return (IList<object>)conn.Query(map, sql, args);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<TableMapping> GetMappingAsync(Type type)
        {
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.GetMapping(type);
                }
            }, CancellationToken.None, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public new IAsyncTableQuery<T> Table<T>()
           where T : class
        {
            return new AsyncTableQuery<T>(base.Table<T>());
        }

    }

}
