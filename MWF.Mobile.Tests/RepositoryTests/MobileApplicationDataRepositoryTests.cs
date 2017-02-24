using SQLite.Net.Attributes;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Services;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Linq;
using Xunit;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using System.Collections.Generic;
using System.Collections;
using MWF.Mobile.Tests.Helpers;
using System.Threading.Tasks;
using System.Threading;
using SQLite.Net.Async;

namespace MWF.Mobile.Tests.RepositoryTests
{
    public class MobileApplicationDataRepositoryTests
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

            var dataServiceMock = Mock.Of<IDataService>(ds => ds.GetAsyncDBConnection() == _asyncConnectionMock.Object);
            _fixture.Register<IDataService>(() => dataServiceMock);
        }

        [Fact]
        public async Task Repository_Returns_Inprogress_Instructions()
        {
            base.ClearAll();

            List<MobileData> mobileDataList = new List<MobileData>();
            mobileDataList.Add(new MobileData { ID = Guid.NewGuid(), ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileDataList.Add(new MobileData { ID = Guid.NewGuid(), ProgressState = Core.Enums.InstructionProgress.NotStarted });

            var mockTableQuery = new MockAsyncTableQuery<MobileData>(mobileDataList);

            _asyncConnectionMock.Setup(c => c.Table<MobileData>()).Returns(mockTableQuery);

            var mdr = _fixture.Create<MobileDataRepository>();

            var inProgressInstructions = (await mdr.GetInProgressInstructionsAsync(Guid.NewGuid())).ToList();

            foreach (var instruction in inProgressInstructions)
            {
                Assert.True((instruction.ProgressState == Core.Enums.InstructionProgress.Driving || instruction.ProgressState == Core.Enums.InstructionProgress.OnSite), "An instruction has been returned that has already started");
            }            
        }

        [Fact]
        public async Task Repository_Returns_NotStarted_Instructions()
        {
            base.ClearAll();

            List<MobileData> mobileDataList = new List<MobileData>();
            mobileDataList.Add(new MobileData { ID = Guid.NewGuid(), ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileDataList.Add(new MobileData { ID = Guid.NewGuid(), ProgressState = Core.Enums.InstructionProgress.NotStarted });

            var mockTableQuery = new MockAsyncTableQuery<MobileData>(mobileDataList);

            _asyncConnectionMock.Setup(c => c.Table<MobileData>()).Returns(mockTableQuery);

            var mdr = _fixture.Create<MobileDataRepository>();

            var notStartedInstructions = (await mdr.GetNotStartedInstructionsAsync(Guid.NewGuid())).ToList();

            foreach (var instruction in notStartedInstructions)
            {
                Assert.True((instruction.ProgressState == Core.Enums.InstructionProgress.NotStarted ), "An instruction has been returned that has already started");
            }
        }
    } 
}
