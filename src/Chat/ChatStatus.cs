namespace WalkieTalkie.Chat
{
    public class ChatStatus
    {
        private bool _chatting;
        private string _chattingWith;

        public ChatStatus()
        {
            _chatting = false;
            _chattingWith = string.Empty;
        }

        public void StartChatting(string with)
        {
            _chatting = true;
            _chattingWith = with;
        }

        public void StopChatting()
        {
            _chatting = false;
            _chattingWith = string.Empty;
        }

        public bool IsChatting()
        {
            return _chatting;
        }

        public string IsChattingWith()
        {
            return new string(_chattingWith);
        }
    }
}
