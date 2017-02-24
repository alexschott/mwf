using Cirrious.MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.CrossCore;

namespace MWF.Mobile.Core.ViewModels
{
    public class BarcodeSectionViewModel: MvxViewModel, IEnumerable<BarcodeItemViewModel>
    {

        private BarcodeScanningViewModel _barcodeScanningViewModel;

        public BarcodeSectionViewModel(BarcodeScanningViewModel barcodeScanningViewModel)
        {
            _barcodes = new ObservableCollection<BarcodeItemViewModel>();
            _barcodeScanningViewModel = barcodeScanningViewModel;
        }

        private ObservableCollection<BarcodeItemViewModel> _barcodes;
        public ObservableCollection<BarcodeItemViewModel> Barcodes
        {
            get
            {
                return _barcodes;
            }
            set
            {
                _barcodes = value;
                RaisePropertyChanged(() => Barcodes);
            }
        }

        private string _sectionHeader;
        public string SectionHeader
        {
            get { return _sectionHeader; }
            set { _sectionHeader = value; RaisePropertyChanged(() => SectionHeader); }
        }

        public IEnumerator<BarcodeItemViewModel> GetEnumerator()
        {
            return Barcodes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
