using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PercentageDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<List<int>, decimal>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionary = (Dictionary<List<int>, decimal>)value;
            var jObject = new JObject();

            foreach (var kvp in dictionary)
            {
                string key = string.Join(",", kvp.Key);
                jObject.Add(key, JToken.FromObject(kvp.Value, serializer));
            }

            jObject.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dictionary = new Dictionary<List<int>, decimal>();
            var jObject = JObject.Load(reader);

            foreach (var kvp in jObject)
            {
                List<int> key = kvp.Key.Split(',').Select(int.Parse).ToList();
                decimal value = kvp.Value.ToObject<decimal>(serializer);
                dictionary[key] = value;
            }

            return dictionary;
        }
    }

}
