using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Proto.Persistence.EventStore
{
    public class EventStoreProvider : IProvider
    {
        private readonly IEventStoreConnection _connection;
        private readonly StreamNameStrategy _eventStreamNameStrategy;
        private readonly StreamNameStrategy _checkpointStreamNameStrategy;

        public EventStoreProvider(IEventStoreConnection connection)
        {
            _connection = connection;
            _eventStreamNameStrategy = DefaultStrategy.DefaultEventStreamNameStrategy;
            _checkpointStreamNameStrategy = DefaultStrategy.DefaultSnapshotStreamNameStrategy;
        }

        public EventStoreProvider(IEventStoreConnection connection,
            StreamNameStrategy eventStreamNameStrategy,
            StreamNameStrategy checkpointStreamNameStrategy)
        {
            _connection = connection;
            _eventStreamNameStrategy = eventStreamNameStrategy;
            _checkpointStreamNameStrategy = checkpointStreamNameStrategy;
        }
        
        public async Task GetEventsAsync(string actorName, long indexStart, long indexEnd, Action<object> callback)
        {
            var events = await _connection.ReadEvents(_eventStreamNameStrategy(actorName), indexStart,
                indexEnd - indexStart + 1);
            foreach (var @event in events)
            {
                callback(@event);
            }
        }

        public async Task<(object Snapshot, long Index)> GetSnapshotAsync(string actorName)
        {
        }

        public async Task PersistEventAsync(string actorName, long index, object @event)
        {
        }

        public async Task PersistSnapshotAsync(string actorName, long index, object snapshot)
        {
        }

        public async Task DeleteEventsAsync(string actorName, long inclusiveToIndex)
        {
        }

        public async Task DeleteSnapshotsAsync(string actorName, long inclusiveToIndex)
        {
        }
    }
}