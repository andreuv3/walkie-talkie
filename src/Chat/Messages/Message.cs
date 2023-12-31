﻿namespace WalkieTalkie.Chat.Messages
{
    public class Message
    {
        public string From { get; set; }
        public string Content { get; set; }
        public DateTimeOffset SendedAt { get; set; }
        public string FormattedSendedAt => $"em {SendedAt.ToString("dd/MM/yyyy")} às {SendedAt.ToString("HH:mm:ss")}";

        public Message()
        {
        }

        public Message(string from, string content)
        {
            From = from;
            Content = content;
            SendedAt = DateTimeOffset.Now;   
        }
    }
}
