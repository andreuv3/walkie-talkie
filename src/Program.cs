using Microsoft.Extensions.Configuration;
using WalkieTalkie.Chat;
using WalkieTalkie.UI;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false, false)
        .Build();

    var ui = new UserInterface();
    ui.ShowTitle();
    string username = ui.RequestUsername();

    var mosquittoConfiguration = configuration.GetRequiredSection("Mosquitto");
    string host = mosquittoConfiguration["Host"]!;
    int port = int.Parse(mosquittoConfiguration["Port"]!);
    int qos = int.Parse(mosquittoConfiguration["Qos"]!);
    int timeout = int.Parse(mosquittoConfiguration["Timeout"]!);

    var chat = new Chat(host, port, qos, timeout);
    chat.SetUsername(username);
    chat.Connect();
    chat.SubscribeToOwnTopic();
    chat.SubscribteToUsersTopic();
    chat.SubscribeToGroupsTopic();
    chat.GoOnline();

    var option = ChatAction.Initial;
    while (option != ChatAction.Exit)
    {
        ui.ShowMenu();
        option = ui.RequestAction();
        switch (option)
        {
            case ChatAction.ListUsers:
                chat.ListUsers();
                break;
            case ChatAction.RequestChat:
                chat.RequestChat();
                break;
            case ChatAction.ManageChatRequests:
                chat.ManageChatRequests();
                break;
            case ChatAction.SendMessage:
                break;
            case ChatAction.ListGroups:
                chat.ListGroups();
                break;
            case ChatAction.CreateGroup:
                chat.CreateGroup();
                break;
            case ChatAction.JoinGroup:
                break;
            case ChatAction.SendGroupMessage:
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
