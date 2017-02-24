using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using MWF.Mobile.Core.Models.Attributes;
using System.Xml.Serialization;
using System.Xml;

namespace MWF.Mobile.Core.Models
{
    // Model class which represents a fault for a specific safety check item (SafetyCheckFaultType)
    [XmlType("fault")]
    public class SafetyCheckFault : IBlueSphereEntity
    {

        public SafetyCheckFault()
        {
            ID = Guid.NewGuid();
            Images = new List<Image>();
        }

        [Unique]
        [PrimaryKey]
        [XmlAttribute("id")]
        public Guid ID { get; set; }

        [XmlAttribute("title")]
        public string Title { get; set; }

        [XmlAttribute("faultid")]
        public Guid FaultTypeID { get; set; }

        [XmlAttribute("reference")]
        public string FaultTypeReference { get; set; }

        [XmlAttribute("comment")]
        public string Comment { get; set; }

        [XmlIgnore]
        public bool IsDiscretionaryQuestion { get; set; }

        [XmlIgnore]
        public bool IsDiscretionaryPass { get; set; }

        [XmlAttribute("discretionaryPassAllowDrive")]
        public int IsDiscretionaryPassXml
        {
            get
            {
                return Convert.ToInt32(IsDiscretionaryPass);
            }
            set
            {
                value = Convert.ToInt32(IsDiscretionaryPass);
            }
        }

        [ChildRelationship(typeof(Image))]
        [XmlArray("images")]
        public List<Image> Images { get; set; }

        [ForeignKey(typeof(SafetyCheckData))]
        [XmlIgnore]
        public Guid SafetyCheckDataID { get; set; }

        [XmlIgnore]
        public Enums.SafetyCheckStatus Status { get; set; }

        #region Public Methods

        public SafetyCheckFault Clone()
        {
            //simple memberwise clone deal with all value types
            SafetyCheckFault clone = this.MemberwiseClone() as SafetyCheckFault;

            // deep copy of images

            clone.Images = new List<Image>();

            foreach (Image image in this.Images)
            {
                clone.Images.Add(image);
            }

            return clone;
        }

        //Sets the safety check fault with member values from another safety check fault
        public void ValuesFrom(SafetyCheckFault sourceFault)
        {

            this.ID = sourceFault.ID;
            this.FaultTypeID = sourceFault.FaultTypeID;
            this.Comment = sourceFault.Comment;
            this.IsDiscretionaryPass = sourceFault.IsDiscretionaryPass;
            this.FaultTypeReference = sourceFault.FaultTypeReference;
            this.SafetyCheckDataID = sourceFault.SafetyCheckDataID;

            this.Images.Clear();

            foreach (Image image in sourceFault.Images)
            {
                this.Images.Add(image);
            }

        }

        #endregion

    }
}
