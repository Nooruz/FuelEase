namespace KIT.GasStation.Web.Services
{
    public interface IGroupRegistry
    {
        void Add(string connectionId, string group);
        void Remove(string connectionId, string group);
        void RemoveAllForConnection(string connectionId);
        IReadOnlyCollection<string> GetGroupsForConnection(string connectionId);
        IReadOnlyCollection<string> GetAllGroups();
    }
}
