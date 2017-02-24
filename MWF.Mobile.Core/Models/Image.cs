using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite.Net.Attributes;
using Newtonsoft.Json;
using MWF.Mobile.Core.Models.Attributes;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models
{
    // Model class which holds an image associated with a safety check fault
    [XmlType("image")]
    public class Image : IBlueSphereEntity
    {

        private string _encodeImageData;
        private byte[] _bytes;


        [Unique]
        [PrimaryKey]
        [XmlAttribute("id")]
        public Guid ID { get; set; }

        [XmlAttribute("sequence")]
        public int Sequence { get; set; }

        [XmlAttribute("encoded")]
        public string EncodeImageData
        {
            get { return _encodeImageData; }
            set
            {
                _encodeImageData = value;
                if (_encodeImageData != null && (_encodeImageData.Length % 4 == 0))
                {
                    _bytes = Convert.FromBase64String(_encodeImageData);
                }
            }
        }

        [XmlAttribute("filename")]
        public string Filename { get; set; }

        [ForeignKey(typeof(SafetyCheckFault))]
        [XmlIgnore]
        public Guid SafetyCheckFaultID
        { get; set; }

        [XmlIgnore]
        [Ignore]
        // Raw
        public byte[] Bytes
        {
            get { return _bytes; }
            set
            {
                _bytes = value;
                _encodeImageData = Convert.ToBase64String(_bytes);
            }
        }

    }


    public class UploadCameraImageObject : IBlueSphereEntity
    {
        public Guid ID { get; set; }

        public Guid DriverId { get; set; }

        public string DriverTitle { get; set; }

        public string Smp { get; set; }

        public List<Guid> MobileApplicationIDs { get; set; }

        public string OrderIDs { get; set; }

        public string Comment { get; set; }

        public DateTime DateTimeOfUpload { get; set; }

        public List<Image> Pictures { get; set; }
    }
}
