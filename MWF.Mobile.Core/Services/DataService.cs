using SQLite.Net;
using SQLite.Net.Async;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using System;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Cirrious.CrossCore;

namespace MWF.Mobile.Core.Services
{

    public class DataService : IDataService
    {

        #region Private Members

        private readonly IDeviceInfo _deviceInfo = null;
        private const string DBNAME = "db.sql";
        private SQLite.Net.SQLiteConnectionString _connectionString = null;
        private SQLite.Net.Interop.ISQLitePlatform _platform = null;
        private string _path = "";
        private SQLiteConnectionPool _connectionPool = null;

        #endregion

        #region Construction

        public DataService(IDeviceInfo deviceInfo, SQLite.Net.Interop.ISQLitePlatform platform)
        {
            _deviceInfo = deviceInfo;
            _platform = platform;
            _path = Path.Combine(_deviceInfo.DatabasePath, DBNAME);
            _connectionString = new SQLiteConnectionString(_path, true);
            _connectionPool = new SQLiteConnectionPool(_platform);

            this.CreateTablesIfRequired();
        }

        #endregion

        #region Public Methods

        public async Task RunInTransactionAsync(Action<Database.IConnection> action)
        {
            var conn = this.GetAsyncDBConnection();
            await conn.RunInTransactionAsync(action);
        }

        #endregion

        #region Properties

        public virtual Database.IConnection GetDBConnection()
        {
            var _conn = new Database.Connection(_platform, _path, SQLite.Net.Interop.SQLiteOpenFlags.ReadWrite | SQLite.Net.Interop.SQLiteOpenFlags.Create | SQLite.Net.Interop.SQLiteOpenFlags.FullMutex);            
            return _conn;
        }

        public virtual Database.IAsyncConnection GetAsyncDBConnection()
        {
            return new Database.AsyncConnection(() => _connectionPool.GetConnection(_connectionString));
        }

        public string DatabasePath
        {
            get { return GetDBConnection().DatabasePath; }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates all the tables required for local storage.
        /// If tables already exist connection.CreateTable handles
        /// this gracefully
        /// </summary>
        private void CreateTablesIfRequired()
        {
            var connection = this.GetDBConnection();
            connection.CreateTable<Additional>();
            connection.CreateTable<Models.Instruction.Address>();
            connection.CreateTable<ApplicationProfile>();
            connection.CreateTable<ConfirmQuantity>();
            connection.CreateTable<CurrentDriver>();
            connection.CreateTable<Customer>();
            connection.CreateTable<DeliveryDescription>();
            connection.CreateTable<Device>();
            connection.CreateTable<Driver>();
            connection.CreateTable<GatewayQueueItem>();
            connection.CreateTable<Instruction>();
            connection.CreateTable<Image>();
            connection.CreateTable<ItemAdditional>();
            connection.CreateTable<Item>();
            connection.CreateTable<LatestSafetyCheck>();
            connection.CreateTable<LogMessage>();
            connection.CreateTable<MWFMobileConfig>();
            connection.CreateTable<MobileData>();
            connection.CreateTable<Order>();
            connection.CreateTable<SafetyCheckData>();
            connection.CreateTable<SafetyCheckFault>();
            connection.CreateTable<SafetyCheckFaultType>();
            connection.CreateTable<SafetyProfile>();
            connection.CreateTable<Signature>();
            connection.CreateTable<Vehicle>();
            connection.CreateTable<Models.Trailer>();
            connection.CreateTable<Models.Instruction.Trailer>();
            connection.CreateTable<VehicleView>();
            connection.CreateTable<VerbProfile>();
            connection.CreateTable<VerbProfileItem>();
        }

        #endregion


    }

}
