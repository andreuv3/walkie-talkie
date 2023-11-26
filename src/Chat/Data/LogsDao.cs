namespace WalkieTalkie.Chat.Data
{
    public class LogsDao
    {
        private readonly ICollection<string> _logs;

        public LogsDao()
        {
            _logs = new HashSet<string>();
        }

        public void Add(string log)
        {
            _logs.Add(log);
        }
    }
}
