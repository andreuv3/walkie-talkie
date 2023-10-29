using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat.Data
{
    public class GroupsDao
    {
        private readonly ICollection<Group> _groups;

        public GroupsDao()
        {
            _groups = new HashSet<Group>();
        }

        public Group? FindGroup(string name)
        {
            return _groups.FirstOrDefault(g => g.Name == name);
        }

        public void AddGroup(Group group)
        {
            _groups.Add(group);
        }

        public ICollection<Group> FindGroups()
        {
            return _groups.OrderBy(g => g.Name).ToList();
        }

        public bool GroupAlreadyExists(string name)
        {
            return _groups.Any(g => g.Name == name);
        }
    }
}
