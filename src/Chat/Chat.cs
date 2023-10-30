using System.Text;
using System.Text.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using WalkieTalkie.Chat.Data;
using WalkieTalkie.Chat.Messages;

namespace WalkieTalkie.Chat
{
    public class Chat
    {
        private const string UsersTopic = "USERS";
        private const string ControlTopicSuffix = "_CONTROL";
        private const string GroupsTopic = "GROUPS";

        private readonly MqttClient _client;
        private readonly ConversationsDao _conversationsDao;
        private readonly UsersDao _usersDao;
        private readonly GroupsDao _groupsDao;
        private readonly bool _debug;
        private readonly User _user;
        private string _ownControlTopic;
        private readonly ICollection<string> _logs;

        public Chat(string host, int port, bool debug)
        {
            _client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
            _conversationsDao = new ConversationsDao();
            _usersDao = new UsersDao();
            _groupsDao = new GroupsDao();
            _debug = debug;
            _user = new User();
            _ownControlTopic = string.Empty;
            _logs = new List<string>();
        }

        public void ConnectAs(string username)
        {
            _user.Username = username;
            _usersDao.LoadStoredUsers(_user.Username);
            _groupsDao.LoadStoredGroups(_user.Username);
            _ownControlTopic = $"{_user.Username}{ControlTopicSuffix}";
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
            var receivedConversation = JsonSerializer.Deserialize<Conversation>(payload);
            if (receivedConversation == null)
            {
                return;
            }

            var conversation = _conversationsDao.FindConversation(receivedConversation.From, receivedConversation.To);
            if (conversation == null)
            {
                conversation = receivedConversation;
                _conversationsDao.AddConversation(conversation);
                if (_debug)
                {
                    _logs.Add($"Solicitação de conversa recebida de {conversation.From} no tóico {_user.Username}{ControlTopicSuffix}");
                }
            }
            else if (receivedConversation.Accepted)
            {
                conversation.Accept(receivedConversation.Topic);
                Subscribe(conversation.Topic);
                _logs.Add($"Solicitação de conversa aceita por {conversation.To} através do tópico {conversation.Topic}");
            }
            else
            {
                _conversationsDao.RemoveConversation(conversation);
                _logs.Add($"Solicitação de conversa recusada por {conversation.To}");
            }
        }

        private void HandleUsersMessage(string payload)
        {
            var receivedUser = JsonSerializer.Deserialize<User>(payload);

            if (receivedUser == null)
            {
                return;
            }

            var user = _usersDao.FindUser(receivedUser.Username);
            if (user == null)
            {
                _usersDao.AddUser(receivedUser);
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

            var group = _groupsDao.FindGroup(receivedGroup.Name);
            if (group == null)
            {
                _groupsDao.AddGroup(receivedGroup);
            }
        }

        public void SubscribeToBaseTopics()
        {
            Subscribe(_ownControlTopic);
            Subscribe(UsersTopic);
            Subscribe(GroupsTopic);
        }

        public void Disconnect()
        {
            _usersDao.StoreUsers(_user.Username);
            _groupsDao.StoreGroups(_user.Username);
            _client.Disconnect();
        }

        public void GoOnline()
        {
            _user.GoOnline();
            Publish(UsersTopic, _user);
        }

        public void GoOffline()
        {
            _user.GoOffline();
            Publish(UsersTopic, _user);
        }

        public void ListUsers()
        {
            var users = _usersDao.FindUsers();
            foreach (var user in users)
            {
                string status = user.IsOnline ? "online" : "offline";
                string self = user.Username == _user.Username ? " (você mesmo)" : "";
                Console.WriteLine($"{user.Username} está {status}{self}");
            }   
        }

        public void RequestChat()
        {
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
            _conversationsDao.AddConversation(conversation);

            string topic = $"{to}{ControlTopicSuffix}";
            Publish(topic, conversation);

            if (_debug)
            {
                _logs.Add($"{_user.Username} enviou uma solicitação para {to} através do tópico {topic}");
            }

            Console.WriteLine("Solicitação enviada");
        }

        public void ManageChatRequests()
        {
            var notAcceptedConversations = _conversationsDao.FindNotAcceptedConversations(_user.Username);
            if (notAcceptedConversations.Count == 0)
            {
                Console.WriteLine("Você não possui solicitações de novas conversas");
                return;
            }

            Console.WriteLine("Você possui as seguintes solicitações de conversa");
            for (int i = 0; i < notAcceptedConversations.Count; i++)
            {
                var request = notAcceptedConversations.ElementAt(i);
                Console.WriteLine($"{i + 1}. Gerenciar conversa com {request.From}");
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

                if (option < 0 || option > notAcceptedConversations.Count)
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
            var requestToAccept = notAcceptedConversations.ElementAt(requestIndex);

            Console.WriteLine($"Você deseja aceitar a conversa com {requestToAccept.From}?");
            Console.WriteLine("1. Sim");
            Console.WriteLine("2. Não");
            option = -1;
            validOption = false;
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

                if (option < 1 || option > 2)
                {
                    Console.WriteLine("Opção inválida");
                    continue;
                }
                
                validOption = true;
            }

            string controlTopic = $"{requestToAccept.From}{ControlTopicSuffix}";
            var conversation = _conversationsDao.FindConversationFrom(requestToAccept.From);
            if (option == 1)
            {
                string chatTopic = $"{_user.Username}_{requestToAccept.From}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                
                conversation.Accept(chatTopic);

                Publish(controlTopic, conversation);
                Subscribe(conversation.Topic!);

                if (_debug)
                {
                    _logs.Add($"{_user.Username} aceitou conversar com {requestToAccept.From} através do tópico {chatTopic}");
                }

                Console.WriteLine($"Solicitação de {requestToAccept.From} aceita, vocês já podem iniciar uma conversa");
            }
            else
            {
                _conversationsDao.RemoveConversation(conversation);
                Publish(controlTopic, conversation);
                if (_debug)
                {
                    _logs.Add($"{_user.Username} recusou conversar com {requestToAccept.From}");
                }
            }
        }

        public void SendMessage()
        {
            var conversations = _conversationsDao.FindAcceptedConversations();
            if (conversations.Count == 0)
            {
                Console.WriteLine("Você ainda não possui conversas");
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
            var groups = _groupsDao.FindGroups();
            if (groups.Count == 0)
            {
                Console.WriteLine("Nenhum grupo encontrado");
                return;
            }

            foreach (var group in groups)
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

                Console.WriteLine($"Grupo: {group.Name}{membership}");
                Console.WriteLine("Membros:");
                Console.WriteLine($" - {group.Leader.Username} (líder)");
                
                foreach (var member in group.Members.OrderBy(m => m.Username))
                {
                    Console.WriteLine($"{member.Username}");
                }

                Console.WriteLine("--------------------------------------");
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
                    continue;
                }

                bool groupAlreadyExists = _groupsDao.GroupAlreadyExists(groupName);
                if (groupAlreadyExists)
                {
                    Console.WriteLine("Já existe um grupo com este nome, tente novamente");
                    groupName = null;
                }
            }

            var group = new Group { Name = groupName, Leader = _user, Members = new HashSet<User>() };
            Publish(GroupsTopic, group);

            if (_debug)
            {
                _logs.Add($"Grupo {group.Name} criado pelo usuário {group.Leader.Username} que agora é seu líder");
            }
        }

        public void ShowLogs()
        {
            if (_logs.Count == 0)
            {
                Console.WriteLine("Nenhuma log encontrado");
                return;
            }

            foreach (string log in _logs)
            {
                Console.WriteLine(log);
            }
        }

        private void Publish(string topic, object message)
        {
            var payload = BuildPayload(message);
            _client.Publish(topic, payload, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }

        private byte[] BuildPayload(object message)
        {
            string messageAsJson = JsonSerializer.Serialize(message);
            return Encoding.UTF8.GetBytes(messageAsJson);
        }

        private void Subscribe(string topic)
        {
            _client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }
    }
}
