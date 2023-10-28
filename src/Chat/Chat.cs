using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat
{
    public class Chat
    {
        private const string UsersTopic = "USERS";
        private const string ControlTopicSuffix = "CONTROL";
        private const string GroupsTopic = "GROUPS";

        private readonly MqttClient _client;
        private readonly ICollection<ChatRequest> _chatRequests;
        private readonly ICollection<User> _users;
        private readonly ICollection<Group> _groups;
        private readonly User _user;
        private string _ownControlTopic;

        public Chat(string host, int port, int qos, int timeout)
        {
            _client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
            _chatRequests = new HashSet<ChatRequest>();
            _users = new HashSet<User>();
            _groups = new HashSet<Group>();
            _user = new User();
            _ownControlTopic = string.Empty;
        }

        public void SetUsername(string username)
        {
            _user.Username = username;
            _ownControlTopic = $"{_user.Username}_{ControlTopicSuffix}";
        }

        public void Connect()
        {
            _client.Connect(_user.Username, "", "", false, 30);
            _client.MqttMsgPublishReceived += (sender, eventArgs) =>
            {
                var payload = UTF8Encoding.UTF8.GetString(eventArgs.Message);
                if (eventArgs.Topic == _ownControlTopic)
                {
                    HandleOwnControlTopicMessage(payload);
                }

                if (eventArgs.Topic == UsersTopic)
                {
                    HandleUsersMessage(payload);
                }

                if (eventArgs.Topic == GroupsTopic)
                {
                    HandleGroupsMessage(payload);
                }
            };
        }

        private void HandleOwnControlTopicMessage(string payload)
        {
            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(payload);

            if (chatRequest == null)
            {
                return;
            }

            if (!_chatRequests.Any(req => req.From == chatRequest.From))
            {
                _chatRequests.Add(chatRequest);
            }
        }

        private void HandleUsersMessage(string payload)
        {
            var receivedUser = JsonSerializer.Deserialize<User>(payload);

            if (receivedUser == null)
            {
                return;
            }

            var user = _users.FirstOrDefault(u => u.Username == receivedUser.Username);
            if (user == null)
            {
                _users.Add(receivedUser);
            }
            else
            {
                user.IsOnline = receivedUser.IsOnline;
            }
        }

        private void HandleGroupsMessage(string payload)
        {
            var receivedGroup = JsonSerializer.Deserialize<Group>(payload);

            if (receivedGroup == null)
            {
                return;
            }

            var group = _groups.FirstOrDefault(g => g.Name == receivedGroup.Name);
            if (group == null)
            {
                _groups.Add(receivedGroup);
            }
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public void GoOnline()
        {
            _user.GoOnline();
            var payload = BuildPayload(_user);
            _client.Publish(UsersTopic, payload, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        }

        public void GoOffline()
        {
            _user.GoOffline();
            var payload = BuildPayload(_user);
            _client.Publish(UsersTopic, payload, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        }

        public void SubscribeToOwnTopic()
        {
            _client.Subscribe(new string[] { _ownControlTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        public void SubscribteToUsersTopic()
        {
            _client.Subscribe(new string[] { UsersTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        public void SubscribeToGroupsTopic()
        {
            _client.Subscribe(new string[] { GroupsTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        } 

        public void ListUsers()
        {
            Console.WriteLine("Usuários");
            foreach (var user in _users)
            {
                string status = user.IsOnline ? "online" : "offline";
                string self = user.Username == _user.Username ? " (você mesmo)" : "";
                Console.WriteLine($"{user.Username} está {status}{self}");
            }
        }

        public void RequestChat()
        {
            string? recipient = null;
            while (string.IsNullOrWhiteSpace(recipient))
            {
                Console.Write("Para quem você deseja enviar a mensagem? ");
                recipient = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(recipient))
                {
                    Console.WriteLine("Você precisa informar para quem deseja enviar a mensagem");
                }
            }

            string topic = $"{recipient}_{ControlTopicSuffix}";
            var message = new ChatRequest { From = _user.Username };
            var payload = BuildPayload(message);
            _client.Publish(topic, payload, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);

            Console.WriteLine("Solicitação enviada");
        }

        public void ManageChatRequests()
        {
            if (_chatRequests.Count == 0)
            {
                Console.WriteLine("Você não possui solicitações de novas conversas");
                return;
            }

            Console.WriteLine("Você possui as seguintes solicitações de conversa");
            for (int i = 0; i < _chatRequests.Count; i++)
            {
                var request = _chatRequests.ElementAt(i);
                Console.WriteLine($"{i + 1} - {request.From} deseja iniciar uma conversa");
            }

            Console.WriteLine("0 - Voltar para o menu principal");
            Console.WriteLine("Digite o número da solicitação que deseja aceitar ou zero para voltar ao menu principal");
            int option = -1;
            bool validOption = false;
            while (!validOption)
            {
                Console.Write(": ");
                string? optionAsText = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(optionAsText))
                {
                    Console.WriteLine("Você precisa selecionar uma opção.");
                    continue;
                }

                if (!int.TryParse(optionAsText, out option))
                {
                    Console.WriteLine("Opção inválida");
                    continue;
                }

                if (option < 0 || option > _chatRequests.Count)
                {
                    Console.WriteLine("Opção inválida");
                    continue;
                }
                
                validOption = true;
            }

            if (option == 0)
            {
                return;
            }

            int requestIndex = option - 1;
            var selectedRequest = _chatRequests.ElementAt(requestIndex);
            Console.WriteLine($"Aceitando solicitação de {selectedRequest.From}");
        }

        public void ListGroups()
        {
            if (_groups.Count == 0)
            {
                Console.WriteLine("Nenhum grupo encontrado");
                return;
            }

            foreach (var group in _groups)
            {
                string membership = "";
                if (group.IsMember(_user))
                {
                    membership = " (você faz parte deste grupo)";
                }
                else if (group.IsLeader(_user))
                {
                    membership = " (você é líder deste grupo)";
                }
                
                Console.WriteLine($"{group.Name} - Líder: {group.Leader.Username}{membership}");
            }
        }

        public void CreateGroup()
        {
            string? groupName = null;
            while (string.IsNullOrWhiteSpace(groupName))
            {
                Console.Write("Qual é o nome do grupo? ");
                groupName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    Console.WriteLine("Você precisa informar o nome do grupo");
                }

                if (_groups.Any(g => g.Name == groupName))
                {
                    Console.WriteLine("Já existe um grupo com este nome, tente novamente");
                    groupName = null;
                }
            }

            var message = new Group { Name = groupName, Leader = _user, Members = new HashSet<User>() };
            var payload = BuildPayload(message);
            _client.Publish(GroupsTopic, payload, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);

            Console.WriteLine("Grupo criado");
        }

        private byte[] BuildPayload(object message)
        {
            string messageAsJson = JsonSerializer.Serialize(message);
            return Encoding.UTF8.GetBytes(messageAsJson);
        }
    }
}
