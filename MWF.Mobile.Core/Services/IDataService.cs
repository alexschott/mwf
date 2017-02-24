using System;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{
    public interface IDataService
    {
        Database.IConnection GetDBConnection();
        Database.IAsyncConnection GetAsyncDBConnection();
        Task RunInTransactionAsync(Action<Database.IConnection> action);
        string DatabasePath { get; }
    }
}
