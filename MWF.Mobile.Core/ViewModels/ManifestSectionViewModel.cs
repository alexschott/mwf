using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models.Instruction;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{
    public class ManifestSectionViewModel : MvxViewModel, IEnumerable<ManifestInstructionViewModel>
    {

        public ManifestSectionViewModel(ManifestViewModel manifestViewModel)
        {
            _manifestViewModel = manifestViewModel;
            _instructions = new ObservableCollection
                        <ManifestInstructionViewModel>();
        }

        private ManifestViewModel _manifestViewModel;


        private ObservableCollection<ManifestInstructionViewModel> _instructions;
        public ObservableCollection<ManifestInstructionViewModel> Instructions
        {
            get
            {
                return _instructions;
            }
            set
            {
                _instructions = value;
                RaisePropertyChanged(() => Instructions);
            }
        }

        private string _sectionHeader;
        public string SectionHeader
        {
            get { return _sectionHeader; }
            set { _sectionHeader = value; RaisePropertyChanged(() => SectionHeader); }
        }

        public IEnumerator<ManifestInstructionViewModel> GetEnumerator()
        {
            return Instructions.GetEnumerator();
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
