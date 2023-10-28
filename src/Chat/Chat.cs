using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MQTTnet.Client;
using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat
{
    public class Chat
    {
        private const string UsersTopic = "USERS";
        private const string ControlTopicSuffix = "CONTROL";

        private readonly IMqttClient _client;
        private string _username;

        public Chat(IMqttClient client)
        {
            _client = client;
        }

        public void SetUsername(string username)
        {
            _username = username;
        }

        public async Task GoOnline()
        {
            var message = new UserStatus { Username = _username, IsOnline = true };
            var payload = BuildPayload(message);
            await _client.PublishBinaryAsync(UsersTopic, payload);
        }

        public async Task GoOffline()
        {
            var message = new UserStatus { Username = _username, IsOnline = false };
            var payload = BuildPayload(message);
            await _client.PublishBinaryAsync(UsersTopic, payload);
        }

        public async Task RequestChat()
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
            await _client.PublishBinaryAsync(topic, payload);
        }

        private byte[] BuildPayload(object message)
        {
            string messageAsJson = JsonSerializer.Serialize(message);
            return Encoding.UTF8.GetBytes(messageAsJson);
        }
    }
}
