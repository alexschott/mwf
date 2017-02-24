using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Converters
{
    public class SingleObjectToListConverter<T> : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
                return serializer.Deserialize<List<T>>(reader);
            else
            {
                T t = serializer.Deserialize<T>(reader);
                return new List<T>(new[] {t});
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
