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

namespace MWF.Mobile.Android.Helpers
{
    public static class ListViewChildMeasurer
    {

        /// <summary>
        /// Helper method for explicitly making a list view height be based on the height of it's children
        /// This is required when a list view is contained within a scroll view and would other wise only show a single item
        /// </summary>
        /// <param name="listView"></param>
        public static void SetListViewHeightBasedOnChildren(ListView listView)
        {
            IListAdapter listAdapter = listView.Adapter;
            if (listAdapter == null)
                return;

            int desiredWidth = View.MeasureSpec.MakeMeasureSpec(listView.Width, MeasureSpecMode.Unspecified);
            int totalHeight = 0;
            View view = null;
            for (int i = 0; i < listAdapter.Count; i++)
            {
                view = listAdapter.GetView(i, view, listView);
                if (i == 0)
                    view.LayoutParameters = new ViewGroup.LayoutParams(desiredWidth, 0);

                view.Measure(desiredWidth, 0);
                totalHeight += view.MeasuredHeight;
            }
            ViewGroup.LayoutParams layoutParams = listView.LayoutParameters;
            layoutParams.Height = totalHeight + (listView.DividerHeight * (listAdapter.Count - 1));
            listView.LayoutParameters = layoutParams;
            listView.RequestLayout();
        }

    }
}