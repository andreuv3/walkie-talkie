using System.Text.Json;
using WalkieTalkie.Chat.Data;
using WalkieTalkie.Chat.Messages;
using WalkieTalkie.EventBus;

namespace WalkieTalkie.Chat
{
    public class Chat
    {
        private const string UsersTopic = "USERS/";
        private const string ControlTopicSuffix = "_CONTROL";
        private const string GroupsTopic = "GROUPS/";
        private const string GroupsConversationTopic = "GROUPS_MESSAGES/";
        private const string ConversationTopicPattern = @"(\w+)_(\w+)_(\d+)";

        private readonly Bus _bus;
        private readonly ConversationsDao _conversationsDao;
        private readonly UsersDao _usersDao;
        private readonly GroupsDao _groupsDao;
        private readonly LogsDao _logsDao;
        private readonly bool _debug;
        private readonly ICollection<string> _logs;
        private readonly ChatStatus _status;
        private User _user;

        public Chat(Bus bus, ConversationsDao conversationsDao, UsersDao usersDao, GroupsDao groupsDao, LogsDao logsDao, bool debug)
        {
            _bus = bus;
            _conversationsDao = conversationsDao;
            _usersDao = usersDao;
            _groupsDao = groupsDao;
            _logsDao = logsDao;
            _debug = debug;
            _logs = new List<string>();
            _status = new ChatStatus();
        }

        public void ConnectAs(string username)
        {
            _user = new User(username, $"{_user.Username}{ControlTopicSuffix}");
            _bus.Connect(_user.Username);
            _bus.Receive(ReceiveMessage);
            _bus.Subscribe(_user.Topic);
            _bus.Subscribe($"{UsersTopic}+");
            _bus.Subscribe($"{GroupsTopic}+");
            _bus.Subscribe($"{GroupsConversationTopic}+");
        }

        private void ReceiveMessage(string topic, string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            var match = System.Text.RegularExpressions.Regex.Match(topic, ConversationTopicPattern);
            if (match.Success)
            {
                HandleConversationMessage(topic, payload);
                return;
            }

            if (topic == _user.Topic)
            {
                HandleOwnControlTopicMessage(payload);
                return;
            }

            if (topic.StartsWith(UsersTopic))
            {
                HandleUsersMessage(payload);
                return;
            }

            if (topic.StartsWith(GroupsTopic))
            {
                HandleGroupsMessage(payload);
                return;
            }

            if (topic.StartsWith(GroupsConversationTopic))
            {
                HandleGroupsConversationMessage(topic, payload);
                return;
            }
        }

        private void HandleOwnControlTopicMessage(string payload)
        {
            var receivedConversation = JsonSerializer.Deserialize<Conversation>(payload);
            if (receivedConversation != null && receivedConversation.IsValid())
            {
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
                    conversation.Accept(receivedConversation.Topic!);
                    _bus.Subscribe(conversation.Topic!);
                    _logs.Add($"Solicitação de conversa aceita por {conversation.To} através do tópico {conversation.Topic}");
                }
                else
                {
                    _conversationsDao.RemoveConversation(conversation);
                    _logs.Add($"Solicitação de conversa recusada por {conversation.To}");
                }

                return;
            }

            var receivedGroupRequest = JsonSerializer.Deserialize<GroupRequest>(payload);
            if (receivedGroupRequest != null)
            {
                if (!_groupsDao.GroupAlreadyExists(receivedGroupRequest.GroupName))
                {
                    return;
                }

                var group = _groupsDao.FindGroupByName(receivedGroupRequest.GroupName);
                if (!group.ContainsLeader(_user))
                {
                    return;
                }

                if (_debug)
                {
                    _logs.Add($"Solicitação de grupo recebida. O usuário {receivedGroupRequest.Username} pediu para entrar no grupo {receivedGroupRequest.GroupName}");
                }

                group.AddRequest(receivedGroupRequest);
            }
        }

        private void HandleUsersMessage(string payload)
        {
            var receivedUser = JsonSerializer.Deserialize<User>(payload);
            if (receivedUser != null)
            {
                _usersDao.SaveUser(receivedUser);
            }
        }

        private void HandleGroupsMessage(string payload)
        {
            var receivedGroup = JsonSerializer.Deserialize<Group>(payload);
            if (receivedGroup != null)
            {
                _groupsDao.SaveGroup(receivedGroup);
                if (_debug)
                {
                    _logs.Add($"Grupo {receivedGroup.Name} recebido, {receivedGroup.Leader.Username} é seu líder");
                }
            }
        }

        private void HandleConversationMessage(string topic, string payload)
        {
            var receivedMessage = JsonSerializer.Deserialize<Message>(payload);
            if (receivedMessage != null)
            {
                var conversation = _conversationsDao.FindConversationByTopic(topic);
                if (conversation != null && _status.IsChatting()) //TODO: and if the user is not chatting? the message is lost?
                {
                    if (receivedMessage.From != _user.Username && _status.IsChattingWith() == conversation.With(_user.Username))
                    {
                        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                        Console.WriteLine($"{conversation.With(_user.Username)} [{receivedMessage.FormattedSendedAt}]: {receivedMessage.Content}");
                        Console.Write("Você: ");
                    }
                }
            }
        }

        private void HandleGroupsConversationMessage(string topic, string payload)
        {
            string groupName = topic.Substring(topic.IndexOf(GroupsConversationTopic), topic.Length);
            var receivedMessage = JsonSerializer.Deserialize<Message>(payload);
            var group = _groupsDao.FindGroupByName(groupName);
            if (group == null)
            {
                return;
            }
            if (!group.ContainsLeader(_user) && !group.ContainsMember(_user))
            {
                return;
            }
            // TODO: show message if chatting
        }

        public void GoOnline()
        {
            _user.GoOnline();
            _bus.Publish($"{UsersTopic}{_user.Username}", _user, true);
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
            _bus.Publish(topic, conversation);

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

                _bus.Publish(controlTopic, conversation);
                _bus.Subscribe(conversation.Topic!);

                if (_debug)
                {
                    _logs.Add($"{_user.Username} aceitou conversar com {requestToAccept.From} através do tópico {chatTopic}");
                }

                Console.WriteLine($"Solicitação de {requestToAccept.From} aceita, vocês já podem iniciar uma conversa");
            }
            else
            {
                _conversationsDao.RemoveConversation(conversation);
                _bus.Publish(controlTopic, conversation);
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
                string lastMessage = string.Empty;
                if (conversation.LastMessage != null)
                {
                    lastMessage = $" - {conversation.LastMessage.FormattedSendedAt}";
                }
                Console.WriteLine($"Conversar com {conversation.With(_user.Username)}{lastMessage}");
            }

            string? to = null;
            while (string.IsNullOrWhiteSpace(to))
            {
                Console.Write("Com quem você deseja conversar? ");
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

                if (!conversations.Any(c => c.With(_user.Username) == to))
                {
                    Console.WriteLine($"Você não possui uma conversa com {to}");
                    to = null;
                }
            }

            var selectedConversation = conversations.First(c => c.With(_user.Username) == to);
            _status.StartChatting(selectedConversation.With(_user.Username));
            Console.WriteLine("Sempre que quiser enviar uma mensagem, digite e pressione enter");
            Console.WriteLine("Caso queira sair da conversa, deixe em branco e pressione enter");

            string? content = null;
            while (string.IsNullOrWhiteSpace(content))
            {
                Console.Write("Você: ");
                content = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine($"Saindo da conversa com {_status.IsChattingWith()}");
                    _status.StopChatting();
                    break;
                }

                var message = new Message(_user.Username, content);
                _bus.Publish(selectedConversation.Topic!, message);
                content = null;
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
                if (group.ContainsMember(_user))
                {
                    membership = " (você faz parte deste grupo)";
                }
                else if (group.ContainsLeader(_user))
                {
                    membership = " (você é líder deste grupo)";
                }

                Console.WriteLine($"Grupo: {group.Name}{membership}");
                Console.WriteLine("Membros:");
                Console.WriteLine($" - {group.Leader.Username} (líder)");
                
                foreach (var member in group.Members.OrderBy(m => m.Username))
                {
                    string isYou = member.Username == _user.Username ? " (você)" : "";
                    Console.WriteLine($" - {member.Username}{isYou}");
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
            _bus.Publish($"{GroupsTopic}{group.Name}", group, true);

            if (_debug)
            {
                _logs.Add($"Grupo {group.Name} criado pelo usuário {group.Leader.Username} que agora é seu líder");
            }
        }

        public void JoinGroup()
        {
            string? groupName = null;
            while (string.IsNullOrWhiteSpace(groupName))
            {
                Console.Write("A qual grupo você gostaria de se juntar? ");
                groupName = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(groupName))
                {
                    Console.WriteLine("Você precisa informar o nome do grupo o qual deseja se juntar");
                    continue;
                }

                bool groupExists = _groupsDao.GroupAlreadyExists(groupName);
                if (!groupExists)
                {
                    Console.WriteLine("O grupo informado não existe");
                    groupName = null;
                }
            }

            var group = _groupsDao.FindGroupByName(groupName);

            if (group.ContainsLeader(_user))
            {
                Console.WriteLine("Você é o líder deste grupo");
                return;
            }

            if (group.ContainsMember(_user))
            {
                Console.WriteLine("Você já faz parte deste grupo");
                return;
            }

            string topic = $"{group.Leader.Username}{ControlTopicSuffix}";
            var groupRequest = new GroupRequest(group.Name, _user.Username);
            _bus.Publish(topic, groupRequest);

            if (_debug)
            {
                _logs.Add($"{_user.Username} enviou uma solicitação para se juntar ao grupo {group.Name} através do tópico {topic}. O líder {group.Leader.Username} irá aprovar ou rejeitar a solicitação");
            }

            Console.WriteLine("Solicitação enviada");
        }

        public void SendGroupMessage()
        {
            var groups = _groupsDao.FindGroupsUserIsPartOf(_user);
            if (groups.Count == 0)
            {
                Console.WriteLine("Você ainda não faz parte de nenhum grupo");
                return;
            }

            foreach (var group in groups)
            {
                string lastMessage = string.Empty;
                if (group.LastMessage != null)
                {
                    lastMessage = $" - {group.LastMessage.FormattedSendedAt}";
                }
                Console.WriteLine($"Conversar no grupo {group.Name}");
            }

            string? to = null;
            while (string.IsNullOrWhiteSpace(to))
            {
                Console.Write("Em qual grupo você deseja conversar? ");
                to = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(to))
                {
                    Console.WriteLine("Você precisa informar para qual grupo deseja enviar a mensagem");
                }
            }

            var selectedGroup = groups.First(g => g.Name == to);
            _status.StartChatting(selectedGroup.Name);
            Console.WriteLine("Sempre que quiser enviar uma mensagem, digite e pressione enter");
            Console.WriteLine("Caso queira sair da conversa, deixe em branco e pressione enter");

            string? content = null;
            while (string.IsNullOrWhiteSpace(content))
            {
                Console.Write("Você: ");
                content = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine($"Saindo da conversa do grupo {_status.IsChattingWith()}");
                    _status.StopChatting();
                    break;
                }

                var message = new Message(_user.Username, content);
                _bus.Publish($"{GroupsConversationTopic}/{selectedGroup.Name}", message);
                content = null;
            }
        }

        public void ManageGroups()
        {
            string? groupName = null;
            Group? group = null;
            while (string.IsNullOrWhiteSpace(groupName))
            {
                Console.Write("Qual grupo você quer gerenciar? ");
                groupName = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(groupName))
                {
                    Console.WriteLine("Você precisa informar o nome do grupo o qual deseja gerenciar");
                    continue;
                }

                bool groupExists = _groupsDao.GroupAlreadyExists(groupName);
                if (!groupExists)
                {
                    Console.WriteLine("O grupo informado não existe");
                    groupName = null;
                    continue;
                }

                group = _groupsDao.FindGroupByName(groupName);
                if (!group.ContainsLeader(_user))
                {
                    Console.WriteLine("Você só pode gerenciar grupos dos quais é líder");
                    groupName = null;
                    continue;
                }

                if (!group.ContainsPendingRequests())
                {
                    Console.WriteLine("O grupo não possui solicitações pendentes");
                    groupName = null;
                    continue;
                }
            }

            foreach (var request in group!.Requests)
            {
                Console.WriteLine($"1 - Solicitação de {request.Username}");
            }

            int option = -1;
            bool validOption = false;
            while (!validOption)
            {
                Console.Write(": ");
                string? optionAsText = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(optionAsText))
                {
                    Console.WriteLine("Você precisa informar uma opção");
                    continue;
                }

                if (!int.TryParse(optionAsText, out option))
                {
                    Console.WriteLine("Opção inválida");
                    continue;
                }

                if (option < 1 || option > group.Requests.Count)
                {
                    Console.WriteLine("Opção inválida");
                    continue;
                }

                var request = group.Requests.ElementAt(option - 1);
                Console.WriteLine($"Você deseja permitir que {request.Username} participe do grupo {request.GroupName}?");
                Console.WriteLine("1. Sim");
                Console.WriteLine("2. Não");
                option = -1;
                validOption = false;
                while (!validOption)
                {
                    Console.Write(": ");
                    optionAsText = Console.ReadLine();
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

                if (option == 1)
                {
                    request.Accept();
                    group.AddMember(new User(request.Username));
                }
                else
                {
                    request.Reject();
                }

                group.RemoveRequest(request);

                _bus.Publish($"{GroupsTopic}{group.Name}", group, true);

                if (_debug && option == 1)
                {
                    _logs.Add($"Grupo {group.Name} foi atualizado, {request.Username} agora é um membro");
                }
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
    
        public void GoOffline()
        {
            _user.GoOffline();
            _bus.Publish($"{UsersTopic}{_user.Username}", _user, true);
        }

        public void Disconnect()
        {
            _bus.Disconnect();
        }
    }
}
