namespace WalkieTalkie.Chat.Messages
{
    public class GroupRequest
    {
        public string GroupName { get; set; }
        public string Username { get; set; }
        public bool Accepted { get; set; }

        public GroupRequest()
        {
        }

        public GroupRequest(string groupName, string username)
        {
            GroupName = groupName;
            Username = username;
            Accepted = false;
        }

        public void Accept()
        {
            Accepted = true;
        }

        public void Reject()
        {
            Accepted = false;
        }
    }
}
