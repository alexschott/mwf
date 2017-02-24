using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels.Interfaces
{
    public interface IBackButtonHandler
    {

        Task<bool> OnBackButtonPressedAsync();

    }
}
