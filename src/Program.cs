using Microsoft.Extensions.Configuration;
using WalkieTalkie.Chat;
using WalkieTalkie.Chat.Data;
using WalkieTalkie.EventBus;
using WalkieTalkie.UI;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false, false)
        .Build();

    var mosquittoConfiguration = configuration.GetRequiredSection("Mosquitto");
    string host = mosquittoConfiguration["Host"]!;
    int port = int.Parse(mosquittoConfiguration["Port"]!);
    bool debug = bool.Parse(configuration["Debug"]!);

    var ui = new UserInterface(debug);
    ui.ShowTitle();
    string username = ui.RequestUsername();

    var bus = new Bus(host, port);
    var chat = new Chat(bus, debug);
    chat.ConnectAs(username);
    chat.GoOnline();

    var option = ChatAction.Initial;
    while (option != ChatAction.Exit)
    {
        ui.ShowMenu();
        option = ui.RequestAction();
        switch (option)
        {
            case ChatAction.ListUsers:
                ui.ShowTitle("USUÁRIOS");
                chat.ListUsers();
                ui.ShowGoBackMessage();
                break;
            case ChatAction.RequestChat:
                ui.ShowTitle("SOLICITAÇÃO DE CONVERSA");
                chat.RequestChat();
                ui.ShowGoBackMessage();
                break;
            case ChatAction.SendMessage:
                ui.ShowTitle("CONVERSAS");
                chat.SendMessage();
                break;
            case ChatAction.ManageChatRequests:
                ui.ShowTitle("SOLICITAÇÕES DE CONVERSA");
                chat.ManageChatRequests();
                ui.ShowGoBackMessage();
                break;
            case ChatAction.ListGroups:
                ui.ShowTitle("GRUPOS");
                chat.ListGroups();
                ui.ShowGoBackMessage();
                break;
            case ChatAction.CreateGroup:
                ui.ShowTitle("CRIAR GRUPO");
                chat.CreateGroup();
                ui.ShowGoBackMessage("Grupo criado");
                break;
            case ChatAction.JoinGroup:
                ui.ShowTitle("PARTICIPAR DE UM GRUPO");
                chat.JoinGroup();
                ui.ShowGoBackMessage("");
                break;
            case ChatAction.SendGroupMessage:
                ui.ShowTitle("CONVERSAS EM GRUPO");
                chat.SendGroupMessage();
                ui.ShowGoBackMessage();
                break;
            case ChatAction.ManagetGroupRequests:
                ui.ShowTitle("GERENCIAR GRUPOS");
                chat.ManageGroups();
                ui.ShowGoBackMessage();
                break;
            case ChatAction.ShowLogs:
                ui.ShowTitle("[DEPURAÇÃO] Histórico");
                chat.ShowLogs();
                ui.ShowGoBackMessage();
                break;
        }
    }

    chat.GoOffline();
    chat.Disconnect();
}
catch (Exception ex)
{
    Console.WriteLine($"Ocorreu um erro inesperado durante a execução do programa: {ex.Message}");
    Console.WriteLine(ex.StackTrace);

    if (ex.InnerException != null)
    {
        Console.WriteLine($"Mais detalhes do erro: {ex.InnerException.Message}");
        Console.WriteLine(ex.InnerException.StackTrace);
    }
}
