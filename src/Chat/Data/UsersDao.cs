using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat.Data
{
    public class UsersDao
    {
        private const string Filename = "users.txt";
        private readonly ICollection<User> _users;

        public UsersDao()
        {
            _users = new HashSet<User>();
        }

        public void LoadStoredUsers(string username)
        {
            if (File.Exists($"{username}_{Filename}"))
            {    
                string[] lines = File.ReadAllLines($"{username}_{Filename}");
                foreach (var line in lines)
                {
                    string[] parts = line.Split('|');
                    _users.Add(new User
                    {
                        Username = parts[0],
                        IsOnline = bool.Parse(parts[1])
                    });
                }
            }
        }

        public User? FindUser(string username)
        {
            return _users.FirstOrDefault(u => u.Username == username);
        }

        public void AddUser(User user)
        {
            _users.Add(user);
        }

        public ICollection<User> FindUsers()
        {
            return _users.OrderBy(u => u.IsOnline).ThenBy(u => u.Username).ToList();
        }

        public void StoreUsers(string username)
        {
            using var writer = new StreamWriter($"{username}_{Filename}");
            foreach (var user in _users)
            {
                writer.WriteLine($"{user.Username}|{user.IsOnline}");
            }
        }
    }
}
