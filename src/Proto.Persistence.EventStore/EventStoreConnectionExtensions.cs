using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Proto.Persistence.EventStore
{
    public static class EventStoreConnectionExtensions
    {
        public static async Task<long> SaveEvents(this IEventStoreConnection connection,
            string streamIdentifier, IEnumerable<object> events)
        {
            var esEvents = events
                .Select(x =>
                    new EventData(
                        Guid.NewGuid(),
                        x.GetType().GetTypeInfo().Name,
                        true,
                        JsonSerialization.Serialise(x),
                        null));

            var result = await connection.AppendToStreamAsync(streamIdentifier, ExpectedVersion.Any, esEvents);
            return result.NextExpectedVersion;
        }

        public static async Task<IEnumerable<object>> ReadEvents(this IEventStoreConnection connection,
            string streamName, long start, long count)
        {
            var slice = await connection.ReadStreamEventsForwardAsync(streamName, start, count, false);
            if (slice.Status == SliceReadStatus.StreamDeleted || slice.Status == SliceReadStatus.StreamNotFound)
                return null;
            
            return !slice.Events.Any() ? null : slice.Events.SelectMany(JsonSerialization.Deserialize);
        }

        public static async Task<object> ReadLastEvent(this IEventStoreConnection connection, string streamName)
        {
            var slice = await connection.ReadStreamEventsBackwardAsync(streamName, StreamPosition.End, 1, true);
            if (slice.Status == SliceReadStatus.StreamDeleted || slice.Status == SliceReadStatus.StreamNotFound)
                return null;

            return !slice.Events.Any() ? null : JsonSerialization.Deserialize(slice.Events.First());
        }
    }
}