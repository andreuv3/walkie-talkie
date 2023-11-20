namespace WalkieTalkie.Chat.Messages
{
    public class User
    {
        public string Username { get; set; }
        public bool IsOnline { get; set; }

        public User()
        {
        }

        public User(string username)
        {
            Username = username;
        }

        public void GoOnline()
        {
            IsOnline = true;
        }

        public void GoOffline()
        {
            IsOnline = false;
        }
    }
}
