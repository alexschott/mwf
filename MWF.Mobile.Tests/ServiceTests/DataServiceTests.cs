using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.Models;
using SQLite.Net.Attributes;
using Xunit;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace MWF.Mobile.Tests.ServiceTests
{

    public class DataServiceTests
        : MvxIoCSupportingTest
    {

        private Mock<IDeviceInfo> _mockDeviceInfo;
        private SQLite.Net.Interop.ISQLitePlatform _platform;

        protected override void AdditionalSetup()
        {
            base.AdditionalSetup();

            _mockDeviceInfo = new Mock<IDeviceInfo>();
            _mockDeviceInfo.Setup(di => di.DatabasePath).Returns(string.Empty);

            _platform = new SQLite.Net.Platform.Generic.SQLitePlatformGeneric();
        }

        [Fact]
        public void DataService_ConnectionIsNotNull()
        {
            base.ClearAll();

            var dataService = new DataService(_mockDeviceInfo.Object, _platform);
            var connection = dataService.GetDBConnection();

            Assert.NotNull(connection);
        }

        [Fact]
        public void DataService_CreateTables()
        {
            base.ClearAll();

            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var mockConnection = new Mock<Core.Database.IConnection>();

            var mockDataService = new Mock<DataService>(_mockDeviceInfo.Object, _platform);
            mockDataService.CallBase = true;
            mockDataService.Setup(ds => ds.GetDBConnection()).Returns(mockConnection.Object);
            var dataService = mockDataService.Object;

            // Check that the various tables has been created
            mockConnection.Verify(c => c.CreateTable<ApplicationProfile>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<CurrentDriver>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<Customer>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<Driver>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<Device>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<GatewayQueueItem>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<Image>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<SafetyCheckData>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<SafetyCheckFault>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<SafetyProfile>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<Signature>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<Trailer>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<Vehicle>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<VehicleView>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<VerbProfile>(), Times.Once);
            mockConnection.Verify(c => c.CreateTable<VerbProfileItem>(), Times.Once);

        }

    }

}
