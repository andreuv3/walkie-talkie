using Microsoft.Extensions.Configuration;
using WalkieTalkie.Chat;
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

    var chat = new Chat(host, port, debug);
    chat.ConnectAs(username);
    chat.SubscribeToBaseTopics();
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
                break;
            case ChatAction.SendGroupMessage:
                break;
            case ChatAction.ManagetGroupRequests:
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
