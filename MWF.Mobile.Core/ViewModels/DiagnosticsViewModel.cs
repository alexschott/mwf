using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Extensions;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using Cirrious.CrossCore;
using MWF.Mobile.Core.ViewModels.Interfaces;

namespace MWF.Mobile.Core.ViewModels
{

    public class DiagnosticsViewModel : BaseFragmentViewModel
    {

        #region private members

        private readonly IDataService _dataService;
        private readonly IReachability _reachability;
        private readonly ICustomUserInteraction _userInteraction;
        private readonly INavigationService _navigationService;
        private readonly IDiagnosticsService _diagnosticsService;
        private readonly IApplicationProfileRepository _applicationProfileRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IDeviceInfo _deviceInfo;
        private string _unexpectedErrorMessage = "Unfortunately, there was an error uploading diagnostic data. Please try restarting the device and try again.";

        #endregion

        #region construction

        public DiagnosticsViewModel(IReachability reachability, IDataService dataService, IRepositories repositories, ICustomUserInteraction userInteraction, INavigationService navigationService, IDiagnosticsService diagnosticsService, IDeviceInfo deviceInfo)
        {
            _dataService = dataService;
            _reachability = reachability;
            _userInteraction = userInteraction;
            _navigationService = navigationService;
            _diagnosticsService = diagnosticsService;
            _applicationProfileRepository = repositories.ApplicationRepository;
            _configRepository = repositories.ConfigRepository;
            _deviceInfo = deviceInfo;

        }

        #endregion

        #region public properties

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string FragmentTitle { get { return "Send Diagnostics"; } }

        public string EnterButtonLabel
        {
            get { return "Send"; }
        }

        public string DiagnosticsMessageLabel
        {
            get { return "Push send to upload diagnostic information to Proteo."; }
        }

        public string VersionText
        {
            get { return string.Format("Version: {0}           DeviceID: {1}",_deviceInfo.SoftwareVersion, _deviceInfo.AndroidId); }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
        }

        public string ProgressTitle
        {
            get { return "Uploading Data...";  }
        }

        public string ProgressMessage
        {
            get { return "Diagnostic data is being uploaded, please wait.";  }
        }

        private MvxCommand _sendDiagnosticsCommand;
        public System.Windows.Input.ICommand SendDiagnosticsCommand
        {
            get
            {
                _sendDiagnosticsCommand = _sendDiagnosticsCommand ?? new MvxCommand(async () => await this.UploadDiagnosticsAsync());
                return _sendDiagnosticsCommand;
            }
        }

        #endregion

        #region private methods

        public async Task UploadDiagnosticsAsync()
        {
            if (!_reachability.IsConnected())
            {
                await _userInteraction.AlertAsync("You need a connection to the internet to submit diagnostics.");
            }
            else
            {
                bool success = false;
                this.IsBusy = true;

                try
                {
                    success = await _diagnosticsService.UploadDiagnosticsAsync(_dataService.DatabasePath);
                }
                catch(Exception)
                {
                    success = false;
                }
                finally
                {
                    this.IsBusy = false;
                }

                if (success)
                {
                    await _userInteraction.AlertAsync("Support diagnostic information uploaded successfully.", null, "Upload Complete");
                    await _navigationService.MoveToNextAsync();
                }
                else
                {
                    await _userInteraction.AlertAsync(_unexpectedErrorMessage);
                }
            }
        }
     
        #endregion

    }
}
