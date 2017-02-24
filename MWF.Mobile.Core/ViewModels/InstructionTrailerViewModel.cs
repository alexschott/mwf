using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.ViewModels.Interfaces;
using Cirrious.CrossCore.Platform;
using MWF.Mobile.Core.ViewModels.Extensions;

namespace MWF.Mobile.Core.ViewModels
{
    public class InstructionTrailerViewModel
        : BaseTrailerListViewModel, IInstructionNotificationViewModel
    {

        #region Private Fields

        private MobileData _mobileData;
        private NavData<MobileData> _navData;

        private MvxSubscriptionToken _notificationToken;
        private IMvxMessenger _messenger;

        private readonly IApplicationProfileRepository _applicationProfileRepository;

        #endregion

        #region Construction

        public InstructionTrailerViewModel(IGatewayService gatewayService,
                                            INavigationService navigationService, 
                                            IRepositories repositories,
                                            IReachability reachabiity,
                                            IToast toast,
                                            IInfoService startUpService)
            : base(gatewayService, repositories, reachabiity, toast, startUpService, navigationService)
        {
            _notificationToken = Messenger.Subscribe<Messages.GatewayInstructionNotificationMessage>(async m => await CheckInstructionNotificationAsync(m));
            _applicationProfileRepository = _repositories.ApplicationRepository;
        }

        public async Task Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);
            _mobileData = _navData.Data;

            //set the default trailer to be the one specified on the order
            if (_mobileData.Order.Additional.Trailer != null)
            {
                await this.SetDefaultTrailerRegAsync(_mobileData.Order.Additional.Trailer.TrailerId);
            }
        }

        #endregion

        #region Private Properties

        protected new IMvxMessenger Messenger
        {
            get
            {
                return (_messenger = _messenger ?? Mvx.Resolve<IMvxMessenger>());
            }
        }

        #endregion

        #region Protected/Private Methods

        protected override async Task ConfirmTrailerAsync(Models.Trailer trailer, string title, string message)
        {
            try
            {
                if (await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync(message, title, "Confirm"))
                {
                    await UpdateReadyForSafetyCheckAsync(trailer);
                    _navData.OtherData["UpdatedTrailer"] = trailer;
                    await _navigationService.MoveToNextAsync(_navData);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task UpdateReadyForSafetyCheckAsync(Models.Trailer trailer)
        {
            // if a trailer has been selected and it differs from the current trailer then we need to update
            // everything required to update safety profiles in readiness for the next step
            var trailerDiffersFromCurrentTrailer =
                trailer != null &&
                (!_infoService.CurrentTrailerID.HasValue || trailer.ID != _infoService.CurrentTrailerID.Value);

            if (trailerDiffersFromCurrentTrailer)
            {
                this.IsBusy = true;

                try
                {
                    // we only need to check profiles once a day

                    var applicationProfileData = await _applicationProfileRepository.GetAllAsync();
                    var applicationProfile = applicationProfileData.First();

                    if (DateTime.Now.Subtract(applicationProfile.LastVehicleAndDriverSync).TotalHours > 23)
                    {
                        await UpdateVehicleListAsync();
                        await UpdateTrailerListAsync();

                        // Try and update safety profiles before continuing
                        await UpdateSafetyProfilesAsync();

                        applicationProfile.LastVehicleAndDriverSync = DateTime.Now;

                        try
                        {
                            await _applicationProfileRepository.UpdateAsync(applicationProfile);
                        }
                        catch (Exception ex)
                        {
                            MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "ApplicationProfileRepository", "UpdateAsync", ex.StackTrace);
                            throw;
                        }
                    }
                }
                finally
                {
                    this.IsBusy = false;
                }
            }
        }

        #endregion

        #region Core.Portable.IDisposable

        public void Dispose()
        {
            UnsubscribeNotificationToken();
        }

        #endregion

        #region IInstructionNotificationViewModel

        public void UnsubscribeNotificationToken()
        {
            if (_notificationToken != null)
                Messenger.Unsubscribe<Messages.GatewayInstructionNotificationMessage>(_notificationToken);
        }

        public Task CheckInstructionNotificationAsync(GatewayInstructionNotificationMessage message)
        {
            return this.RespondToInstructionNotificationAsync(message, _navData, () =>
            {
                _mobileData = _navData.Data;
                RaiseAllPropertiesChanged();
            });
        }

        #endregion

    }

}
