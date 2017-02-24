using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid.Platform;
using Cirrious.MvvmCross.Plugins.PictureChooser.Droid;
using MWF.Mobile.Android.Controls;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.ViewModels;

namespace MWF.Mobile.Android.Portable
{
    public class CustomUserInteraction : ICustomUserInteraction
    {
        public AlertDialog InstructionNotificationDialog;
        public AlertDialog CurrentPopUpAlertDialog;
        public AlertDialog ConfirmDialog;
        public Vibrate Vibrate = new Vibrate();
        public Sound Sound = new Sound();

        private List<ManifestInstructionViewModel> CurrentPopInstructions { get; set; }

        public CustomUserInteraction()
        {
            CurrentPopInstructions = new List<ManifestInstructionViewModel>();
        }

        protected Activity CurrentActivity
        {
            get { return Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity; }
        }

        #region ICustomUserInteraction Members

        /// <summary>
        /// This method is called when new instructions that have been polled were added, updated or deleted.
        /// It causes a modal pop up to appear showing the new instructions and what action was taken to them.
        /// </summary>
        /// <param name="alteredInstructions">The list of instructions to display</param>
        /// <param name="done">The action that is taken when the postive button is pressed</param>
        /// <param name="title">Title of the modal</param>
        /// <param name="okButton">The text of the positive button</param>
        public void PopUpInstructionNotification(List<ManifestInstructionViewModel> alteredInstructions, Action<List<ManifestInstructionViewModel>> done = null, string title = "", string okButton = "OK")
        {
            Application.SynchronizationContext.Post(ignored =>
            {
                if (CurrentActivity == null)
                    return;

                //This closes the pop if its showing so it can reopen another, else it will play sound and vibrate first time.
                if (InstructionNotificationDialog != null && InstructionNotificationDialog.IsShowing)
                {
                    try
                    {
                        InstructionNotificationDialog.Dismiss();
                        InstructionNotificationDialog = null;
                    } catch (Exception ex)
                    {
                        throw;
                    }
                }
                else
                {
                    Vibrate.VibrateDevice();
                    Sound.Play();
                }

                var customView = CurrentActivity.LayoutInflater.Inflate(Resource.Layout.PopUp_InstructionNotification, null);

                InstructionGroupedListObject addInstructions = new InstructionGroupedListObject();
                InstructionGroupedListObject updateInstructions = new InstructionGroupedListObject();
                InstructionGroupedListObject deleteInstructions = new InstructionGroupedListObject();
                InstructionGroupedListObject messages = new InstructionGroupedListObject();
                InstructionGroupedListObject messagesWithPoints = new InstructionGroupedListObject();

                CurrentPopInstructions.AddRange(alteredInstructions);

                //Filter the instructions into SyncStates (Added, Updated, Deleted)
                foreach (var instruction in CurrentPopInstructions)
                {
                    if (instruction.InstructionType == InstructionType.OrderMessage)
                    {
                        if (instruction.MobileData.Order.Addresses == null)
                            messages.Instructions.Add(instruction);
                        else
                            messagesWithPoints.Instructions.Add(instruction);
                    }
                    else
                    {
                        switch (instruction.MobileData.SyncState)
                        {
                            case SyncState.Add:
                                addInstructions.Instructions.Add(instruction);
                                break;
                            case SyncState.Update:
                                updateInstructions.Instructions.Add(instruction);
                                break;
                            case SyncState.Delete:
                                deleteInstructions.Instructions.Add(instruction);
                                break;
                        }
                    }
                }

                var inoList = new List<InstructionGroupedListObject>();
                var headers = new List<string>();

                if (addInstructions.Instructions.Count > 0)
                {
                    inoList.Add(addInstructions);
                    headers.Add("Instructions added (" + addInstructions.Instructions.Count + ")");
                }

                if (updateInstructions.Instructions.Count > 0)
                {
                    inoList.Add(updateInstructions);
                    headers.Add(" Instructions updated (" + updateInstructions.Instructions.Count + ")");
                }

                if (deleteInstructions.Instructions.Count > 0)
                {
                    inoList.Add(deleteInstructions);
                    headers.Add("Instructions deleted (" + deleteInstructions.Instructions.Count + ")");
                }
                if (messages.Instructions.Count > 0)
                {
                    inoList.Add(messages);
                    headers.Add("Messages added (" + messages.Instructions.Count + ")");
                }

                if (messagesWithPoints.Instructions.Count > 0)
                {
                    inoList.Add(messagesWithPoints);
                    headers.Add("Messages with points added (" + messagesWithPoints.Instructions.Count + ")");
                }

                if (inoList.Count > 0)
                {
                    //Create the expandableListView to be displayed
                    var expandableListView = (ExpandableListView)customView.FindViewById(Resource.Id.instuctionList);

                    expandableListView.ChildClick += expandableListView_ChildClick;

                    var expandableListAdapter = new ExpandableListAdapter(CurrentActivity, headers, inoList);

                    expandableListView.SetAdapter(expandableListAdapter);

                    //Expand all the sections from the start.
                    for (int i = 0; i < expandableListAdapter.GroupCount; i++)
                    {
                        expandableListView.ExpandGroup(i);
                    }

                    //Create the dialog popup
                    var notificationDialog = new AlertDialog.Builder(CurrentActivity)
                        .SetView(customView)
                        .SetTitle(title)

                        //Prevent the user from closing the pop up by clicking on the sides.
                        .SetCancelable(false)

                        .SetPositiveButton(okButton, (s, e) =>
                            {
                                if (done != null)
                                {
                                    done(CurrentPopInstructions);
                                    CurrentPopInstructions.Clear();
                                }
                            });

                    InstructionNotificationDialog = notificationDialog.Create();
                    InstructionNotificationDialog.Show();
                }

            }, null);
        }

        public Task<List<ManifestInstructionViewModel>> PopUpInstructionNotificationAsync(List<ManifestInstructionViewModel> alteredInstructions, string title = "", string okButton = "OK")
        {
            var tcs = new TaskCompletionSource<List<ManifestInstructionViewModel>>();
            this.PopUpInstructionNotification(alteredInstructions, manifestInstructionViewModels => tcs.SetResult(manifestInstructionViewModels), title, okButton);
            return tcs.Task;
        }

        public void PopUpImage(byte[] bytes, string message, Action done = null, string title = "", string okButton = "OK")
        {
            Application.SynchronizationContext.Post(ignored =>
            {
                if (CurrentActivity == null)
                    return;

                var customView = CurrentActivity.LayoutInflater.Inflate(Resource.Layout.PopUp_Image, null);
                var imageView = (ImageView)customView.FindViewById(Resource.Id.popUpImageView);

                var converter = new MvxInMemoryImageValueConverter();
                var bitmap = (Bitmap)converter.Convert(bytes, typeof(Bitmap), null, null);
                imageView.SetImageBitmap(bitmap);

                // Scale the image view to use maximum width
                SetImageViewSize(customView, imageView, bitmap);

                new AlertDialog.Builder(CurrentActivity)
                    .SetView(customView)
                    .SetMessage(message)
                    .SetTitle(title)
                    .SetPositiveButton(okButton, (s, e) =>
                        {
                            if (done != null)
                                done();
                        })
                    .Show();

            }, null);
        }

        public Task PopUpImageAsync(byte[] bytes, string message, string title = "", string okButton = "OK")
        {
            var tcs = new TaskCompletionSource<object>();
            PopUpImage(bytes, message, () => tcs.SetResult(null), title, okButton);
            return tcs.Task;
        }

        /// <summary>
        /// This method is called when new instruction that has been polled and if the user is currently viewing that instruction then
        /// it causes a modal pop up to appear stating that the instruction has been altered and what action will be taken
        /// </summary>
        /// <param name="message">The message to appear in the pop up</param>
        /// <param name="done">The action that is taken when the postive button is pressed</param>
        /// <param name="title">Title of the modal</param>
        /// <param name="okButton">The text of the positive button</param>
        public void Alert(string message, Action done = null, string title = "", string okButton = "OK")
        {
            Application.SynchronizationContext.Post(ignored =>
            {
                if (CurrentActivity == null)
                    return;

                //This closes the pop if its showing so it can reopen another, else it will play sound and vibrate first time.

                if (CurrentPopUpAlertDialog != null && CurrentPopUpAlertDialog.IsShowing)
                {
                    try
                    {
                        CurrentPopUpAlertDialog.Dismiss();
                        CurrentPopUpAlertDialog = null;
                    } catch (Exception ex)
                    {
                        throw;
                    }

                }

                var customView = CurrentActivity.LayoutInflater.Inflate(Resource.Layout.PopUp_Alert, null);
                var textView = (TextView)customView.FindViewById(Resource.Id.popUpAlertText);
                textView.Text = message;

                var notificationDialog = new AlertDialog.Builder(CurrentActivity)
                    .SetView(customView)
                    .SetTitle(title)

                    //Prevent the user from closing the pop up by clicking on the sides.
                    .SetCancelable(false)

                    .SetPositiveButton(okButton, delegate
                    {
                        if (done != null)
                            done();
                    });

                CurrentPopUpAlertDialog = notificationDialog.Create();
                CurrentPopUpAlertDialog.Show();

            }, null);
        }

        public Task AlertAsync(string message, string title = "", string okButton = "OK")
        {
            var tcs = new TaskCompletionSource<object>();
            Alert(message, () => tcs.SetResult(null), title, okButton);
            return tcs.Task;
        }

        /// <summary>
        /// This is the same as the IUserInteraction Confirm method however this has got the 'SetCancelable' to false so they have to
        /// click one of the buttons
        /// </summary>
        public void Confirm(string message, Action<bool> answer, string title = null, string okButton = "OK", string cancelButton = "Cancel")
        {
            Application.SynchronizationContext.Post(ignored =>
            {
                bool complete = false; //This is used so that if both options are pressed only 1 is executed
                if (CurrentActivity == null)
                    return;

                //This closes the pop if its showing so it can reopen another, else it will play sound and vibrate first time.

                if (ConfirmDialog != null && ConfirmDialog.IsShowing)
                    return;

                var notificationDialog = new AlertDialog.Builder(CurrentActivity)
                    .SetMessage(message)
                    .SetTitle(title)

                    //Prevents the user from closes the pop up by click on the sides.
                    .SetCancelable(false)

                    .SetPositiveButton(okButton, (s, e) =>
                        {
                            if (answer != null && !complete)
                            {
                                complete = true;
                                answer(true);
                            }

                        })
                    .SetNegativeButton(cancelButton, (s, e) =>
                        {
                            if (answer != null && !complete)
                            {
                                complete = true;
                                answer(false);
                            }

                        });

                ConfirmDialog = notificationDialog.Create();
                ConfirmDialog.Show();
            }, null);
        }

        public Task<bool> ConfirmAsync(string message, string title = "", string okButton = "OK", string cancelButton = "Cancel")
        {
            var tcs = new TaskCompletionSource<bool>();
            Confirm(message, tcs.SetResult, title, okButton, cancelButton);
            return tcs.Task;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets the image view size. Since none of the android "ScaleType" enums deal with scaling
        /// an image up and maintaining aspect ratio, a code solution is required.
        /// </summary>
        private void SetImageViewSize(View parentView, ImageView imageView, Bitmap bitmap)
        {
            var metrics = parentView.Resources.DisplayMetrics;
            int height = metrics.HeightPixels;
            int width = metrics.WidthPixels - 200;

            float bmapHeight = bitmap.Height;
            float bmapWidth = bitmap.Width;

            float wRatio = width / bmapWidth;
            float hRatio = height / bmapHeight;

            float ratioMultiplier = wRatio;

            if (hRatio < wRatio)
                ratioMultiplier = hRatio;

            int newBmapWidth = (int)(bmapWidth * ratioMultiplier);
            int newBmapHeight = (int)(bmapHeight * ratioMultiplier);

            imageView.LayoutParameters.Width = newBmapWidth;
            imageView.LayoutParameters.Height = newBmapHeight;
        }

        private void expandableListView_ChildClick(object sender, ExpandableListView.ChildClickEventArgs e)
        {
            InstructionNotificationDialog.Hide();

            var childSelected = (InstructionGroupedListObject)e.Parent.ExpandableListAdapter.GetChild(e.GroupPosition, e.ChildPosition);

            childSelected.Instructions[e.ChildPosition].OpenMessageModal((result) =>
            {
                if (result)
                {
                    var childList = (InstructionGroupedListObject)e.Parent.ExpandableListAdapter.GetChild(e.GroupPosition, e.ChildPosition);
                    var child = childList.Instructions[e.ChildPosition];
                    child.MobileData.ProgressState = InstructionProgress.Complete;

                    CurrentPopInstructions.Remove(child);
                }

                InstructionNotificationDialog.Show();
            });
        }

        #endregion Private Methods
    }

    /// <summary>
    /// This object is used to display the grouped lists on the instruction notification Popup
    /// </summary>
    public class InstructionGroupedListObject : Java.Lang.Object
    {
        public InstructionGroupedListObject() { Instructions = new List<ManifestInstructionViewModel>(); }

        public List<ManifestInstructionViewModel> Instructions { get; set; }
    }

}