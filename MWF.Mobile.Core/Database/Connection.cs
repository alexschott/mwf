using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite.Net;
using SQLite.Net.Interop;

namespace MWF.Mobile.Core.Database
{

    /// <summary>
    /// Wrapper class around SQLiteConnection, allowing implementation of an interface to make it testable.
    /// </summary>
    /// <remarks>Other methods of SQLiteConnection may need to be added to this class and the IConnection interface - only the ones currently used have been added so far.</remarks>
    internal class Connection : IConnection, IDisposable
    {

        private readonly SQLiteConnection _connection;

        public Connection(SQLite.Net.Interop.ISQLitePlatform platform, string databasePath, SQLiteOpenFlags openFlags)
        {
            _connection = new SQLiteConnection(platform, databasePath, openFlags);
        }

        internal Connection(SQLiteConnection sqliteConnection)
        {
            _connection = sqliteConnection;
        }

        public string DatabasePath
        {
            get { return _connection.DatabasePath; }
        }

        public int CreateTable<T>()
        {
            return _connection.CreateTable<T>();
        }

        /// <summary>
        /// Retrieve a table's data. This method is only intended to be used within the base Repository class inside the UpdateAsync() method.
        /// Other code should instead use the Table<T>() method of IAsyncConnection.
        /// </summary>
        public IEnumerable<T> Table<T>() where T : class
        {
            return _connection.Table<T>();
        }

        public int Insert(object obj)
        {
            return _connection.Insert(obj);
        }

        public int DeleteAll<T>()
        {
            return _connection.DeleteAll<T>();
        }

        public int Delete(object objectToDelete)
        {
            return _connection.Delete(objectToDelete);
        }

        public int Execute(string query, params object[] args)
        {
            return _connection.Execute(query, args);
        }

        public TableMapping GetMapping(Type type)
        {
            return _connection.GetMapping(type);
        }

        public IList<object> Query(TableMapping map, string query, params object[] args)
        {
            return _connection.Query(map, query, args);
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }

    }

}
