using System.Collections.Concurrent;
using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat.Data
{
    public class UsersDao
    {
        private readonly ConcurrentDictionary<string, User> _users;

        public UsersDao()
        {
            _users = new ConcurrentDictionary<string, User>();
        }

        public void SaveUser(User user)
        {
            if (!_users.ContainsKey(user.Username))
            {
                _users.TryAdd(user.Username, user);
            }
            else
            {
                _users[user.Username] = user;
            }
        }

        public ICollection<User> FindUsers()
        {
            return _users.Select(u => u.Value).OrderBy(u => u.IsOnline).ThenBy(u => u.Username).ToList();
        }
    }
}
