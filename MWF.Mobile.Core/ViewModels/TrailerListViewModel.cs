using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.ViewModels
{

    public class TrailerListViewModel
        : BaseTrailerListViewModel
    {
        private readonly IApplicationProfileRepository _applicationProfileRepository;

        public TrailerListViewModel(IGatewayService gatewayService, 
                                    IRepositories repositories, 
                                    IReachability reachabibilty, 
                                    IToast toast, 
                                    IInfoService infoService, 
                                    INavigationService navigationService) : base(gatewayService, repositories, reachabibilty, toast, infoService, navigationService)
        {
            _applicationProfileRepository = repositories.ApplicationRepository;
        }

        #region Protected/Private Methods

        protected override async Task ConfirmTrailerAsync(Trailer trailer, string title, string message)
        {
            Guid trailerID = Guid.Empty;

            if (trailer != null)
                trailerID = trailer.ID;

            //This will take to the next view model with a trailer value of null.
            if (await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync(message, title, "Confirm"))
            {
                this.IsBusy = true;

                try
                {
                    // we only need to check profiles once a day
                    var profileData = await _applicationProfileRepository.GetAllAsync();

                    if (profileData.Count() == 0)
                    {
                        // we have seen an issue where there was no profile so lets cater for this now.
                        var profile = await _gatewayService.GetApplicationProfileAsync();

                        try
                        {
                            await _applicationProfileRepository.InsertAsync(profile);
                        }
                        catch (Exception ex)
                        {
                            MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "ApplicationProfileRepository", "InsertAsync", ex.StackTrace);
                            throw;
                        }
                    }

                    var applicationProfile = profileData.OrderByDescending(x=> x.IntLink).First();

                    if (DateTime.Now.Subtract( applicationProfile.LastVehicleAndDriverSync).TotalHours > 23)
                    {
                        await UpdateVehicleListAsync();
                        await UpdateTrailerListAsync();
                        // Try and update safety profiles before continuing
                        await UpdateSafetyProfilesAsync();
                        ProgressMessage = "Updating Application Profile.";
                        applicationProfile = await _gatewayService.GetApplicationProfileAsync();
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

                _infoService.SetCurrentTrailer(trailer);
                try
                {
                    await _navigationService.MoveToNextAsync();
                }
                catch (Exception ex)
                {
                    MvxTrace.Error("Next {0}", ex.Message);
                    throw;
                }
            }
        }

        #endregion
    }
}
