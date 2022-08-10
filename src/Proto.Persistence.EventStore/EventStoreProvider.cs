using EventStore.Client;

namespace Proto.Persistence.EventStore;

public class EventStoreProvider : IProvider {
    readonly EventStoreClient   _client;
    readonly StreamNameStrategy _eventStreamNameStrategy;
    readonly StreamNameStrategy _snapshotStreamNameStrategy;

    Func<string, Type?> _stringToType;
    Func<Type, string>  _typeToString;

    public EventStoreProvider(EventStoreClient client) {
        _client                     = client;
        _eventStreamNameStrategy    = DefaultStrategy.DefaultEventStreamNameStrategy;
        _snapshotStreamNameStrategy = DefaultStrategy.DefaultSnapshotStreamNameStrategy;

        _stringToType = Type.GetType;
        _typeToString = type => type.AssemblyQualifiedName!;
    }

    public EventStoreProvider(
        EventStoreClient   client,
        StreamNameStrategy eventStreamNameStrategy,
        StreamNameStrategy snapshotStreamNameStrategy
    ) {
        _client                     = client;
        _eventStreamNameStrategy    = eventStreamNameStrategy;
        _snapshotStreamNameStrategy = snapshotStreamNameStrategy;

        _stringToType = Type.GetType;
        _typeToString = type => type.AssemblyQualifiedName!;
    }

    public EventStoreProvider WithTypeResolver(Func<Type, string> typeToString, Func<string, Type> stringToType) {
        _stringToType = stringToType;
        _typeToString = typeToString;
        return this;
    }

    public async Task<long> GetEventsAsync(
        string         actorName,
        long           indexStart,
        long           indexEnd,
        Action<object> callback
    ) {
        var count = indexEnd == long.MaxValue ? indexEnd - 1 : indexEnd - indexStart + 1;
        var start = indexStart;

        if (indexStart > 0) {
            start = indexStart - 1;
            count++;
        }

        var slice = await _client.ReadEvents(_eventStreamNameStrategy(actorName), start, count, _stringToType!);

        var events = slice.Events.ToList();
        if (start != indexStart && events.Count > 0) events.RemoveAt(0);

        foreach (var @event in events) {
            callback(@event);
        }

        return slice.Version;
    }

    public async Task<(object? Snapshot, long Index)> GetSnapshotAsync(string actorName) {
        var @event = await _client.ReadLastEvent(_snapshotStreamNameStrategy(actorName), _stringToType!);

        return (@event.Event, @event.Version);
    }

    public Task<long> PersistEventAsync(string actorName, long index, object @event)
        => _client.SaveEvent(_eventStreamNameStrategy(actorName), @event, index, index, _typeToString);

    public Task PersistSnapshotAsync(string actorName, long index, object snapshot)
        => _client.SaveEvent(
            _snapshotStreamNameStrategy(actorName),
            snapshot,
            index,
            -1,
            _typeToString
        );

    public Task DeleteEventsAsync(string actorName, long inclusiveToIndex)
        => throw new NotSupportedException("Deleting events is not supported by EventStore");

    public Task DeleteSnapshotsAsync(string actorName, long inclusiveToIndex)
        => throw new NotSupportedException("Deleting snapshots is not supported by EventStore");
}
