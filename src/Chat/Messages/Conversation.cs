namespace WalkieTalkie.Chat.Messages
{
    public class Conversation
    {
        public string From { get; set; }
        public string To { get; set; }
        public bool Accepted { get; set; }
        public string? Topic { get; set; }
        public ICollection<Message> UnreadMessages { get; set; }
        public Message? LastMessage => UnreadMessages.LastOrDefault();

        public Conversation()
        {
            UnreadMessages = new LinkedList<Message>();
        }

        public Conversation(string from, string to)
        {
            From = from;
            To = to;
            Accepted = false;
            Topic = null;
            UnreadMessages = new LinkedList<Message>();
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
    }
}
