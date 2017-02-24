using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net;

namespace MWF.Mobile.Core.Database
{

    public interface IConnection
    {
        string DatabasePath { get; }
        IEnumerable<T> Table<T>() where T : class;
        int CreateTable<T>();
        int Insert(object obj);
        int Delete(object objectToDelete);
        int DeleteAll<T>();
        int Execute(string query, params object[] args);
        TableMapping GetMapping(Type type);
        IList<object> Query(TableMapping map, string query, params object[] args);
    }

}
