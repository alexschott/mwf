using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Attributes;

namespace MWF.Mobile.Tests.RepositoryTests.TestModels
{
    public class ChildEntity2 : IBlueSphereEntity
    {
        [Unique]
        public Guid ID { get; set; }

        public string Title { get; set; }

        [ForeignKey(typeof(ParentEntity))]
        public Guid ParentID { get; set; }

    }
}
