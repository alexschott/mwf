using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Ploeh.AutoFixture.Kernel;

namespace MWF.Mobile.Tests.Helpers
{
    public class OmitPropertySpecimenBuilder : ISpecimenBuilder
    {
        string _propertyName;

        public OmitPropertySpecimenBuilder(string propertyName)
        {
            _propertyName = propertyName;
        }

        public object Create(object request, ISpecimenContext context)
        {
            var propertyInfo = request as PropertyInfo;
            if (propertyInfo != null &&
                propertyInfo.Name.Equals(_propertyName) &&
                propertyInfo.PropertyType == typeof(string))
            {
                return new OmitSpecimen();
            }

            return new NoSpecimen(request);
        }
    }

    public class OmitPropertySpecimenBuilder<T> : ISpecimenBuilder where T : class
    {
        string _propertyName;

        public OmitPropertySpecimenBuilder(string propertyName)
        {
            _propertyName = propertyName;
        }

        public object Create(object request, ISpecimenContext context)
        {
            var propertyInfo = request as PropertyInfo;
            if (propertyInfo != null &&
                propertyInfo.Name.Equals(_propertyName) &&
                propertyInfo.PropertyType == typeof(string) &&
                propertyInfo.DeclaringType == typeof(T))
            {
                return new OmitSpecimen();
            }

            return new NoSpecimen(request);
        }
    }
}
