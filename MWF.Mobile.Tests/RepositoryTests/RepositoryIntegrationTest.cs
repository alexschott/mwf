using System;
using System.IO;
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
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Tests.Helpers;
using MWF.Mobile.Tests.RepositoryTests.TestModels;

namespace MWF.Mobile.Tests.RepositoryTests
{
    // Tests a full write and read to a sqlite database via the repository and dataservice layers
    public class RepositoryIntegrationTest
        : MvxIoCSupportingTest
    {

        private IDataService _dataService;
        private IFixture _fixture;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            var mockDeviceInfo = _fixture.InjectNewMock<IDeviceInfo>();
            mockDeviceInfo.Setup(di => di.DatabasePath).Returns(string.Empty);

            var platform = new SQLite.Net.Platform.Generic.SQLitePlatformGeneric();
            _dataService = new DataService(mockDeviceInfo.Object, platform);
            var connection = _dataService.GetDBConnection();

            this.CreateEmptyTable<Device>(connection);
            this.CreateEmptyTable<GrandParentEntity>(connection);
            this.CreateEmptyTable<ParentEntity>(connection);
            this.CreateEmptyTable<ChildEntity>(connection);
            this.CreateEmptyTable<ChildEntity2>(connection);
            this.CreateEmptyTable<SingleChildEntity>(connection);
            this.CreateEmptyTable<MultiChildEntity>(connection);
        }

        private void CreateEmptyTable<T>(Core.Database.IConnection connection)
        {
            connection.CreateTable<T>();
            connection.DeleteAll<T>();
        }

        [Fact]
        public async Task Repository_Insert_Read()
        {
            base.ClearAll();

            List<Device> devicesIn = _fixture.CreateMany<Device>().OrderBy(d => d.ID).ToList();

            var deviceRepository = new DeviceRepository(_dataService);

            // Insert records
            foreach (var device in devicesIn)
            {
                await deviceRepository.InsertAsync(device);
            }

            // Read them all back
            List<Device> devicesOut = (await deviceRepository.GetAllAsync()).OrderBy(d => d.ID).ToList();

            // Check we got the same number of records out that we put in
            Assert.Equal(devicesIn.Count, devicesOut.Count);

            // Check that all items/properties have the same values
            for (int i = 0; i < devicesIn.Count; i++)
            {
                Assert.Equal(devicesIn[i].ID, devicesOut[i].ID);
                Assert.Equal(devicesIn[i].Title, devicesOut[i].Title);
                Assert.Equal(devicesIn[i].Type, devicesOut[i].Type);
                Assert.Equal(devicesIn[i].CustomerID, devicesOut[i].CustomerID);
                Assert.Equal(devicesIn[i].CustomerTitle, devicesOut[i].CustomerTitle);
                Assert.Equal(devicesIn[i].DeviceIdentifier, devicesOut[i].DeviceIdentifier);
            }
        }

        [Fact]
        public async Task Repository_Insert_GetByID()
        {
            base.ClearAll();

            var deviceIn = _fixture.Create<Device>();
            Guid ID = deviceIn.ID;

            var deviceRepository = new DeviceRepository(_dataService);

            // Insert record
            await deviceRepository.InsertAsync(deviceIn);

            // Get the device back by id
            var deviceOut = await deviceRepository.GetByIDAsync(ID);

            //Check that all items/properties have the same values
            Assert.Equal(deviceIn.Title, deviceOut.Title);
            Assert.Equal(deviceIn.Type, deviceOut.Type);
            Assert.Equal(deviceIn.CustomerID, deviceOut.CustomerID);
            Assert.Equal(deviceIn.CustomerTitle, deviceOut.CustomerTitle);
            Assert.Equal(deviceIn.DeviceIdentifier, deviceOut.DeviceIdentifier);
        }

        [Fact]
        // Tests a repository can deal with an entity type which has a child relationship
        // with an other entity type
        public async Task Repository_SingleChildRelation_Insert_GetByID()
        {
            base.ClearAll();

            var verbProfile1In = _fixture.Create<VerbProfile>();
            var verbProfile2In = _fixture.Create<VerbProfile>();

            var verbProfileRepository = new VerbProfileRepository(_dataService);

            // Insert records
            await verbProfileRepository.InsertAsync(verbProfile1In);
            await verbProfileRepository.InsertAsync(verbProfile2In);

            // Get the first profile back by id
            var verbProfile1Out = await verbProfileRepository.GetByIDAsync(verbProfile1In.ID);

            // Check that verb profile we retreived has correct number of children
            Assert.Equal(verbProfile1In.Children.Count, verbProfile1Out.Children.Count);

            //Check that all the children we have retreived have the correct property values
            for (int i = 0; i < verbProfile1In.Children.Count; i++)
            {
                Assert.Equal(verbProfile1In.Children[i].ID, verbProfile1Out.Children[i].ID);
                Assert.Equal(verbProfile1In.Children[i].Title, verbProfile1Out.Children[i].Title);
                Assert.Equal(verbProfile1In.Children[i].Category, verbProfile1Out.Children[i].Category);
                Assert.Equal(verbProfile1In.Children[i].Code, verbProfile1Out.Children[i].Code);
                Assert.Equal(verbProfile1In.Children[i].IsHighlighted, verbProfile1Out.Children[i].IsHighlighted);
                Assert.Equal(verbProfile1In.Children[i].Order, verbProfile1Out.Children[i].Order);
                Assert.Equal(verbProfile1In.Children[i].ShowComment, verbProfile1Out.Children[i].ShowComment);
                Assert.Equal(verbProfile1In.Children[i].ShowImage, verbProfile1Out.Children[i].ShowImage);
                Assert.Equal(verbProfile1In.Children[i].ShowSignature, verbProfile1Out.Children[i].ShowSignature);
            }
        }

        [Fact]
        // Tests a repository can deal with an entity type which has a child relationships
        // with more than one entity type
        public async Task Repository_MultipleChildRelation_Insert_GetByID()
        {
            base.ClearAll();

            var parentEntityIn = _fixture.Create<ParentEntity>();

            parentEntityIn.FirstChild.IsFirstChild = true;
            parentEntityIn.SecondChild.IsFirstChild = false;

            var parentEntity2In = _fixture.Create<ParentEntity>();

            ParentEntityRepository repository = new ParentEntityRepository(_dataService);

            // Insert records
            await repository.InsertAsync(parentEntityIn);
            await repository.InsertAsync(parentEntity2In);

            // Get the first entity back by id
            var parentEntityOut = await repository.GetByIDAsync(parentEntityIn.ID);

            // Check that the entity we retreived has correct number of children (of both types)
            Assert.Equal(parentEntityIn.Children.Count, parentEntityOut.Children.Count);
            Assert.Equal(parentEntityIn.Children2.Count, parentEntityOut.Children2.Count);

            // Check that all the children we have retreived have the correct property values
            for (int i = 0; i < parentEntityIn.Children.Count; i++)
            {
                Assert.Equal(parentEntityIn.Children[i].ID, parentEntityOut.Children[i].ID);
                Assert.Equal(parentEntityIn.Children[i].Title, parentEntityOut.Children[i].Title);
            }

            // Check that all the children we have retreived have the correct property values
            for (int i = 0; i < parentEntityIn.Children2.Count; i++)
            {
                Assert.Equal(parentEntityIn.Children2[i].ID, parentEntityOut.Children2[i].ID);
                Assert.Equal(parentEntityIn.Children2[i].Title, parentEntityOut.Children2[i].Title);
            }

            // Check single child relationship
            Assert.Equal(parentEntityIn.Child.ID, parentEntityOut.Child.ID);
            Assert.Equal(parentEntityIn.Child.Title, parentEntityOut.Child.Title);

            // Check multi child relationship
            Assert.Equal(parentEntityIn.FirstChild.ID, parentEntityOut.FirstChild.ID);
            Assert.Equal(parentEntityIn.FirstChild.Title, parentEntityOut.FirstChild.Title);
            Assert.Equal(parentEntityIn.FirstChild.IsFirstChild, parentEntityOut.FirstChild.IsFirstChild);

            Assert.Equal(parentEntityIn.SecondChild.ID, parentEntityOut.SecondChild.ID);
            Assert.Equal(parentEntityIn.SecondChild.Title, parentEntityOut.SecondChild.Title);
            Assert.Equal(parentEntityIn.SecondChild.IsFirstChild, parentEntityOut.SecondChild.IsFirstChild);
        }

        [Fact]
        // Tests a repository can deal with an entity type which has nested child relationships
        // e.g. Grandparent -> Parent -> Child
        public async Task Repository_NestedChildRelation_Insert_GetByID()
        {
            base.ClearAll();

            var grandParentEntityIn = _fixture.Create<GrandParentEntity>();
            var grandParentEntity2In = _fixture.Create<GrandParentEntity>();

            var repository = new GrandParentEntityRepository(_dataService);

            // Insert records
            await repository.InsertAsync(grandParentEntityIn);
            await repository.InsertAsync(grandParentEntity2In);

            // Get the first entity back by id
            var grandParentEntityOut = await repository.GetByIDAsync(grandParentEntityIn.ID);

            CheckEntityTreesAreSame(grandParentEntityIn, grandParentEntityOut);
        }

        [Fact]
        public async Task Repository_NestedChildRelation_InsertMany_GetAll()
        {
            base.ClearAll();

            var repository = new GrandParentEntityRepository(_dataService);

            // Insert records
            List<GrandParentEntity> grandParentEntitiesIn = _fixture.CreateMany<GrandParentEntity>().ToList();
            await repository.InsertAsync(grandParentEntitiesIn);

            // Get all the entities out
            var grandParentEntitiesOut = (await repository.GetAllAsync()).ToList();

            // Check that we got the same number of entities back out
            Assert.Equal(grandParentEntitiesIn.ToList().Count, grandParentEntitiesOut.ToList().Count);

            // Check down the hierarchy that property values line up
            for (int i = 0; i < grandParentEntitiesIn.Count; i++)
            {
                CheckEntityTreesAreSame(grandParentEntitiesIn[i], grandParentEntitiesOut[i]);
            }
        }

        [Fact]
        // Tests a repository can deal with an entity type which has nested child relationships
        // e.g. Grandparent -> Parent -> Child
        public async Task Repository_NestedChildRelation_DeleteAll()
        {
            base.ClearAll();

            var grandParentEntityIn = _fixture.Create<GrandParentEntity>();

            var repository = new GrandParentEntityRepository(_dataService);

            // Insert records
            await repository.InsertAsync(grandParentEntityIn);

            // DeleteAll
            await repository.DeleteAllAsync();

            var asyncConnection = _dataService.GetAsyncDBConnection();

            // Check the table and all its child tables are now empty
            Assert.Empty(await asyncConnection.Table<GrandParentEntity>().ToListAsync());
            Assert.Empty(await asyncConnection.Table<ParentEntity>().ToListAsync());
            Assert.Empty(await asyncConnection.Table<ChildEntity>().ToListAsync());
            Assert.Empty(await asyncConnection.Table<ChildEntity2>().ToListAsync());
            Assert.Empty(await asyncConnection.Table<SingleChildEntity>().ToListAsync());
        }

        #region helper functions

        private void CheckEntityTreesAreSame(GrandParentEntity grandParentEntityIn, GrandParentEntity grandParentEntityOut)
        {
            // Check down the hierarchy that property values line up
            for (int i = 0; i < grandParentEntityIn.Children.Count; i++)
            {
                Assert.Equal(grandParentEntityIn.Children[i].ID, grandParentEntityOut.Children[i].ID);
                Assert.Equal(grandParentEntityIn.Children[i].Title, grandParentEntityOut.Children[i].Title);

                // Check that the chidlren have correct number of children 
                Assert.Equal(grandParentEntityIn.Children[i].Children.Count, grandParentEntityOut.Children[i].Children.Count);

                for (int j = 0; j < grandParentEntityIn.Children[i].Children.Count; j++)
                {
                    Assert.Equal(grandParentEntityIn.Children[i].Children[j].ID, grandParentEntityOut.Children[i].Children[j].ID);
                    Assert.Equal(grandParentEntityIn.Children[i].Children[j].Title, grandParentEntityOut.Children[i].Children[j].Title);
                }

                // Check single child relationship
                Assert.Equal(grandParentEntityIn.Children[i].Child.ID, grandParentEntityOut.Children[i].Child.ID);
                Assert.Equal(grandParentEntityIn.Children[i].Child.Title, grandParentEntityOut.Children[i].Child.Title);

            }
        }

        #endregion

    }

    #region Test Repository Classes

    internal class ParentEntityRepository : Repository<ParentEntity>
    {
        public ParentEntityRepository(IDataService dataService)
            : base(dataService)
        { }
    }

    internal class GrandParentEntityRepository : Repository<GrandParentEntity>
    {
        public GrandParentEntityRepository(IDataService dataService)
            : base(dataService)
        { }
    }

    #endregion

}
