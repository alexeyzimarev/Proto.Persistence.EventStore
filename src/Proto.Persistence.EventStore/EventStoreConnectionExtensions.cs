using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Proto.Persistence.EventStore
{
    public static class EventStoreConnectionExtensions
    {
        private const int MaxReadSize = 4096;
        private static readonly ILogger Log = Proto.Log.CreateLogger<EventStoreProvider>();

        public static async Task<long> SaveEvent(this IEventStoreConnection connection,
            string streamName, object @event, long index, long expectedVersion)
        {
            var esEvents = new[]
            {
                new EventData(
                    Guid.NewGuid(),
                    @event.GetType().GetTypeInfo().Name,
                    true,
                    JsonSerialization.Serialise(@event),
                    JsonSerialization.Serialise(
                        new EventMetadata {CrlTypeName = @event.GetType().FullName, Index = index}))
            };

            WriteResult result;
            try
            {
                result = await connection
                    .AppendToStreamAsync(streamName, expectedVersion, esEvents)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Cannot save events to stream {stream}: {message}", streamName, e.Message);
                throw;
            }

            return result.NextExpectedVersion;
        }

        public static async Task<(IEnumerable<object> Events, long Version)> ReadEvents(
            this IEventStoreConnection connection,
            string streamName, long start, long count)
        {
            var events = new List<object>();
            ResolvedEvent lastEvent;
            try
            {
                long nextPageStart;
                long runningCount = 0;
                do
                {
                    var eventsLeft = count - runningCount;
                    var pageSize = eventsLeft < MaxReadSize ? (int) eventsLeft : MaxReadSize;

                    var slice = await connection
                        .ReadStreamEventsForwardAsync(streamName, start, pageSize, false)
                        .ConfigureAwait(false);

                    if (slice.Status == SliceReadStatus.StreamDeleted || slice.Status == SliceReadStatus.StreamNotFound)
                        return (new List<object>(), -1);

                    runningCount += slice.Events.Length;
                    nextPageStart = !slice.IsEndOfStream ? slice.NextEventNumber : -1;

                    events.AddRange(slice.Events.Select(Deserialize));
                    lastEvent = slice.Events.Last();
                } while (nextPageStart != -1 && runningCount < count);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Cannot read events from stream {stream}: {message}", streamName, e.Message);
                throw;
            }

            var metadata = JsonSerialization.Deserialize<EventMetadata>(lastEvent.Event.Metadata);

            return (events, metadata.Index);
        }

        public static async Task<(object Event, long Version)> ReadLastEvent(this IEventStoreConnection connection,
            string streamName)
        {
            try
            {
                var slice = await connection
                    .ReadStreamEventsBackwardAsync(streamName, StreamPosition.End, 1, true)
                    .ConfigureAwait(false);

                if (slice.Status == SliceReadStatus.StreamDeleted || slice.Status == SliceReadStatus.StreamNotFound)
                    return (null, -1);

                if (!slice.Events.Any()) return (null, 0);

                var @event = Deserialize(slice.Events.First());
                var meta = JsonSerialization.Deserialize<EventMetadata>(slice.Events.First().Event.Metadata);
                return (@event, meta.Index);

            }
            catch (Exception e)
            {
                Log.LogError(e, "Cannot read last event from stream {stream}: {message}", streamName, e.Message);
                throw;
            }
        }

        private static object Deserialize(ResolvedEvent @event)
        {
            var meta = JsonSerialization.Deserialize<EventMetadata>(@event.Event.Metadata);
            var type = Type.GetType(meta.CrlTypeName);
            return JsonSerialization.Deserialize(@event.Event.Data, type);
        }

        internal class EventMetadata
        {
            public string CrlTypeName { get; set; }
            public long Index { get; set; }
        }
    }
}