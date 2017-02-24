using System;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Core.ViewModels.Interfaces;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{
    public interface INavigationService
    {
        Task GoBackAsync();
        Task<Guid> GoBackAsync(NavData navData);
        bool IsBackActionDefined();
        Task MoveToNextAsync();
        Task<Guid> MoveToNextAsync(NavData navData);
        Task GoToManifestAsync();
        Task LogoutAsync();
        Task DirectLogoutAsync();
        NavData<T> GetNavData<T>(Guid navID) where T : class;
        NavData CurrentNavData { get; }
        bool ShowModalViewModel<TViewModel, TResult>(NavData navData, Action<TResult> onResult) where TViewModel : IModalViewModel<TResult>;

    }
}
