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
using System.Collections;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Android.Portable;

namespace MWF.Mobile.Android.Controls
{
    public class ExpandableListAdapter : BaseExpandableListAdapter
    {

        private Context Context;
        private List<string> Groups;
        private List<InstructionGroupedListObject> Children;

        public ExpandableListAdapter(Context context, List<string> groups, List<InstructionGroupedListObject> children)
        {
            this.Context = context;
            this.Groups = groups;
            this.Children = children;
        }

        public override Java.Lang.Object GetChild(int groupPosition, int childPosition)
        {
            return (Java.Lang.Object)Children[groupPosition];
        }

        public override long GetChildId(int groupPosition, int childPosition)
        {
            return childPosition;
        }

        public override View GetChildView(int groupPosition, int childPosition, bool isLastChild, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                LayoutInflater infaInflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
                convertView = infaInflater.Inflate(Resource.Layout.Item_InstructionNotificationText, null);
            }

            var group = (InstructionGroupedListObject)GetChild(groupPosition, childPosition);
            var child = group.Instructions[childPosition];
            var text = (child.InstructionType == Core.Enums.InstructionType.OrderMessage)
            ? child.MobileData.MessageText
            : string.Format("{0} ({1})", child.MobileData.Order.Description, child.MobileData.Order.Description2);

            TextView tv = (TextView)convertView.FindViewById(Resource.Id.itemText);
            tv.SetText(text, TextView.BufferType.Normal);

            return convertView;
        }

        public override int GetChildrenCount(int groupPosition)
        {
            return Children[groupPosition].Instructions.Count;
        }

        public override Java.Lang.Object GetGroup(int groupPosition)
        {
            return Groups[groupPosition];
        }

        public override long GetGroupId(int groupPosition)
        {
            return groupPosition;
        }

        public override View GetGroupView(int groupPosition, bool isExpanded, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                LayoutInflater infalInflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
                convertView = infalInflater.Inflate(Resource.Layout.Item_InstructionNotifcationHeader, null);
            }

            string group = (string)GetGroup(groupPosition);
            TextView tv = (TextView)convertView.FindViewById(Resource.Id.listHeader);
            tv.SetText(group, TextView.BufferType.Normal);

            return convertView;
        }

        public override int GroupCount
        {
            get { return Groups.Count; }
        }

        public override bool HasStableIds
        {
            get { return true; }
        }

        public override bool IsChildSelectable(int groupPosition, int childPosition)
        {
            var child = Children[groupPosition].Instructions[childPosition];

            return child.InstructionType == Core.Enums.InstructionType.OrderMessage;
        }
    }

    public class Section
    {
        public Section(string caption, ArrayAdapter adapter)
        {
            this.Caption = caption;
            this.Adapter = adapter;
        }

        public string Caption { get; set; }

        public ArrayAdapter Adapter { get; set; }
    }
}