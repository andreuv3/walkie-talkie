using System.Text;
using System.Text.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using static uPLibrary.Networking.M2Mqtt.MqttClient;

namespace WalkieTalkie.EventBus
{
    public class Bus
    {
        private readonly MqttClient _client;

        public Bus(string host, int port)
        {
            _client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
        }

        public void Connect(string clientId, bool cleanSession = false, ushort keepAlivePeriod = 30, string username = "", string password = "")
        {
            _client.Connect(clientId, username, password, cleanSession, keepAlivePeriod);
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public void Receive(Action<string, string?> callback)
        {
            _client.MqttMsgPublishReceived += (sender, eventArgs) =>
            {
                string topic = eventArgs.Topic;
                string? payload = UTF8Encoding.UTF8.GetString(eventArgs.Message);
                callback(topic, payload);
            };
        }

        public void Publish(string topic, object message, bool retain = false, byte qos = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE)
        {
            var payload = BuildPayload(message);
            _client.Publish(topic, payload, qos, retain);
        }

        private byte[] BuildPayload(object message)
        {
            string messageAsJson = JsonSerializer.Serialize(message);
            return Encoding.UTF8.GetBytes(messageAsJson);
        }

        public void Subscribe(string topic, byte qos = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE)
        {
            _client.Subscribe(new string[] { topic }, new byte[] { qos });
        }
    }
}
