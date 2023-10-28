using Microsoft.Extensions.Configuration;
using WalkieTalkie.Broker;
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
    var client = await BrokerFactory.BuildFromConfiguration(host, port, qos, timeout);

    var chat = new Chat(client);
    chat.SetUsername(username);
    await chat.GoOnline();

    var option = ChatAction.Initial;
    while (option != ChatAction.Exit)
    {
        ui.ShowMenu();
        option = ui.RequestAction();
        switch (option)
        {
            case ChatAction.RequestChat:
                await chat.RequestChat();
                break;
            case ChatAction.ManageChatRequests:
                break;
            case ChatAction.SendMessage:
                break;
            case ChatAction.JoinGroup:
                break;
            case ChatAction.SendGroupMessage:
                break;
        }
    }

    await chat.GoOffline();
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
