using System;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Proto.Persistence.EventStore
{
    public static class JsonSerialization
    {
        public static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static T Deserialize<T>(byte[] data) =>
            JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data), DefaultSettings);

        public static object Deserialize(byte[] data, Type type) =>
            JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), type, DefaultSettings);

        public static byte[] Serialise(object @event) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, DefaultSettings));
    }
}