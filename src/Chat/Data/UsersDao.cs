using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat.Data
{
    public class UsersDao
    {
        private readonly ICollection<User> _users;

        public UsersDao()
        {
            _users = new HashSet<User>();
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
    }
}
