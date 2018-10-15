using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Proto.Persistence.EventStore
{
    public class EventStoreProvider : IProvider
    {
        private readonly IEventStoreConnection _connection;
        private readonly StreamNameStrategy _eventStreamNameStrategy;
        private readonly StreamNameStrategy _snapshotStreamNameStrategy;

        public EventStoreProvider(IEventStoreConnection connection)
        {
            _connection = connection;
            _eventStreamNameStrategy = DefaultStrategy.DefaultEventStreamNameStrategy;
            _snapshotStreamNameStrategy = DefaultStrategy.DefaultSnapshotStreamNameStrategy;
        }

        public EventStoreProvider(IEventStoreConnection connection,
            StreamNameStrategy eventStreamNameStrategy,
            StreamNameStrategy snapshotStreamNameStrategy)
        {
            _connection = connection;
            _eventStreamNameStrategy = eventStreamNameStrategy;
            _snapshotStreamNameStrategy = snapshotStreamNameStrategy;
        }
        
        public async Task<long> GetEventsAsync(string actorName, long indexStart, long indexEnd, Action<object> callback)
        {
            var count = indexEnd == long.MaxValue ? indexEnd : indexEnd - indexStart + 1;
            var events = await _connection.ReadEvents(_eventStreamNameStrategy(actorName), indexStart, count);

            foreach (var @event in events.Events)
            {
                callback(@event);
            }

            return events.Version;
        }

        public async Task<(object Snapshot, long Index)> GetSnapshotAsync(string actorName)
        {
            var @event = await _connection.ReadLastEvent(_snapshotStreamNameStrategy(actorName));

            return (@event.Event, @event.Version);
        }

        public Task<long> PersistEventAsync(string actorName, long index, object @event)
            => _connection.SaveEvent(_eventStreamNameStrategy(actorName), @event, index, index - 1);

        public Task PersistSnapshotAsync(string actorName, long index, object snapshot)
            => _connection.SaveEvent(_snapshotStreamNameStrategy(actorName), snapshot, index, ExpectedVersion.Any);

        public Task DeleteEventsAsync(string actorName, long inclusiveToIndex)
        {
            throw new NotSupportedException("Deleting events is not supported by EventStore");
        }

        public Task DeleteSnapshotsAsync(string actorName, long inclusiveToIndex)
        {
            throw new NotSupportedException("Deleting snapshots is not supported by EventStore");
        }
    }
}