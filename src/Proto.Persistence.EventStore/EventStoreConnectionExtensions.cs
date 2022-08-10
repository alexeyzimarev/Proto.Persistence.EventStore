using System.Reflection;
using System.Text.Json;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Proto.Persistence.EventStore;

public static class EventStoreConnectionExtensions {
    const int MaxReadSize = 4096;

    static readonly ILogger Log = Proto.Log.CreateLogger<EventStoreProvider>();

    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task<long> SaveEvent(
        this EventStoreClient client,
        string                streamName,
        object                @event,
        long                  index,
        long                  expectedIndex,
        Func<Type, string>    typeToString,
        CancellationToken     cancellationToken = default
    ) {
        var esEvents = new[] {
            new EventData(
                Uuid.NewUuid(),
                @event.GetType().GetTypeInfo().Name,
                JsonSerializer.SerializeToUtf8Bytes(@event),
                JsonSerializer.SerializeToUtf8Bytes(
                    new EventMetadata { CrlTypeName = typeToString(@event.GetType()), Index = index }
                )
            )
        };

        try {
            IWriteResult result;

            if (expectedIndex <= 0) {
                var streamState = index == 0 ? StreamState.NoStream : StreamState.Any;

                result = await client
                    .AppendToStreamAsync(
                        streamName,
                        streamState,
                        esEvents,
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            else {
                result = await client
                    .AppendToStreamAsync(
                        streamName,
                        StreamRevision.FromInt64(index - 1),
                        esEvents,
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            return result.NextExpectedStreamRevision.ToInt64();
        }
        catch (Exception e) {
            Log.LogError(e, "Cannot save events to stream {stream}: {message}", streamName, e.Message);
            throw;
        }
    }

    public static async Task<(IEnumerable<object> Events, long Version)> ReadEvents(
        this EventStoreClient client,
        string                streamName,
        long                  start,
        long                  count,
        Func<string, Type>    stringToType
    ) {
        var  events    = new List<object>();
        long lastIndex = 0;

        try {
            long runningCount = 0;
            var  position     = StreamPosition.FromInt64(start);

            while (true) {
                var eventsLeft = count - runningCount;
                var pageSize   = eventsLeft < MaxReadSize ? (int)eventsLeft : MaxReadSize;

                var slice = await client
                    .ReadStreamAsync(Direction.Forwards, streamName, position, pageSize)
                    .ToArrayAsync()
                    .ConfigureAwait(false);

                var sliceEvents = slice.Select(x => Deserialize(x, stringToType)).ToList();
                events.AddRange(sliceEvents.Select(x => x.@event).Where(x => x != null)!);
                lastIndex = sliceEvents.Any() ? sliceEvents.Last().index : lastIndex;

                runningCount += slice.Length;

                if (runningCount >= count || slice.Length < pageSize) {
                    break;
                }

                position = slice.Last().OriginalEventNumber.Next();
            }
        }
        catch (StreamNotFoundException) {
            return (new List<object>(), -1);
        }
        catch (Exception e) {
            Log.LogError(e, "Cannot read events from stream {stream}: {message}", streamName, e.Message);
            throw;
        }

        return (events, lastIndex);
    }

    public static async Task<(object? Event, long Version)> ReadLastEvent(
        this EventStoreClient client,
        string                streamName,
        Func<string, Type>    stringToType
    ) {
        try {
            var slice = await client
                .ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1, true)
                .ToArrayAsync()
                .ConfigureAwait(false);

            if (slice.Length == 0) return (null, 0);

            var @event = Deserialize(slice[0], stringToType);
            return (@event.@event, @event.index);
        }
        catch (StreamNotFoundException) {
            return (null, -1);
        }
        catch (Exception e) {
            Log.LogError(e, "Cannot read last event from stream {stream}: {message}", streamName, e.Message);
            throw;
        }
    }

    static (object? @event, long index) Deserialize(ResolvedEvent @event, Func<string, Type> stringToType) {
        var meta = JsonSerializer.Deserialize<EventMetadata>(@event.Event.Metadata.Span, Options);
        var type = stringToType(meta!.CrlTypeName);
        return (JsonSerializer.Deserialize(@event.Event.Data.Span, type, Options), meta.Index);
    }

    internal class EventMetadata {
        public string CrlTypeName { get; set; } = null!;
        public long   Index       { get; set; }
    }
}
