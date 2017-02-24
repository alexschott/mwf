using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Repositories;
using SQLite.Net.Attributes;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;
using System.Threading;
using SQLite.Net.Async;

namespace MWF.Mobile.Tests.RepositoryTests
{

    // Tests the BaseRepository class (albeit by using a concrete subclass)
    // Note these are fairly basic tests. End to End integration tests using an
    // actual sqlite database are in RepositoryIntegrationTests.cs.

    public class RepositoryTests
        : MvxIoCSupportingTest
    {

        private Mock<Core.Database.IAsyncConnection> _asyncConnectionMock;
        private Mock<Core.Database.IConnection> _connectionMock;
        private IFixture _fixture;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _asyncConnectionMock = new Mock<Core.Database.IAsyncConnection>();
            _connectionMock = new Mock<Core.Database.IConnection>();
            _asyncConnectionMock.Setup(c => c.RunInTransactionAsync(It.IsAny<Action<Core.Database.IConnection>>(), It.IsAny<CancellationToken>()))
                .Callback<Action<Core.Database.IConnection>, CancellationToken>((a, ct) => a.Invoke(_connectionMock.Object))
                .Returns(Task.FromResult(0));

            var dataServiceMock = new Mock<IDataService>();
            dataServiceMock.Setup(ds => ds.GetAsyncDBConnection()).Returns(_asyncConnectionMock.Object);
            dataServiceMock.Setup(ds => ds.RunInTransactionAsync(It.IsAny<Action<Core.Database.IConnection>>()))
                .Callback<Action<Core.Database.IConnection>>(a => a.Invoke(_connectionMock.Object))
                .Returns(Task.FromResult(0));

            _fixture.Register<IDataService>(() => dataServiceMock.Object);
        }

        [Fact]
        public async Task Repository_Insert()
        {
            base.ClearAll();

            var deviceRepository = _fixture.Create<DeviceRepository>();
            var device = _fixture.Create<Device>();

            await deviceRepository.InsertAsync(device);

            // SQL connection should have been hit with an insert
            _connectionMock.Verify(c => c.Insert(It.Is<Device>(d => Object.Equals(d, device))), Times.Once);
        }

        [Fact]
        public async Task Repository_InsertMany()
        {
            base.ClearAll();

            var deviceRepository = _fixture.Create<DeviceRepository>();
            var devices = _fixture.CreateMany<Device>().ToList();

            await deviceRepository.InsertAsync(devices);

            // SQL connection should have been hit with an insert for each device
            _connectionMock.Verify(c => c.Insert(It.IsAny<Device>()), Times.Exactly(devices.Count)); 
        }

        [Fact]
        public async Task Repository_Delete()
        {
            base.ClearAll();

            var deviceRepository = _fixture.Create<DeviceRepository>();
            var device = _fixture.Create<Device>();

            await deviceRepository.DeleteAsync(device);

            // SQL connection should have been hit with a delete
            _connectionMock.Verify(c => c.Delete(It.Is<Device>(d => Object.Equals(d, device))), Times.Once);
        }

        [Fact]
        public async Task Repository_GetAll()
        {
            base.ClearAll();

            var devices = _fixture.Create<Core.Database.IAsyncTableQuery<Device>>();
            _asyncConnectionMock.Setup(c => c.Table<Device>()).Returns(devices);

            var deviceRepository = _fixture.Create<DeviceRepository>();
            var device = _fixture.Create<Device>();

            IEnumerable<Device> devicesOut = await deviceRepository.GetAllAsync();

            // SQL connection should have been hit with a pull from the table
            _asyncConnectionMock.Verify(c => c.Table<Device>(), Times.Once);
        }

    }

}
