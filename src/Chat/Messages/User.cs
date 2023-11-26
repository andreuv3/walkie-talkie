namespace WalkieTalkie.Chat.Messages
{
    public class User
    {
        public string Username { get; set; }
        public bool IsOnline { get; set; }
        public string Topic { get; set; }

        public User()
        {
        }

        public User(string username)
        {
            Username = username;
        }

        public void HasUsernameAndTopic(string username, string topic)
        {
            Username = username;
            Topic = topic;
        }

        public void GoOnline()
        {
            IsOnline = true;
        }

        public void GoOffline()
        {
            IsOnline = false;
        }

        public static User Nobody()
        {
            return new User
            {
                Username = string.Empty,
                Topic = string.Empty,
                IsOnline = false,
            };
        }
    }
}
