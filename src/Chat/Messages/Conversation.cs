namespace WalkieTalkie.Chat.Messages
{
    public class Conversation
    {
        public string From { get; set; }
        public string To { get; set; }
        public bool Accepted { get; set; }
        public string? Topic { get; set; }
        public ICollection<Message> Messages { get; set; }
        public Message? LastMessage => Messages.LastOrDefault();

        public Conversation()
        {
            Messages = new LinkedList<Message>();
        }

        public Conversation(string from, string to)
        {
            From = from;
            To = to;
            Accepted = false;
            Topic = null;
            Messages = new LinkedList<Message>();
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
    }
}
