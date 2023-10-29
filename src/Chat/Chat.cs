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
        private const string ControlTopicSuffix = "CONTROL";
        private const string GroupsTopic = "GROUPS";

        private readonly MqttClient _client;
        private readonly ConversationsDao _conversationsDao;
        private readonly UsersDao _usersDao;
        private readonly GroupsDao _groupsDao;
        private readonly User _user;
        private string _ownControlTopic;

        public Chat(string host, int port)
        {
            _client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
            _conversationsDao = new ConversationsDao();
            _usersDao = new UsersDao();
            _groupsDao = new GroupsDao();
            _user = new User();
            _ownControlTopic = string.Empty;
        }

        public void ConnectAs(string username)
        {
            _user.Username = username;
            _ownControlTopic = $"{_user.Username}_{ControlTopicSuffix}";
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
            }
            else
            {
                conversation.Accept(receivedConversation.Topic);
            }

            if (conversation.Accepted)
            {
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

            string topic = $"{to}_{ControlTopicSuffix}";
            Publish(topic, conversation);
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
            string controlTopic = $"{requestToAccept.From}_{ControlTopicSuffix}";
            string chatTopic = $"{_user.Username}_{requestToAccept.From}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            
            var conversation = _conversationsDao.FindConversationFrom(requestToAccept.From);
            conversation.Accept(chatTopic);

            Publish(controlTopic, conversation);
            Subscribe(conversation.Topic!);

            Console.WriteLine($"Solicitação de {requestToAccept.From} aceita, vocês já podem iniciar uma conversa");
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
                
                Console.WriteLine($"{group.Name} [líder: {group.Leader.Username}{membership}]");
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
        }

        private void Publish(string topic, object message)
        {
            var payload = BuildPayload(message);
            _client.Publish(topic, payload, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
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
