﻿using System.Text;
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
        private readonly ICollection<User> _users;
        private readonly ICollection<Group> _groups;
        private readonly ICollection<Conversation> _conversations;
        private readonly User _user;
        private string _ownControlTopic;

        public Chat(string host, int port, int qos, int timeout)
        {
            _client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
            _users = new HashSet<User>();
            _groups = new HashSet<Group>();
            _conversations = new HashSet<Conversation>();
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
                if (string.IsNullOrWhiteSpace(payload))
                {
                    return;
                }

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
            Console.WriteLine("HandleOwnControlTopicMessage");
            Console.WriteLine(payload);
            Console.WriteLine("****************************");
            var receivedConversation = JsonSerializer.Deserialize<Conversation>(payload);
            if (receivedConversation == null)
            {
                return;
            }

            var conversation = _conversations
                .Where(c => (c.From == receivedConversation.From && c.To == receivedConversation.To) || 
                            (c.From == receivedConversation.To && c.To == receivedConversation.From))
                .FirstOrDefault();

            if (conversation == null)
            {
                _conversations.Add(receivedConversation);
                if (receivedConversation.Accepted)
                {
                    _client.Subscribe(new string[] { receivedConversation.Topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                }
            }
            else
            {
                conversation.Accept(receivedConversation.Topic);
                _client.Subscribe(new string[] { conversation.Topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
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
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("               USUÁRIOS               ");
            Console.WriteLine("--------------------------------------");
            foreach (var user in _users)
            {
                string status = user.IsOnline ? "online" : "offline";
                string self = user.Username == _user.Username ? " (você mesmo)" : "";
                Console.WriteLine($"{user.Username} está {status}{self}");
            }
            Console.WriteLine("Pressione qualquer tecla para voltar ao menu principal");
            Console.ReadKey();
        }

        public void RequestChat()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("       SOLICITAÇÃO DE CONVERSA        ");
            Console.WriteLine("--------------------------------------");

            string? to = null;
            while (string.IsNullOrWhiteSpace(to))
            {
                Console.Write("Para quem você deseja enviar a mensagem? ");
                to = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(to))
                {
                    Console.WriteLine("Você precisa informar para quem deseja enviar a mensagem");
                }

                if (to == _user.Username)
                {
                    Console.WriteLine("Você não pode enviar uma mensagem para si mesmo");
                    to = null;
                }
            }

            var conversation = new Conversation (_user.Username, to);
            _conversations.Add(conversation);

            string topic = $"{to}_{ControlTopicSuffix}";
            var payload = BuildPayload(conversation);
            _client.Publish(topic, payload, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);

            Console.WriteLine("Solicitação enviada");
            Console.WriteLine("Pressione qualquer tecla para voltar ao menu principal");
            Console.ReadKey();
        }

        public void ManageChatRequests()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("       SOLICITAÇÕES DE CONVERSA       ");
            Console.WriteLine("--------------------------------------");

            var chatRequests = _conversations.Where(c => !c.Accepted && c.From != _user.Username).ToList();
            if (chatRequests.Count == 0)
            {
                Console.WriteLine("Você não possui solicitações de novas conversas");
                Console.WriteLine("Pressione qualquer tecla para voltar ao menu principal");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Você possui as seguintes solicitações de conversa");
            for (int i = 0; i < chatRequests.Count; i++)
            {
                var request = chatRequests.ElementAt(i);
                Console.WriteLine($"{i + 1}. Aceitar conversar com {request.From}");
            }

            Console.WriteLine("0. Voltar para o menu principal");
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

                if (option < 0 || option > chatRequests.Count)
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
            var requestToAccept = chatRequests.ElementAt(requestIndex);
            string controlTopic = $"{requestToAccept.From}_{ControlTopicSuffix}";
            string chatTopic = $"{_user.Username}_{requestToAccept.From}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            
            var conversation = _conversations.First(c => c.From == requestToAccept.From);
            conversation.Accept(chatTopic);

            var payload = BuildPayload(conversation);
            _client.Publish(controlTopic, payload, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
            _client.Subscribe(new string[] { conversation.Topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            
            Console.WriteLine($"Solicitação de {requestToAccept.From} aceita, vocês já podem iniciar uma conversa");
            Console.WriteLine("Pressione qualquer tecla para voltar ao menu principal");
            Console.ReadKey();
        }

        public void SendMessage()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("              CONVERSAS               ");
            Console.WriteLine("--------------------------------------");

            var conversations = _conversations.Where(c => c.Accepted).ToList();
            if (conversations.Count == 0)
            {
                Console.WriteLine("Você ainda não possui conversas");
                Console.WriteLine("Pressione qualquer tecla para voltar ao menu principal");
                Console.ReadKey();
                return;
            }

            foreach (var conversation in conversations)
            {
                Console.WriteLine($"*** Conversa com {conversation.With(_user.Username)} ***");
                var lastMessage = conversation.LastMessage;
                if (lastMessage == null)
                {
                    Console.WriteLine("    Nenhuma mensagem nesta conversa");
                }
                else
                {
                    Console.WriteLine($"    {lastMessage.FormattedSendedAt}");
                    Console.WriteLine($"    {lastMessage.Content}");
                }
            }
        }

        public void ListGroups()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("               USUÁRIOS               ");
            Console.WriteLine("--------------------------------------");
            if (_groups.Count == 0)
            {
                Console.WriteLine("Nenhum grupo encontrado");
                Console.WriteLine("Pressione qualquer tecla para voltar ao menu principal");
                Console.ReadKey();
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
                
                Console.WriteLine($"{group.Name} [líder: {group.Leader.Username}{membership}]");
            }
            Console.WriteLine("Pressione qualquer tecla para voltar ao menu principal");
            Console.ReadKey();
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
