using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Extensions;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.ViewModels
{

    public class VehicleListViewModel
        : BaseFragmentViewModel
    {
        private Services.IGatewayService _gatewayService;
        private IEnumerable<Vehicle> _originalVehicleList;

        private readonly ICurrentDriverRepository _currentDriverRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IToast _toast;
        private readonly IReachability _reachability;
        private readonly IInfoService _infoService;
        private readonly INavigationService _navigationService;
        private readonly ILoggingService _loggingService;
        private readonly ISafetyProfileRepository _safetyProfileRepository;

        public VehicleListViewModel(
            IGatewayService gatewayService, 
            IVehicleRepository vehicleRepository, 
            IReachability reachabibilty,
            IToast toast, 
            IInfoService infoService, 
            INavigationService navigationService, 
            ICurrentDriverRepository currentDriverRepository,
            ILoggingService loggingService, 
            ISafetyProfileRepository safetyProfileRepository)
        {
            _toast = toast;
            _reachability = reachabibilty;

            _gatewayService = gatewayService;
            _infoService = infoService;
            _navigationService = navigationService;
            _loggingService = loggingService;

            _currentDriverRepository = currentDriverRepository;
            _vehicleRepository = vehicleRepository;
            _safetyProfileRepository = safetyProfileRepository;
            
        }

        public async Task Init()
        {
            Vehicles = _originalVehicleList = await _vehicleRepository.GetAllAsync();
            _vehicleListCount = FilteredVehicleCount;

            await this.LastVehicleSelectAsync();
        }

        private string _vehicleSelectText;
        public string VehicleSelectText
        {
            get { return _vehicleSelectText; }
            set { _vehicleSelectText = value; RaisePropertyChanged(() => VehicleSelectText); }
        }

        public override string FragmentTitle
        {
            get { return "Vehicle"; }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
        }

        public string ProgressTitle
        {
            get { return "Downloading data..."; }
        }

        public string ProgressMessage
        {
            get { return "Updating Vehicles.."; }
        }

        private int _vehicleListCount;
        public int VehicleListCount
        {
            get { return _vehicleListCount; }
            private set { _vehicleListCount = value; }
        }

        public int FilteredVehicleCount
        {
            get { return Vehicles.ToList().Count; }
        }

        private IEnumerable<Vehicle> _vehicles;
        public IEnumerable<Vehicle> Vehicles
        {
            get { return _vehicles; }
            set { _vehicles = value; RaisePropertyChanged(() => Vehicles); }
        }

        public Task MoveToNextAsync(Vehicle vehicle)
        {
            _infoService.SetCurrentVehicle(vehicle);
            return _navigationService.MoveToNextAsync();
        }

        private async Task LastVehicleSelectAsync()
        {
            var currentDriver = await _currentDriverRepository.GetByIDAsync(_infoService.CurrentDriverID.Value);

            if (currentDriver == null)
                return;

            var lastVehicleID = currentDriver.LastVehicleID;

            if (lastVehicleID == null)
                return;

            var vehicle = await _vehicleRepository.GetByIDAsync(lastVehicleID);

            if (vehicle == null)
                return;

            if (await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync(vehicle.Registration, "Last Used Vehicle", "Confirm"))
                await this.MoveToNextAsync(vehicle);
        }

        private MvxCommand<Vehicle> _showVehicleDetailCommand;
        public ICommand ShowVehicleDetailCommand
        {
            get { return (_showVehicleDetailCommand = _showVehicleDetailCommand ?? new MvxCommand<Vehicle>(async v => await this.VehicleDetailAsync(v))); }
        }

        public async Task VehicleDetailAsync(Vehicle vehicle)
        {
            if (await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync(vehicle.Registration, "Confirm your vehicle", "Confirm"))
            {
                var newDriver = await _currentDriverRepository.GetByIDAsync(_infoService.CurrentDriverID.Value);

                if (newDriver == null)
                    return;

                await _currentDriverRepository.DeleteAsync(newDriver);
                newDriver.LastVehicleID = vehicle.ID;

                try
                {
                    await _currentDriverRepository.InsertAsync(newDriver);
                }
                catch (Exception ex)
                {
                    MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "CurrentDriverRepository", "InsertAsync", ex.StackTrace);
                    throw;
                }

                await this.MoveToNextAsync(vehicle);
            }
        }

        private string _vehicleSearchText;
        public string VehicleSearchText
        {
            get { return _vehicleSearchText; }
            set { _vehicleSearchText = value; this.FilterList(); RaisePropertyChanged(() => VehicleSearchText); }
        }

        private void FilterList()
        {
            if (_originalVehicleList != null)
            {
                if (string.IsNullOrEmpty(VehicleSearchText))
                {
                    Vehicles = _originalVehicleList;
                }
                else
                {
                    Vehicles = _originalVehicleList.Where(t => t.Registration != null && t.Registration.ToUpper().Contains(VehicleSearchText.ToUpper()));
                }

                this.VehicleSelectText = string.Format("Select vehicle - Showing {0} of {1}", FilteredVehicleCount, VehicleListCount);
            }
        }

        private MvxCommand _refreshListCommand;
        public ICommand RefreshListCommand
        {
            get { return (_refreshListCommand = _refreshListCommand ?? new MvxCommand(async () => await this.UpdateVehicleListAsync())); }
        }

        public async Task UpdateVehicleListAsync()
        {
            this.IsBusy = true;

            try
            {
                if (!_reachability.IsConnected())
                {
                    _toast.Show("No internet connection!");
                }
                else
                {
                    var vehicleViews = await _gatewayService.GetVehicleViewsAsync();

                    var vehicleViewVehicles = new Dictionary<string, IEnumerable<Models.BaseVehicle>>(vehicleViews.Count());

                    foreach (var vehicleView in vehicleViews)
                    {
                        vehicleViewVehicles.Add(vehicleView.Title, await _gatewayService.GetVehiclesAsync(vehicleView.Title));
                    }

                    var vehiclesAndTrailers = vehicleViewVehicles.SelectMany(vvv => vvv.Value).DistinctBy(v => v.ID);
                    var vehicles = vehiclesAndTrailers.Where(bv => !bv.IsTrailer).Select(bv => new Models.Vehicle(bv));

                    if (vehicles != null)
                    {
                        await _vehicleRepository.DeleteAllAsync();

                        try
                        {
                            await _vehicleRepository.InsertAsync(vehicles);
                        }
                        catch (Exception ex)
                        {
                            MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "VehicleRepository", "InsertAsync", ex.StackTrace);
                            throw;
                        }

                        Vehicles = _originalVehicleList = await _vehicleRepository.GetAllAsync();

                        //Recalls the filter text if there is text in the search field.
                        if (VehicleSearchText != null)
                        {
                            this.FilterList();
                        }
                    }
                }

                await UpdateSafetyProfilesAsync();
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        protected async Task UpdateSafetyProfilesAsync()
        {
            
            // First check if we have a internet connection. If we do go and get the latest safety checks from Blue Sphere.
            if (_reachability.IsConnected())
            {
                IEnumerable<SafetyProfile> safetyProfiles = null;

                try
                {
                    safetyProfiles = await _gatewayService.GetSafetyProfilesAsync();
                }
                catch (TaskCanceledException)
                {
                    // Although we have used reachability to determine that there is an available network connection,
                    // it is still possible for the data fetch to fail which triggers a TaskCanceledException.
                }

                if (safetyProfiles != null)
                {
                    await _safetyProfileRepository.DeleteAllAsync();

                    try
                    {
                        await _safetyProfileRepository.InsertAsync(safetyProfiles);
                    }
                    catch (Exception ex)
                    {
                        MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "SafetyProfileRepository", "InsertAsync", ex.StackTrace);
                        throw;
                    }
                }
            }

            var safetyProfileData = await _safetyProfileRepository.GetAllAsync();

            if (safetyProfileData.ToList().Count == 0)
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("No Profiles Found.");
        }

    }

}
