namespace WalkieTalkie.Chat
{
    public static class ChatConstants
    {
        public const string UsersTopic = "USERS/";
        public const string ControlTopicSuffix = "_CONTROL";
        public const string GroupsTopic = "GROUPS/";
        public const string GroupsConversationTopic = "GROUPS_MESSAGES/";
        public const string ConversationTopicPattern = @"(\w+)_(\w+)_(\d+)";
    }
}
