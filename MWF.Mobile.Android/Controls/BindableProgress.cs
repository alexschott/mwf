using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views;

namespace MWF.Mobile.Android.Controls
{

    public class BindableProgress : View
    {

        private readonly Context _context;
        private ProgressDialog _dialog;

        public BindableProgress(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            _context = context;
            this.HookUpFragmentChangeHandler();
        }

        public BindableProgress(Context context)
            : this(context, null)
        { }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title)
                    return;

                _title = value;

                if (_dialog != null)
                    _dialog.SetTitle(value);
            }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                if (value == _message)
                    return;

                _message = value;

                if (_dialog != null)
                    _dialog.SetMessage(value);
            }
        }

        public bool ShowProgress
        {
            get { return _dialog != null; }
            set
            {
                if (value == this.ShowProgress)
                    return;
                
                if (value)
                {
                    _dialog = new ProgressDialog(_context);
                    _dialog.SetTitle(Title);
                    _dialog.SetMessage(Message);
                    _dialog.Show();
                    _dialog.SetCanceledOnTouchOutside(false);
                }
                else
                {
                    _dialog.Hide();
                    _dialog = null;
                }
            }
        }

        private void HookUpFragmentChangeHandler()
        {
            var activityView = _context as Views.BaseActivityView;

            if (activityView == null)
            {
                var contextWrapper = _context as ContextWrapper;

                if (contextWrapper != null)
                    activityView = contextWrapper.BaseContext as Views.BaseActivityView;
            }

            if (activityView != null)
                activityView.OnFragmentChanged += ActivityView_OnFragmentChanged;
        }

        private void ActivityView_OnFragmentChanged(object sender, EventArgs e)
        {
            this.ShowProgress = false;
        }

    }

}
