using System.Collections.Concurrent;

namespace LearnHub.Services
{
    public interface IPresenceTracker
    {
        bool AddConnection(long userId, string connectionId);
        bool RemoveConnection(long userId, string connectionId);
        bool IsOnline(long userId);
    }

    public class PresenceTracker : IPresenceTracker
    {
        private readonly ConcurrentDictionary<long, HashSet<string>> _connections = new();
        private readonly object _lock = new();

        public bool AddConnection(long userId, string connectionId)
        {
            lock (_lock)
            {
                var isNew = !_connections.ContainsKey(userId);
                var set = _connections.GetOrAdd(userId, _ => new HashSet<string>());
                set.Add(connectionId);
                return isNew;
            }
        }

        public bool RemoveConnection(long userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.TryGetValue(userId, out var set))
                    return false;

                set.Remove(connectionId);

                if (set.Count == 0)
                {
                    _connections.TryRemove(userId, out _);
                    return true;
                }

                return false;
            }
        }

        public bool IsOnline(long userId)
        {
            return _connections.TryGetValue(userId, out var set) && set.Count > 0;
        }
    }
}
