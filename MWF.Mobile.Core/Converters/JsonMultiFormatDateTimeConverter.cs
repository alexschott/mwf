using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Converters
{
    public class JsonMultiFormatDateTimeConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //Time formats can be different depending on the service, web app, etc. So will need to accommodate for them accordingly 

            DateTime value;
            var dateTimeString = Convert.ToString(reader.Value);
            var dateTimeStyle = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

            // First try UTC format
            var success = DateTime.TryParseExact(dateTimeString, "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture, dateTimeStyle, out value);

            // Try UK format
            if (!success)
                success = DateTime.TryParseExact(dateTimeString, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, dateTimeStyle, out value);

            // Try US format
            if (!success)
                success = DateTime.TryParseExact(dateTimeString, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture, dateTimeStyle, out value);

            if (!success)
                throw new Exception(string.Format("Unable to parse date/time string {0}", dateTimeString));

            return value.ToLocalTime();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((DateTime)value).ToString("dd/MM/yyyy hh:mm:ss"));
        }

    }
}
