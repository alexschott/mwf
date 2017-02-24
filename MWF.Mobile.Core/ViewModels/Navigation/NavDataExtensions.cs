using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.ViewModels.Navigation;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Repositories;

namespace MWF.Mobile.Core.ViewModels.Navigation.Extensions
{
    public static class NavDataHelper
    {
        public static MobileApplicationDataChunkContentActivity GetDataChunk(this NavData navData)
        {
            object obj = null;
            MobileApplicationDataChunkContentActivity dataChunk;
            navData.OtherData.TryGetValue("DataChunk", out obj);

            if (obj == null)
            {
                dataChunk = new MobileApplicationDataChunkContentActivity();
                navData.OtherData["DataChunk"] = dataChunk;
            }
            else
            {
                dataChunk = obj as MobileApplicationDataChunkContentActivity;
            }

            return dataChunk;
        }

        public static MobileApplicationDataChunkContentActivity GetAdditionalDataChunk(this NavData navData, MobileData mobileData)
        {
            object obj = null;
            Dictionary<Guid, MobileApplicationDataChunkContentActivity> dataChunks;
            
            if (!navData.OtherData.TryGetValue("AdditionalDataChunks", out obj))
            {
                dataChunks = new Dictionary<Guid, MobileApplicationDataChunkContentActivity>();
                navData.OtherData["AdditionalDataChunks"] = dataChunks;
            }
            else
            {
                dataChunks = obj as Dictionary<Guid, MobileApplicationDataChunkContentActivity>;
            }

            MobileApplicationDataChunkContentActivity dataChunk;

            if (!dataChunks.TryGetValue(mobileData.ID, out dataChunk))
            {
                dataChunk = new MobileApplicationDataChunkContentActivity();
                dataChunks[mobileData.ID] = dataChunk;
            }

            return dataChunk;
        }

        public static IEnumerable<MobileApplicationDataChunkContentActivity> GetAllDataChunks(this NavData<MobileData> navData)
        {
            List<MobileApplicationDataChunkContentActivity> dataChunks = new List<MobileApplicationDataChunkContentActivity>();

            dataChunks.Add(navData.GetDataChunk());

            var additionalInstructions = navData.GetAdditionalInstructions();

            foreach (var additionalInstruction in additionalInstructions)
            {
                 dataChunks.Add(navData.GetAdditionalDataChunk(additionalInstruction));
            }

            return dataChunks;
        }

        public static List<MobileData> GetAdditionalInstructions(this NavData<MobileData> navData)
        {
            if (!navData.OtherData.IsDefined("AdditionalInstructions"))
            {
                navData.OtherData["AdditionalInstructions"] = new List<MobileData>();
            }          

            return navData.OtherData["AdditionalInstructions"] as List<MobileData>;
        }

        public static IEnumerable<MobileData> GetAllInstructions(this NavData<MobileData> navData)
        {
            List<MobileData> mobileDatas = new List<MobileData>();
            mobileDatas.Add(navData.Data);
            mobileDatas.AddRange(navData.GetAdditionalInstructions());

            return mobileDatas;
        }

        public static DeliveryOptions GetWorseCaseDeliveryOptions(this NavData<MobileData> navData)
        {
            bool customerNameRequiredForDelivery = false;
            bool customerSignatureRequiredForDelivery = false;
            bool barcodeScanRequiredForDelivery = false;
            bool bypassCommentsScreen = true;
            bool bypassCleanClausedScreen = true;


            List<MobileData> instructions = new List<MobileData>() { navData.Data };
            var additionalInstructions = navData.GetAdditionalInstructions();
            instructions.AddRange(additionalInstructions);

            foreach (var instruction in instructions)
            {
                customerNameRequiredForDelivery = customerNameRequiredForDelivery || instruction.Order.Additional.CustomerNameRequiredForDelivery;
                customerSignatureRequiredForDelivery = customerSignatureRequiredForDelivery || instruction.Order.Additional.CustomerSignatureRequiredForDelivery;
                barcodeScanRequiredForDelivery = barcodeScanRequiredForDelivery || instruction.Order.Items.Any(i => i.Additional.BarcodeScanRequiredForDelivery);
                bypassCleanClausedScreen = bypassCleanClausedScreen && !instruction.Order.Items.Any(i => !i.Additional.BypassCleanClausedScreen);
                bypassCommentsScreen = bypassCommentsScreen && !instruction.Order.Items.Any(i => !i.Additional.BypassCommentsScreen);
            }

            return new DeliveryOptions(customerNameRequiredForDelivery,
                                       customerSignatureRequiredForDelivery,
                                       barcodeScanRequiredForDelivery,
                                       bypassCommentsScreen,
                                       bypassCleanClausedScreen);
           
        }


        public static bool IsDefined(this Dictionary<string,object> dict, string key)
        {
            if (!dict.ContainsKey(key)) return false;
            else return dict[key] != null;
        }
    }

    public class DeliveryOptions
    {
        private bool _customerNameRequiredForDelivery;
        private bool _customerSignatureRequiredForDelivery;
        private bool _barcodeScanRequiredForDelivery;
        private bool _bypassCommentsScreen;
        private bool _bypassCleanClausedScreen;


        public DeliveryOptions(bool customerNameRequiredForDelivery, 
                                bool customerSignatureRequiredForDelivery,
                                bool barcodeScanRequiredForDelivery,
                                bool bypassCommentsScreen,
                                bool bypassCleanClausedScreen
                                )
        {
            _customerNameRequiredForDelivery = customerNameRequiredForDelivery;
            _customerSignatureRequiredForDelivery = customerSignatureRequiredForDelivery;
            _barcodeScanRequiredForDelivery = barcodeScanRequiredForDelivery;
            _bypassCommentsScreen = bypassCommentsScreen;
            _bypassCleanClausedScreen = bypassCleanClausedScreen;
        }

        public bool CustomerNameRequiredForDelivery
        {
            get { return _customerNameRequiredForDelivery; }
        }

        public bool CustomerSignatureRequiredForDelivery
        {
            get { return _customerSignatureRequiredForDelivery; }
        }

        public bool BarcodeScanRequiredForDelivery
        {
            get { return _barcodeScanRequiredForDelivery; }
        }

        public bool BypassCommentsScreen
        {
            get { return _bypassCommentsScreen; }
        }

        public bool BypassCleanClausedScreen
        {
            get { return _bypassCleanClausedScreen; }
        }


    }

}
