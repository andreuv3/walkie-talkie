using System.Text;
using System.Text.Json;
using uPLibrary.Networking.M2Mqtt;
using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat
{
    public class Chat
    {
        private const string UsersTopic = "USERS";
        private const string ControlTopicSuffix = "CONTROL";

        private readonly MqttClient _client;
        private string _username;

        public Chat(string host, int port, int qos, int timeout)
        {
            _client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
            _username = string.Empty;
        }

        public void SetUsername(string username)
        {
            _username = username;
        }

        public void Connect()
        {
            _client.Connect(_username);
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public void GoOnline()
        {
            var message = new UserStatus { Username = _username, IsOnline = true };
            var payload = BuildPayload(message);
            _client.Publish(UsersTopic, payload);
        }

        public void GoOffline()
        {
            var message = new UserStatus { Username = _username, IsOnline = false };
            var payload = BuildPayload(message);
            _client.Publish(UsersTopic, payload);
        }

        public void RequestChat()
        {
            string? recipient = null;
            while (string.IsNullOrWhiteSpace(recipient))
            {
                Console.Write("Para quem você deseja enviar a mensagem?");
                recipient = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(recipient))
                {
                    Console.WriteLine("Você precisa informar para quem deseja enviar a mensagem");
                }
            }

            string topic = $"{recipient}_{ControlTopicSuffix}";
            var message = new ChatRequest { Username = _username };
            var payload = BuildPayload(message);
            _client.Publish(topic, payload);
        }

        private byte[] BuildPayload(object message)
        {
            string messageAsJson = JsonSerializer.Serialize(message);
            return Encoding.UTF8.GetBytes(messageAsJson);
        }
    }
}
