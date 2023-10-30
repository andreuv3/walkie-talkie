using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat.Data
{
    public class GroupsDao
    {
        private const string Filename = "groups.txt";
        private readonly ICollection<Group> _groups;

        public GroupsDao()
        {
            _groups = new HashSet<Group>();
        }

        public void LoadStoredGroups(string username)
        {
            if (File.Exists($"{username}_{Filename}"))
            {
                string[] lines = File.ReadAllLines($"{username}_{Filename}");
                foreach (var line in lines)
                {
                    string[] parts = line.Split('|');
                    string name = parts[0];
                    string leaderName = parts[1];
                    string[] members = parts[2].Split(';');
                    var group = new Group
                    {
                        Name = name,
                        Leader = new User
                        {
                            Username = leaderName,
                            IsOnline = true
                        },
                        Members = members
                            .Select(m => new User
                            {
                                Username = m,
                                IsOnline = true
                            })
                            .ToList()
                    };
                    _groups.Add(group);
                }
            }
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

        public void StoreGroups(string username)
        {
            using var writer = new StreamWriter($"{username}_{Filename}");
            foreach (var group in _groups)
            {
                string members = string.Join(';', group.Members.Select(m => m.Username));
                writer.WriteLine($"{group.Name}|{group.Leader.Username}|{members}");
            }
        }
    }
}
