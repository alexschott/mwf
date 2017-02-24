using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using Cirrious.MvvmCross.Binding.BindingContext;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Android.Controls;

namespace MWF.Mobile.Android.Views.Fragments
{

    public class ConfirmTimesFragment : BaseFragment
    {

        private TimePicker onSiteTimePicker = null;
        private TimePicker completeTimePicker = null;
        private DatePicker onSiteDatePicker = null;
        private DatePicker completeDatePicker = null;
        private LinearLayout onSiteLinearLayout = null;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_ConfirmTimes, null);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            onSiteTimePicker = view.FindViewById<TimePicker>(Resource.Id.onSiteTimePicker);
            completeTimePicker = view.FindViewById<TimePicker>(Resource.Id.completeTimePicker);
            onSiteDatePicker = view.FindViewById<DatePicker>(Resource.Id.onSiteDatePicker);
            completeDatePicker = view.FindViewById<DatePicker>(Resource.Id.completeDatePicker);
            onSiteLinearLayout = view.FindViewById<LinearLayout>(Resource.Id.onSiteLinearLayout);

            // Set to 24 Hour mode
            onSiteTimePicker.SetIs24HourView(Java.Lang.Boolean.True);
            completeTimePicker.SetIs24HourView(Java.Lang.Boolean.True);

            // time picker event handlers
            onSiteTimePicker.TimeChanged += _onSiteTimePicker_TimeChanged;
            completeTimePicker.TimeChanged += _completeTimePicker_TimeChanged;

            var onSiteDateTime = ((ConfirmTimesViewModel)this.ViewModel).OnSiteDateTime;

            // binding and Time Pickers just does not work
            onSiteTimePicker.CurrentHour = (Java.Lang.Integer)onSiteDateTime.TimeOfDay.Hours;
            onSiteTimePicker.CurrentMinute = (Java.Lang.Integer)onSiteDateTime.TimeOfDay.Minutes ;

            completeTimePicker.CurrentHour = (Java.Lang.Integer)DateTime.Now.Hour;
            completeTimePicker.CurrentMinute = (Java.Lang.Integer)DateTime.Now.Minute;

            //onSiteDatePicker.UpdateDate(onSiteDateTime.Year, onSiteDateTime.Month, onSiteDateTime.Day);
            onSiteDatePicker.FocusChange += OnSiteDatePicker_FocusChange;
            completeDatePicker.FocusChange += CompleteDatePicker_FocusChange;

            if (!((ConfirmTimesViewModel)this.ViewModel).ShowOnSiteConfirmation)
                onSiteLinearLayout.Visibility = ViewStates.Gone;
        }

        private void CompleteDatePicker_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            var dateTime = completeDatePicker.DateTime;
            var time = ((ConfirmTimesViewModel)this.ViewModel).CompleteDateTime .TimeOfDay;
            ((ConfirmTimesViewModel)this.ViewModel).CompleteDateTime = dateTime.Date.Add(time);
        }

        private void OnSiteDatePicker_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            var dateTime = onSiteDatePicker.DateTime;
            var time = ((ConfirmTimesViewModel)this.ViewModel).OnSiteDateTime.TimeOfDay;
            ((ConfirmTimesViewModel)this.ViewModel).OnSiteDateTime = dateTime.Date.Add(time);
        }

        private void _completeTimePicker_TimeChanged(object sender, TimePicker.TimeChangedEventArgs e)
        {
            var dateTime = completeDatePicker.DateTime;
            var time = new TimeSpan(e.HourOfDay, e.Minute, 0);

            ((ConfirmTimesViewModel)this.ViewModel).CompleteDateTime = dateTime.Date.Add(time);
        }

        private void _onSiteTimePicker_TimeChanged(object sender, TimePicker.TimeChangedEventArgs e)
        {
            var dateTime = onSiteDatePicker.DateTime;
            var time = new TimeSpan(e.HourOfDay, e.Minute, 0);

            ((ConfirmTimesViewModel)this.ViewModel).OnSiteDateTime = dateTime.Date.Add(time); 
        }

    }
    
}