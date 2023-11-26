namespace WalkieTalkie.Chat.Messages
{
    public class Conversation
    {
        public string From { get; set; }
        public string To { get; set; }
        public bool Accepted { get; set; }
        public string? Topic { get; set; }
        public ICollection<Message> UnreadMessages { get; set; }
        public DateTimeOffset LastMessageAt { get; set; }

        public Conversation()
        {
            UnreadMessages = new LinkedList<Message>();
            LastMessageAt = DateTimeOffset.MinValue;
        }

        public Conversation(string from, string to)
        {
            From = from;
            To = to;
            Accepted = false;
            Topic = null;
            UnreadMessages = new LinkedList<Message>();
            LastMessageAt = DateTimeOffset.MinValue;
        }

        public void Accept(string topic)
        {
            Accepted = true;
            Topic = topic;
        }

        public string With(string userame)
        {
            return From == userame ? To : From;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(To);
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
