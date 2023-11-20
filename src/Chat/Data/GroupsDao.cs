using System.Collections.Concurrent;
using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat.Data
{
    public class GroupsDao
    {
        private readonly ConcurrentDictionary<string, Group> _groups;

        public GroupsDao()
        {
            _groups = new ConcurrentDictionary<string, Group>();
        }

        public void SaveGroup(Group group)
        {
            if (!_groups.ContainsKey(group.Name))
            {
                _groups.TryAdd(group.Name, group);
            }
            else
            {
                _groups[group.Name] = group;
            }
        }

        public ICollection<Group> FindGroups()
        {
            return _groups.Select(g => g.Value).OrderBy(g => g.Name).ToList();
        }

        public bool GroupAlreadyExists(string name)
        {
            return _groups.ContainsKey(name);
        }

        public Group FindGroupByName(string groupName)
        {
            return _groups[groupName];
        }

        public ICollection<Group> FindGroupsUserIsPartOf(User user)
        {
            return _groups
                .Where(g => g.Value.ContainsLeader(user) || g.Value.ContainsMember(user))
                .Select(g => g.Value)
                .OrderBy(g => g.Name)
                .ToList();
        }
    }
}
