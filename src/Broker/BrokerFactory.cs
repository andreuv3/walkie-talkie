using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace WalkieTalkie.Broker
{
    public static class BrokerFactory
    {
        private static IMqttClient? _client;

        public static async Task<IMqttClient> BuildFromConfiguratio(string host, int port, int qos, int timeout)
        {
            if (_client == null)
            {
                var mqttFactory = new MqttFactory();
                var mqttOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer($"{host}:{port}")
                    .WithTimeout(TimeSpan.FromSeconds(timeout))
                    .WithWillQualityOfServiceLevel((MqttQualityOfServiceLevel) qos)
                    .Build();
                
                _client = mqttFactory.CreateMqttClient();
                await _client.ConnectAsync(mqttOptions);
            }

            return _client;
        }
    }
}
