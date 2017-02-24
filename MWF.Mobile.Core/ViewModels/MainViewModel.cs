using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.ViewModels
{

    public class MainViewModel : BaseActivityViewModel
    {
        #region Private Members

        private List<MenuViewModel> _menuItems;
        private MvxCommand<MenuViewModel> _selectMenuItemCommand;
        private MvxCommand _logoutCommand;
        private INavigationService _navigationService;

        #endregion

        public enum Option
        {
            Camera,
            ViewSafetyCheck,
            RunNewSafetyCheck,
            Manifest,
            Inbox,
            About,
            Diagnostics 
        }

        #region Constructor

        public MainViewModel(IMvxViewModelLoader viewModelLoader, INavigationService navigationService)
            : base(viewModelLoader)
        {
            this.SetInitialViewModel<ManifestViewModel>();

            // Start the gateway queue timer which will cause submission of any queued data to the MWF Mobile gateway service on a repeat basis
            // Commented out for now so we don't accidentally start submitting debug data to BlueSphere:
            //gatewayQueuedService.StartQueueTimer();

            //gatewayPollingService.StartPollingTimer();
            _navigationService = navigationService;

            this.InitializeMenu();
        }

        #endregion

        #region Public Properties

        public List<MenuViewModel> MenuItems
        {
            get { return this._menuItems; }
            set { this._menuItems = value; this.RaisePropertyChanged(() => this.MenuItems); }
        }

        public string LogoutText
        {
            get { return "Logout"; }
        }

        public System.Windows.Input.ICommand SelectMenuItemCommand
        {
            get
            {
                return this._selectMenuItemCommand ?? (this._selectMenuItemCommand = new MvxCommand<MenuViewModel>(this.DoSelectMenuItemCommand));
            }
        }

        public System.Windows.Input.ICommand LogoutCommand
        {
            get
            {
                return this._logoutCommand ?? (this._logoutCommand = new MvxCommand(async () => await this.DoLogoutCommandAsync()));
            }
        }

        #endregion

        #region Private Methods

        private void InitializeMenu()
        {
            MenuItems = new List<MenuViewModel>
            {
                new MenuViewModel
                {
                    Option = Option.Manifest,
                    Text = "Manifest Screen"
                },
                new MenuViewModel
                {
                    Option = Option.Camera,
                    Text = "Camera"
                },
                new MenuViewModel
                {
                    Option = Option.ViewSafetyCheck,
                    Text = "View Safety Check"
                },
                new MenuViewModel
                {
                   Option = Option.RunNewSafetyCheck,
                   Text = "Run Safety Check"
                },
                new MenuViewModel
                {
                    Option = Option.Inbox,
                    Text = "Inbox"
                },
                new MenuViewModel
                {
                    Option = Option.About,
                    Text = "About"   
                },
                 new MenuViewModel
                {
                    Option = Option.Diagnostics,
                    Text = "Support"
                }
            };
        }

        private async Task DoLogoutCommandAsync()
        {
            if (await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("Are you sure you want to log out?", "Logout", "Logout", "Cancel"))
              await _navigationService.LogoutAsync();
        }

        private void DoSelectMenuItemCommand(MenuViewModel item)
        {

            switch (item.Option)
            {
                case Option.Camera:
                    this.ShowViewModel<SidebarCameraViewModel>();
                    break;
                case Option.ViewSafetyCheck:
                    this.ShowViewModel<DisplaySafetyCheckViewModel>();
                    break;
                case Option.RunNewSafetyCheck:
                    this.ShowViewModel<SafetyCheckViewModel>();
                    break;
                case Option.Manifest:
                    this.ShowViewModel<ManifestViewModel>();
                    break;
                case Option.Inbox:
                    this.ShowViewModel<InboxViewModel>();
                    break;
                case Option.About:
                    this.ShowViewModel<AboutViewModel>();
                    break;
                case Option.Diagnostics:
                    this.ShowViewModel<DiagnosticsViewModel>();
                    break;
                default:
                    break;
            }

            InitializeMenu();
        }

        #endregion
    }

}
