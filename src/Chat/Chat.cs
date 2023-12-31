﻿using System.Text.Json;
using WalkieTalkie.Chat.Data;
using WalkieTalkie.Chat.Messages;
using WalkieTalkie.EventBus;

namespace WalkieTalkie.Chat
{
    public class Chat
    {
        private readonly Bus _bus;
        private readonly ConversationsDao _conversationsDao;
        private readonly UsersDao _usersDao;
        private readonly GroupsDao _groupsDao;
        private readonly bool _debug;
        private readonly Logger _logger;
        private readonly ChatStatus _status;
        private readonly User _user;

        public Chat(Bus bus, ConversationsDao conversationsDao, UsersDao usersDao, GroupsDao groupsDao, bool debug, Logger logger)
        {
            _bus = bus;
            _conversationsDao = conversationsDao;
            _usersDao = usersDao;
            _groupsDao = groupsDao;
            _debug = debug;
            _logger = logger;
            _status = new ChatStatus();
            _user = User.Nobody();
        }

        public void ConnectAs(string username)
        {
            _user.HasUsernameAndTopic(username, $"{username}{ChatConstants.ControlTopicSuffix}");
            _bus.Connect(_user.Username);
            _bus.Receive(ReceiveMessage);
            _bus.Subscribe(_user.Topic);
            _bus.Subscribe($"{ChatConstants.UsersTopic}+");
            _bus.Subscribe($"{ChatConstants.GroupsTopic}+");
            _bus.Subscribe($"{ChatConstants.GroupsConversationTopic}+");
            _bus.Subscribe($"{ChatConstants.UserConversationsHistoryTopic}{_user.Username}/+");
        }

        private void ReceiveMessage(string topic, string? payload)
        {            
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            var match = System.Text.RegularExpressions.Regex.Match(topic, ChatConstants.ConversationTopicPattern);
            if (!topic.StartsWith(ChatConstants.UserConversationsHistoryTopic) && match.Success)
            {
                HandleConversationMessage(topic, payload);
                return;
            }

            if (topic == _user.Topic)
            {
                HandleOwnControlTopicMessage(payload);
                return;
            }

            if (topic.StartsWith(ChatConstants.UsersTopic))
            {
                HandleUsersMessage(payload);
                return;
            }

            if (topic.StartsWith(ChatConstants.GroupsTopic))
            {
                HandleGroupsMessage(payload);
                return;
            }

            if (topic.StartsWith(ChatConstants.GroupsConversationTopic))
            {
                HandleGroupsConversationMessage(topic, payload);
                return;
            }

            if (topic.StartsWith($"{ChatConstants.UserConversationsHistoryTopic}{_user.Username}/"))
            {
                HandleUserConversationHistoryMessage(payload);
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
                    RegisterLog($"Solicitação de conversa recebida de {conversation.From} no tópico {_user.Username}{ChatConstants.ControlTopicSuffix}");
                }
                else if (receivedConversation.Accepted)
                {
                    conversation.Accept(receivedConversation.Topic!);
                    _bus.Publish($"{ChatConstants.UserConversationsHistoryTopic}{_user.Username}/{receivedConversation.Topic}", conversation, true);
                    _bus.Subscribe(conversation.Topic!);
                    RegisterLog($"Solicitação de conversa aceita por {conversation.To} através do tópico {conversation.Topic}");
                }
                else
                {
                    _conversationsDao.RemoveConversation(conversation);
                    RegisterLog($"Solicitação de conversa recusada por {conversation.To}");
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

                RegisterLog($"Solicitação de grupo recebida. O usuário {receivedGroupRequest.Username} pediu para entrar no grupo {receivedGroupRequest.GroupName}");

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
                
                if (receivedGroup.ContainsLeader(_user) || receivedGroup.ContainsMember(_user))
                {
                    _bus.Subscribe($"{ChatConstants.GroupsConversationTopic}/{receivedGroup.Name}");
                }

                RegisterLog($"Grupo {receivedGroup.Name} recebido, {receivedGroup.Leader.Username} é seu líder");
            }
        }

        private void HandleConversationMessage(string topic, string payload)
        {
            var receivedMessage = JsonSerializer.Deserialize<Message>(payload);
            if (receivedMessage != null)
            {
                var conversation = _conversationsDao.FindConversationByTopic(topic);
                if (conversation != null)
                {
                    if (receivedMessage.From != _user.Username)
                    {
                        if (_status.IsChattingWith() == conversation.With(_user.Username))
                        {
                            Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                            Console.WriteLine($"{conversation.With(_user.Username)} [{receivedMessage.FormattedSendedAt}]: {receivedMessage.Content}");
                            Console.Write("Você: ");
                        }
                        else
                        {
                            conversation.AddUnredMessage(receivedMessage);
                        }

                        conversation.UpdateLastMessageAt();
                    }
                }
            }
        }

        private void HandleGroupsConversationMessage(string topic, string payload)
        {
            string groupName = topic.Replace(ChatConstants.GroupsConversationTopic, "").Trim();
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

            if (receivedMessage != null && receivedMessage.From != _user.Username)
            {
                if (_status.IsChattingWith() == group.Name)
                {
                    Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                    Console.WriteLine($"{receivedMessage.From} [{receivedMessage.FormattedSendedAt}]: {receivedMessage.Content}");
                    Console.Write("Você: ");
                }
                else
                {
                    group.AddUnredMessage(receivedMessage);
                }

                group.UpdateLastMessageAt();
            }
        }

        private void HandleUserConversationHistoryMessage(string payload)
        {
            var receivedConversation = JsonSerializer.Deserialize<Conversation>(payload);
            if (receivedConversation != null && !string.IsNullOrWhiteSpace(receivedConversation.Topic))
            {
                var conversation = _conversationsDao.FindConversationByTopic(receivedConversation.Topic!);
                if (conversation == null)
                {
                    _conversationsDao.AddConversation(receivedConversation);
                }
            }
        }

        public void GoOnline()
        {
            _user.GoOnline();
            _bus.Publish($"{ChatConstants.UsersTopic}{_user.Username}", _user, true);
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
                Console.WriteLine("Deixe em branco e pressione enter se desejar cancelar");
                Console.Write("Para quem você deseja enviar a mensagem? ");
                to = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(to))
                {
                    return;
                }

                if (to == _user.Username)
                {
                    Console.WriteLine("Você não pode enviar uma mensagem para si mesmo");
                    to = null;
                }
            }

            var conversation = new Conversation (_user.Username, to);
            _conversationsDao.AddConversation(conversation);

            string topic = $"{to}{ChatConstants.ControlTopicSuffix}";
            _bus.Publish(topic, conversation);

            RegisterLog($"{_user.Username} enviou uma solicitação para {to} através do tópico {topic}");

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

            string controlTopic = $"{requestToAccept.From}{ChatConstants.ControlTopicSuffix}";
            var conversation = _conversationsDao.FindConversationFrom(requestToAccept.From);
            if (option == 1)
            {
                string chatTopic = $"{_user.Username}_{requestToAccept.From}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                
                conversation.Accept(chatTopic);

                _bus.Publish(controlTopic, conversation);
                _bus.Publish($"{ChatConstants.UserConversationsHistoryTopic}{_user.Username}/{conversation.Topic}", conversation, true);
                _bus.Subscribe(conversation.Topic!);

                RegisterLog($"{_user.Username} aceitou conversar com {requestToAccept.From} através do tópico {chatTopic}");

                Console.WriteLine($"Solicitação de {requestToAccept.From} aceita, vocês já podem iniciar uma conversa");
            }
            else
            {
                _conversationsDao.RemoveConversation(conversation);
                _bus.Publish(controlTopic, conversation);
                RegisterLog($"{_user.Username} recusou conversar com {requestToAccept.From}");
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

            int i = 1;
            foreach (var conversation in conversations)
            {
                Console.WriteLine($"{i++}. Conversar com {conversation.With(_user.Username)}");
            }
            Console.WriteLine("0. Voltar para o menu principal");

            int option = -1;
            bool validOption = false;
            while (!validOption)
            {
                Console.Write("Com quem você deseja conversar? ");
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

                if (option < 0 || option > conversations.Count)
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

            var selectedConversation = conversations.ElementAt(option - 1);
            _status.StartChatting(selectedConversation.With(_user.Username));

            if (selectedConversation.HasUnredMessages())
            {
                Console.WriteLine("Mensagens enquanto você estava fora:");
                foreach (var message in selectedConversation.UnreadMessages)
                {
                    Console.WriteLine($"{selectedConversation.With(_user.Username)} [{message.FormattedSendedAt}]: {message.Content}");
                }
                selectedConversation.ClearUnreadMessages();
            }

            Console.WriteLine("Deixe em branco e pressione enter se desejar cancelar");
            Console.WriteLine("Sempre que quiser enviar uma mensagem, digite e pressione enter");

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
                Console.WriteLine("Deixe em branco e pressione enter se desejar cancelar");
                Console.Write("Qual é o nome do grupo? ");
                groupName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    return;
                }

                bool groupAlreadyExists = _groupsDao.GroupAlreadyExists(groupName);
                if (groupAlreadyExists)
                {
                    Console.WriteLine("Já existe um grupo com este nome, tente novamente");
                    groupName = null;
                }
            }

            var group = new Group { Name = groupName, Leader = _user, Members = new HashSet<User>() };
            _bus.Publish($"{ChatConstants.GroupsTopic}{group.Name}", group, true);
            _bus.Subscribe($"{ChatConstants.GroupsConversationTopic}{group.Name}");

            RegisterLog($"Grupo {group.Name} criado pelo usuário {group.Leader.Username} que agora é seu líder");
            Console.WriteLine("Grupo criado");
        }

        public void JoinGroup()
        {
            string? groupName = null;
            while (string.IsNullOrWhiteSpace(groupName))
            {
                Console.WriteLine("Deixe em branco e pressione enter se desejar cancelar");
                Console.Write("A qual grupo você deseja se juntar? ");
                groupName = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(groupName))
                {
                    return;
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

            string topic = $"{group.Leader.Username}{ChatConstants.ControlTopicSuffix}";
            var groupRequest = new GroupRequest(group.Name, _user.Username);
            _bus.Publish(topic, groupRequest);

            RegisterLog($"{_user.Username} enviou uma solicitação para se juntar ao grupo {group.Name} através do tópico {topic}. O líder {group.Leader.Username} irá aprovar ou rejeitar a solicitação");

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

            int i = 1;
            foreach (var group in groups)
            {
                Console.WriteLine($"{i++}. Conversar no grupo {group.Name}");
            }
            Console.WriteLine("0. Voltar para o menu principal");

            int option = -1;
            bool validOption = false;
            while (!validOption)
            {
                Console.Write("Em qual grupo você deseja conversar? ");
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

                if (option < 0 || option > groups.Count)
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

            var selectedGroup = groups.ElementAt(option - 1);
            _status.StartChatting(selectedGroup.Name);

            if (selectedGroup.HasUnredMessages())
            {
                foreach (var message in selectedGroup.UnreadMessages)
                {
                    Console.WriteLine($"{message.From} [{message.FormattedSendedAt}]: {message.Content}");
                }
                selectedGroup.ClearUnreadMessages();
            }

            Console.WriteLine("Deixe em branco e pressione enter se desejar cancelar");
            Console.WriteLine("Sempre que quiser enviar uma mensagem, digite e pressione enter");

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
                _bus.Publish($"{ChatConstants.GroupsConversationTopic}{selectedGroup.Name}", message);
                content = null;
            }
        }

        public void ManageGroups()
        {
            string? groupName = null;
            Group? group = null;
            while (string.IsNullOrWhiteSpace(groupName))
            {
                Console.WriteLine("Deixe em branco e pressione enter se desejar cancelar");
                Console.Write("Qual grupo você deseja gerenciar? ");
                groupName = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(groupName))
                {
                    return;
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

            int i = 1;
            foreach (var request in group!.Requests)
            {
                Console.WriteLine($"{i++}. Solicitação de {request.Username}");
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
                    Console.WriteLine("Você precisa informar uma opção");
                    continue;
                }

                if (!int.TryParse(optionAsText, out option))
                {
                    Console.WriteLine("Opção inválida");
                    continue;
                }

                if (option < 0 || option > group.Requests.Count)
                {
                    Console.WriteLine("Opção inválida");
                    continue;
                }

                if (option == 0)
                {
                    return;
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

                _bus.Publish($"{ChatConstants.GroupsTopic}{group.Name}", group, true);

                if (option == 1)
                {
                    RegisterLog($"Grupo {group.Name} foi atualizado, {request.Username} agora é um membro");
                }
            }
        }

        private void RegisterLog(string log)
        {
            if (_debug)
            {
                _logger.Log(log);
            }
        }

        public void ShowLogs()
        {
            var logs = _logger.GetLogs();
            if (logs.Count == 0)
            {
                Console.WriteLine("Nenhum log encontrado");
                return;
            }

            foreach (string log in logs)
            {
                Console.WriteLine(log);
            }
        }
    
        public void GoOffline()
        {
            _user.GoOffline();
            _bus.Publish($"{ChatConstants.UsersTopic}{_user.Username}", _user, true);
        }

        public void Disconnect()
        {
            try 
            {
                _bus.Disconnect();
            }
            catch { }
        }
    }
}
