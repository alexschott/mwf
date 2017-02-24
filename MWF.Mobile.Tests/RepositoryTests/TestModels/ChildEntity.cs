using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Attributes;
using SQLite.Net.Attributes;

namespace MWF.Mobile.Tests.RepositoryTests.TestModels
{
    public class ChildEntity : IBlueSphereEntity
    {

        [Unique]
        public Guid ID { get; set; }

        public string Title { get; set; }

        [ForeignKey(typeof(ParentEntity))]
        public Guid ParentID { get; set; }

    }
}
