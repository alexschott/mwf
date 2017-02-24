using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.ViewModels
{
    public class InstructionSafetyCheckViewModel : SafetyCheckViewModel
    {

        #region Construction

        public InstructionSafetyCheckViewModel(IInfoService infoService, INavigationService navigationService, IRepositories repositories, ISafetyCheckService safetyCheckService)
            : base(infoService, navigationService, repositories, safetyCheckService)
        {
        }

        public override Task Init()
        {
            // Don't call down to base Init method: what it does is not relevant for instruction safety checks, there is corresponding code specifically for instructions in Init(navData) below.
            return Task.FromResult(0);
        }

        public async Task Init(Guid navID)
        {
            this._manual = false;
            _navData = _navigationService.GetNavData<MobileData>(navID);

            Models.Trailer trailer = _navData.OtherData["UpdatedTrailer"] as Models.Trailer;

            if (trailer != null)
            {
                var safetyProfileData = await _repositories.SafetyProfileRepository.GetAllAsync();
                SafetyProfileTrailer = safetyProfileData.Where(spt => spt.IntLink == trailer.SafetyCheckProfileIntLink).SingleOrDefault();
            }

            this.SafetyCheckItemViewModels = new ObservableCollection<SafetyCheckItemViewModel>();
            _navData.OtherData["UpdatedTrailerSafetyCheckData"] = null;

            if (SafetyProfileTrailer != null)
            {
                var safetyCheckData = await this.GenerateSafetyCheckDataAsync(SafetyProfileTrailer, _infoService.CurrentDriverID.Value, trailer.ID, trailer.Registration, true);
                _navData.OtherData["UpdatedTrailerSafetyCheckData"] = safetyCheckData;
            }

            if (_navData.OtherData["UpdatedTrailerSafetyCheckData"] == null)
            {
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("A safety check profile for your trailer has not been found - Perform a manual safety check.");
                this._manual = true;
                await this.MoveToNextAsync();
            }

        }

        #endregion

    }

}
