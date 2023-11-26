namespace WalkieTalkie.Chat
{
    public class ChatStatus
    {
        private bool Chatting;
        private string ChattingWith;

        public ChatStatus()
        {
            Chatting = false;
            ChattingWith = string.Empty;
        }

        public void StartChatting(string with)
        {
            Chatting = true;
            ChattingWith = with;
        }

        public void StopChatting()
        {
            Chatting = false;
            ChattingWith = string.Empty;
        }
    }
}
