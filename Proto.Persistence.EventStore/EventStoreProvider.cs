using System;
using System.Threading.Tasks;

namespace Proto.Persistence.EventStore
{
    public class EventStoreProvider : IProvider
    {
        public EventStoreProvider(IEventStoreConnection connection)
        {
            
        }
        
        public async Task GetEventsAsync(string actorName, long indexStart, long indexEnd, Action<object> callback)
        {
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