using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MWF.Mobile.Core.Converters
{

    public class JsonWrappedItemConverter<T> : JsonConverter
        where T: class, new()
    {

        public override bool CanConvert(Type objectType)
        {
            return typeof(T).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject;
            try
            {
                jsonObject = JObject.Load(reader);
            }
            catch
            {
                return null;
            }

            var target = new T();
            var wrappedObject = jsonObject.Children().First().Children().First();
            serializer.Populate(wrappedObject.CreateReader(), target);
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

    }

}
