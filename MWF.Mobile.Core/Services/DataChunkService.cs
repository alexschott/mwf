using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore.Platform;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;

namespace MWF.Mobile.Core.Services
{
    public class DataChunkService : IDataChunkService
    {

        #region Private Members

        private readonly Repositories.IRepositories _repositories = null;
        private readonly IGatewayQueuedService _gatewayQueuedService = null;
        private readonly IGpsService _gpsService = null;
        private readonly ILoggingService _loggingService = null;

        #endregion Private Members

        #region Constructors

        public DataChunkService(
            Repositories.IRepositories repositories,
            IGatewayQueuedService gatewayQueuedService,
            IGpsService gpsService,
            ILoggingService loggingService)
        {
            _repositories = repositories;
            _gatewayQueuedService = gatewayQueuedService;
            _gpsService = gpsService;
            _loggingService = loggingService;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// This method sends the instructions that have been acknowledged by the driver.
        /// This is in the form of a 'Read' Chunk
        /// </summary>
        /// <param name="instructions">The instruction that have been acknowledged</param>
        public async Task SendReadChunkAsync(IEnumerable<MobileData> instructions, Guid currentDriverID, string currentVehicleRegistration)
        {
            //The data chunk to be sent.
            MobileApplicationDataChunkCollection dataChunkCollection = new MobileApplicationDataChunkCollection { MobileApplicationDataChunkCollectionObject = new List<MobileApplicationDataChunk>() };

            foreach (var instruction in instructions)
            {
                MobileApplicationDataChunk dataChunk = new MobileApplicationDataChunk();
                MobileApplicationDataChunkContentActivity dataChunkActivity = new MobileApplicationDataChunkContentActivity();

                //These variables make up the Data variable in the MobileApplicationDataChunk object.
                MobileApplicationDataChunkContentActivities dataChunkActivities = new MobileApplicationDataChunkContentActivities { MobileApplicationDataChunkContentActivitiesObject = new List<MobileApplicationDataChunkContentActivity>() };
                MobileApplicationDataChunkContentOrder dataChunkOrder = new MobileApplicationDataChunkContentOrder { MobileApplicationDataChunkContentOrderActivities = new List<MobileApplicationDataChunkContentActivities>() };

                instruction.LatestDataChunkSequence++;

                dataChunkActivity.Activity = 10;
                dataChunkActivity.DriverId = currentDriverID;
                dataChunkActivity.EffectiveDate = DateTime.Now;
                dataChunkActivity.EffectiveDate = dataChunkActivity.EffectiveDate.AddMilliseconds(-dataChunkActivity.EffectiveDate.Millisecond);
                dataChunkActivity.MwfVersion = "";
                dataChunkActivity.VehicleRegistration = currentVehicleRegistration;
                dataChunkActivity.Smp = _gpsService.GetSmpData(Enums.ReportReason.Begin);
                dataChunkActivity.Title = "READ";
                dataChunkActivity.Sequence = instruction.LatestDataChunkSequence;

                dataChunkActivities.MobileApplicationDataChunkContentActivitiesObject.Add(dataChunkActivity);
                dataChunkOrder.MobileApplicationDataChunkContentOrderActivities.Add(dataChunkActivities);

                dataChunk.EffectiveDate = dataChunkActivity.EffectiveDate;
                dataChunk.ID = Guid.NewGuid();
                dataChunk.MobileApplicationDataID = instruction.ID;
                dataChunk.SyncState = Enums.SyncState.Add;
                dataChunk.Title = "READ";
                dataChunk.Data = dataChunkOrder;
                dataChunk.Sequence = instruction.Sequence;

                dataChunkCollection.MobileApplicationDataChunkCollectionObject.Add(dataChunk);

                if (instruction.SyncState != Enums.SyncState.Delete)
                {

                    var mobileDataToUpdate = await _repositories.MobileDataRepository.GetByIDAsync(instruction.ID);

                    if (mobileDataToUpdate != null)
                        await _repositories.MobileDataRepository.DeleteAsync(mobileDataToUpdate);

                    try
                    {
                        await _repositories.MobileDataRepository.InsertAsync(instruction);
                    }
                    catch (Exception ex)
                    {
                        MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "MobileDataRepository", "InsertAsync", ex.StackTrace);
                        throw;
                    }
                }

            }

            await _gatewayQueuedService.AddToQueueAsync("fwSyncChunkToServer", dataChunkCollection);
        }

        /// <summary>
        /// This method sends the MobileApplicationDataChunk up to bluesphere,
        /// this is called for when the instruction goes into Drive, OnSite and is Completed
        /// </summary>
        /// <param name="updateQuantity"></param>
        public async Task SendDataChunkAsync(MobileApplicationDataChunkContentActivity dataChunkActivity, MobileData currentMobileData, Guid currentDriverID, string currentVehicleRegistration, bool updateQuantity = false, bool updateTrailer = false)
        {
            var mobileData = currentMobileData;
            mobileData.LatestDataChunkSequence++;

            bool deleteMobileData = false;
            string smp = "";

            //These variables make up the Data variable in the MobileApplicationDataChunk object.
            MobileApplicationDataChunkContentActivities dataChunkActivities = new MobileApplicationDataChunkContentActivities { MobileApplicationDataChunkContentActivitiesObject = new List<MobileApplicationDataChunkContentActivity>() };
            MobileApplicationDataChunkContentOrder dataChunkOrder = new MobileApplicationDataChunkContentOrder { MobileApplicationDataChunkContentOrderActivities = new List<MobileApplicationDataChunkContentActivities>() };

            //The data chunk to be sent.
            MobileApplicationDataChunk dataChunk = new MobileApplicationDataChunk();
            MobileApplicationDataChunkCollection dataChunkCollection = new MobileApplicationDataChunkCollection { MobileApplicationDataChunkCollectionObject = new List<MobileApplicationDataChunk>() };

            dataChunkActivity.Activity = 10;
            dataChunkActivity.DriverId = currentDriverID;
            dataChunkActivity.EffectiveDate = DateTime.Now;
            dataChunkActivity.EffectiveDate = dataChunkActivity.EffectiveDate.AddMilliseconds(-dataChunkActivity.EffectiveDate.Millisecond);
            dataChunkActivity.MwfVersion = "";
            dataChunkActivity.VehicleRegistration = currentVehicleRegistration;

            dataChunk.EffectiveDate = dataChunkActivity.EffectiveDate;
            dataChunk.ID = Guid.NewGuid();
            dataChunk.MobileApplicationDataID = mobileData.ID;
            dataChunk.SyncState = Enums.SyncState.Add;

            if (updateQuantity)
            {
                smp = _gpsService.GetSmpData(Enums.ReportReason.ActiveReport);
                dataChunkActivity.Title = "REVISED QUANTITY";

                dataChunk.Title = "REVISED QUANTITY";
            }
            else if (updateTrailer)
            {
                smp = _gpsService.GetSmpData(Enums.ReportReason.Trailer);
                dataChunkActivity.Title = "REVISED TRAILER";
                dataChunk.Title = "REVISED TRAILER";
                dataChunkActivity.Data.Trailer = mobileData.Order.Additional.Trailer;
            }
            else
            {
                switch (mobileData.ProgressState)
                {
                    case Enums.InstructionProgress.Driving:
                        smp = _gpsService.GetSmpData(Enums.ReportReason.Drive);
                        dataChunkActivity.Title = "DRIVE";

                        dataChunk.Title = "DRIVE";
                        break;

                    case Enums.InstructionProgress.OnSite:
                        smp = _gpsService.GetSmpData(Enums.ReportReason.OnSite);
                        dataChunkActivity.Title = "ONSITE";

                        dataChunk.Title = "ONSITE";

                        break;

                    case Enums.InstructionProgress.Complete:
                        smp = _gpsService.GetSmpData(Enums.ReportReason.Complete);
                        dataChunkActivity.Title = "COMPLETE";
                        dataChunk.Title = "COMPLETE";

                        if (mobileData.OnSiteDateTime != DateTime.MinValue)
                            dataChunkActivity.OverRiddenOnSiteDateTime = mobileData.OnSiteDateTime;

                        if (mobileData.CompleteDateTime != DateTime.MinValue)
                            dataChunkActivity.OverRiddenCompleteDateTime= mobileData.CompleteDateTime;

                        //Delete all instruction types apart from Messages 
                        //they need to be stored so they can be displayed in the Inbox
                        if (mobileData.Order.Type != Enums.InstructionType.OrderMessage)
                            deleteMobileData = true;

                        break;

                    default:
                        await _loggingService.LogEventAsync(string.Format("Mobile Application of state {0} attempted an uploaded.", mobileData.ProgressState), Enums.LogType.Warn);
                        return;

                }
            }

            dataChunkActivity.Smp = smp;
            dataChunkActivity.Sequence = mobileData.LatestDataChunkSequence;
            dataChunkActivities.MobileApplicationDataChunkContentActivitiesObject.Add(dataChunkActivity);
            dataChunkOrder.MobileApplicationDataChunkContentOrderActivities.Add(dataChunkActivities);

            dataChunk.Data = dataChunkOrder;
            dataChunk.Sequence = mobileData.Sequence;

            dataChunkCollection.MobileApplicationDataChunkCollectionObject.Add(dataChunk);

            await _gatewayQueuedService.AddToQueueAsync("fwSyncChunkToServer", dataChunkCollection);

            //Delete the instruction from the repository if its completed else just update it.
            if (deleteMobileData)
            {
                var oldMobileData = await _repositories.MobileDataRepository.GetByIDAsync(mobileData.ID);

                if (oldMobileData != null)
                   await _repositories.MobileDataRepository.DeleteAsync(oldMobileData);
            }
            else
            {
                var mobileDataToUpdate = await _repositories.MobileDataRepository.GetByIDAsync(mobileData.ID);

                if (mobileDataToUpdate != null)
                    await _repositories.MobileDataRepository.DeleteAsync(mobileDataToUpdate);

                try
                {
                    await _repositories.MobileDataRepository.InsertAsync(mobileData);
                }
                catch (Exception ex)
                {
                    MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "MobileDataRepository", "InsertAsync", ex.StackTrace);
                    throw;
                }
            }
        }

        #endregion Public Methods
    }
}
