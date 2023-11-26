
namespace WalkieTalkie.Chat.Messages
{
    public class Group
    {
        public string Name { get; set; }
        public User Leader { get; set; }
        public ICollection<User> Members { get; set; }
        public ICollection<GroupRequest> Requests { get; set; }
        public ICollection<Message> Messages { get; set; }
        public Message? LastMessage => Messages.LastOrDefault();
        public ICollection<Message> UnreadMessages { get; set; }
        public DateTimeOffset LastMessageAt { get; set; }

        public Group()
        {
            Members = new HashSet<User>();
            Requests = new HashSet<GroupRequest>();
            Messages = new LinkedList<Message>();
            UnreadMessages = new LinkedList<Message>();
            LastMessageAt = DateTimeOffset.MinValue;
        }

        public bool ContainsLeader(User user)
        {
            return Leader.Username == user.Username;
        }

        public bool ContainsMember(User user)
        {
            return Members.Any(m => m.Username == user.Username);
        }

        public void AddMember(User user)
        {
            if (!ContainsMember(user))
            {
                Members.Add(user);
            }
        }

        public void AddRequest(GroupRequest request)
        {
            if (!ContainsRequest(request.GroupName, request.Username))
            {
                Requests.Add(request);
            }
        }

        public void RemoveRequest(GroupRequest request)
        {
            if (ContainsRequest(request.GroupName, request.Username))
            {
                var existingRequest = Requests.First(r => r.GroupName == request.GroupName && r.Username == request.Username);
                Requests.Remove(existingRequest);
            }
        }

        private bool ContainsRequest(string groupName, string username)
        {
            return Requests.Any(r => r.GroupName == groupName && r.Username == username);
        }

        public bool ContainsPendingRequests()
        {
            return Requests.Any(r => !r.Accepted);
        }

        public void AddUnredMessage(Message message)
        {
            UnreadMessages.Add(message);
        }

        public bool HasUnredMessages()
        {
            return UnreadMessages.Count != 0;
        }

        public void ClearUnreadMessages()
        {
            UnreadMessages.Clear();
        }

        public void UpdateLastMessageAt()
        {
            LastMessageAt = DateTimeOffset.UtcNow;
        }
    }
}
