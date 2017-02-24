using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using Android.Views.InputMethods;

namespace MWF.Mobile.Android.Controls
{
    /// <summary>
    /// 
    /// Based it around this.
    /// 
    /// https://github.com/danialgoodwin/android-widget-keyboardless-edittext/blob/master/KeyboardlessEditText2.java#L119
    /// </summary>
    public class KeyboardlessEditText : EditText
    {
        public KeyboardlessEditText(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            InputType = global::Android.Text.InputTypes.TextFlagNoSuggestions;
            FocusableInTouchMode = true;
        }

        public KeyboardlessEditText(Context context)
            : base(context)
        {
            InputType = global::Android.Text.InputTypes.TextFlagNoSuggestions;
            FocusableInTouchMode = true;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            var ret = base.OnTouchEvent(e);

            hideKeyboard();

            return ret;
        }

        protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, global::Android.Graphics.Rect previouslyFocusedRect)
        {
            base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
            hideKeyboard();
            SetCursorVisible(true);

            if (!gainFocus)
                this.PerformClick();
        }

        public override bool CallOnClick()
        {
            SetCursorVisible(true);
            return true;
        }

        //public override bool OnCheckIsTextEditor()
        //{
        //    base.OnCheckIsTextEditor();
        //    return false;
        //}

        private void hideKeyboard()
        {
            InputMethodManager mgr = (InputMethodManager)this.Context.GetSystemService(Context.InputMethodService);
            mgr.HideSoftInputFromWindow(this.WindowToken, 0);
        }
    }
}