using System.Collections.Concurrent;

namespace KIT.GasStation.Web.Services
{
    public class GroupRegistry : IGroupRegistry
    {
        // group -> connectionIds
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _g2c = new();
        // connectionId -> groups
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _c2g = new();

        public void Add(string connectionId, string group)
        {
            var conns = _g2c.GetOrAdd(group, _ => new());
            conns.TryAdd(connectionId, 0);

            var groups = _c2g.GetOrAdd(connectionId, _ => new());
            groups.TryAdd(group, 0);
        }

        public void Remove(string connectionId, string group)
        {
            if (_g2c.TryGetValue(group, out var conns))
            {
                conns.TryRemove(connectionId, out _);
                if (conns.IsEmpty) _g2c.TryRemove(group, out _);
            }

            if (_c2g.TryGetValue(connectionId, out var groups))
            {
                groups.TryRemove(group, out _);
                if (groups.IsEmpty) _c2g.TryRemove(connectionId, out _);
            }
        }

        public void RemoveAllForConnection(string connectionId)
        {
            if (_c2g.TryRemove(connectionId, out var groups))
            {
                foreach (var g in groups.Keys)
                {
                    if (_g2c.TryGetValue(g, out var conns))
                    {
                        conns.TryRemove(connectionId, out _);
                        if (conns.IsEmpty) _g2c.TryRemove(g, out _);
                    }
                }
            }
        }

        public IReadOnlyCollection<string> GetGroupsForConnection(string connectionId) =>
            _c2g.TryGetValue(connectionId, out var groups) ? groups.Keys.ToArray() : Array.Empty<string>();

        public IReadOnlyCollection<string> GetAllGroups() => _g2c.Keys.ToArray();
    }
}
