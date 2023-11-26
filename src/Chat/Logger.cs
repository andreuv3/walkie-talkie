using WalkieTalkie.Chat.Data;

namespace WalkieTalkie.Chat
{
    public class Logger
    {
        private readonly LogsDao _logsDao;

        public Logger(LogsDao logsDao)
        {
            _logsDao = logsDao;
        }

        public void Log(string log)
        {
            _logsDao.Add(log);
        }

        public ICollection<string> GetLogs()
        {
            return _logsDao.GetLogs();
        }
    }
}
