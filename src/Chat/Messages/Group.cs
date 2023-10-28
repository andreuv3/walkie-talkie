namespace WalkieTalkie.Chat.Messages
{
    public class Group
    {
        public string Name { get; set; }
        public User Leader { get; set; }
        public ICollection<User> Members { get; set; }

        public Group()
        {
            Members = new HashSet<User>();
        }

        public bool IsLeader(User user)
        {
            return Leader.Username == user.Username;
        }

        public bool IsMember(User user)
        {
            return Members.Any(m => m.Username == user.Username);
        }
    }
}
