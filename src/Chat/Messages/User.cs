namespace WalkieTalkie.Chat.Messages
{
    public class User
    {
        public string Username { get; set; }
        public bool IsOnline { get; set; }

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
