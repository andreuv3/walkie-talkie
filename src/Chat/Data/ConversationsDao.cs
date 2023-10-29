using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat.Data
{
    public class ConversationsDao
    {
        private readonly ICollection<Conversation> _conversations;

        public ConversationsDao()
        {
            _conversations = new HashSet<Conversation>();
        }

        public Conversation? FindConversation(string from, string to)
        {
            return _conversations
                .Where(c => (c.From == from && c.To == to) || (c.From == to && c.To == from))
                .FirstOrDefault();
        }

        public Conversation FindConversationFrom(string from)
        {
            return _conversations.First(c => c.From == from);
        }

        public void AddConversation(Conversation conversation)
        {
            _conversations.Add(conversation);
        }

        public ICollection<Conversation> FindNotAcceptedConversations(string username)
        {
            return _conversations.Where(c => !c.Accepted && c.From != username).ToList();
        }

        public ICollection<Conversation> FindAcceptedConversations()
        {
            return _conversations.Where(c => c.Accepted).ToList();
        }

        public void RemoveConversation(Conversation conversation)
        {
            _conversations.Remove(conversation);
        }
    }
}
