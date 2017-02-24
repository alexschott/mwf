using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;

namespace MWF.Mobile.Core.ViewModels.Extensions
{

    public static class ViewModelExtensions
    {

        /// <summary>
        /// A viewmodel can use this method to implement a standard respond to a GatewayInstructionNotificationMessage.
        /// </summary>
        /// <param name="viewModel">The viewmodel that has received the notification message</param>
        /// <param name="message">The message received</param>
        /// <param name="navData">The viewmodel's navigation data (must be of type NavData<MobileData>)</param>
        /// <param name="refreshPage">
        /// An Action delegate that should be invoked if the page needs to be refreshed.
        /// This can be null if no refresh action is required.
        /// Note that this action will not be called on the UI thread, so the viewmodel should use InvokeOnMainThread() to execute any code that modifies the UI.</param>
        public static async Task RespondToInstructionNotificationAsync(this IInstructionNotificationViewModel viewModel, Messages.GatewayInstructionNotificationMessage message, NavData<MobileData> navData, Action refreshPage)
        {
            if (navData.Data.ProgressState == Enums.InstructionProgress.Complete)
                return;

            var instructionID = navData.Data.ID;

            var isVisible = !(viewModel is IVisible) || ((IVisible)viewModel).IsVisible;

            if (message.DeletedInstructionIDs.Contains(instructionID))
            {
                // Note that if the primary current instruction has been deleted and this viewmodel is not visible then there is nothing we can do here.
                // We presume that the active viewmodel will have also responded to this message and will have redirected back to the Manifest screen.
                if (isVisible)
                {
                    await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("Redirecting you back to the manifest screen", "This instruction has been deleted.");
                    await Mvx.Resolve<INavigationService>().GoToManifestAsync();
                }
            }
            else
            {
                var additionalInstructions = navData.GetAdditionalInstructions();
                var additionalInstructionIDs = additionalInstructions.Select(i => i.ID).ToList();

                var isThisInstructionUpdated = message.UpdatedInstructionIDs.Contains(instructionID);
                var updatedAdditionalInstructionIDs = message.UpdatedInstructionIDs.Intersect(additionalInstructionIDs).ToList();
                var deletedAdditionalInstructionIDs = message.DeletedInstructionIDs.Intersect(additionalInstructionIDs).ToList();
                var haveAdditionalInstructionsChanged = updatedAdditionalInstructionIDs.Any() || deletedAdditionalInstructionIDs.Any();

                if (isThisInstructionUpdated || haveAdditionalInstructionsChanged)
                {
                    if (isVisible)
                    {
                        var title = haveAdditionalInstructionsChanged ? "Instructions have been changed." : "This instruction has been updated.";
                        var msg = refreshPage == null ? "Data may have changed." : "Refreshing the page.";
                        await Mvx.Resolve<ICustomUserInteraction>().AlertAsync(msg, title);
                    }

                    var repositories = Mvx.Resolve<Repositories.IRepositories>();

                    if (isThisInstructionUpdated)
                        navData.Data = await repositories.MobileDataRepository.GetByIDAsync(instructionID);

                    foreach (var updatedAdditionalInstructionID in updatedAdditionalInstructionIDs)
                    {
                        var instructionToUpdate = additionalInstructions.FirstOrDefault(ai => ai.ID == updatedAdditionalInstructionID);

                        if (instructionToUpdate != null)
                        {
                            additionalInstructions.Remove(instructionToUpdate);
                            additionalInstructions.Add(await repositories.MobileDataRepository.GetByIDAsync(updatedAdditionalInstructionID));
                        }
                    }

                    foreach (var deletedAdditionalInstructionID in deletedAdditionalInstructionIDs)
                    {
                        var instructionToDelete = additionalInstructions.FirstOrDefault(ai => ai.ID == deletedAdditionalInstructionID);

                        if (instructionToDelete != null)
                            additionalInstructions.Remove(instructionToDelete);
                    }

                    if (refreshPage != null)
                        refreshPage();
                }
            }
        }

    }

}
