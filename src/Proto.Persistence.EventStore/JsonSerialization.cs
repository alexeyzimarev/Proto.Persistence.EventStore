using System.Collections.Generic;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Proto.Persistence.EventStore
{
    public static class JsonSerialization
    {
        public static IEnumerable<object> Deserialize(ResolvedEvent resolvedEvent) =>
            JsonConvert.DeserializeObject<List<object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Data));

        public static byte[] Serialise(object @event) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));
    }
}