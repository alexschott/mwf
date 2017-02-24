using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{
    public class DamageStatus
    {

        public string Text
        {
            get;
            set;
        }

        public string Code
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Text;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as DamageStatus;
            if (rhs == null)
                return false;
            return rhs.Text == Text;
        }

        public override int GetHashCode()
        {
            if (Text == null)
                return 0;
            return Text.GetHashCode();
        }

    }
}
