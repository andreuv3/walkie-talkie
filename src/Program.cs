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

    var chat = new Chat();
    chat.SetUsername(username);

    var option = ChatAction.Initial;
    while (option != ChatAction.Exit)
    {
        ui.ShowMenu();
        option = ui.RequestAction();
        switch (option)
        {
            case ChatAction.RequestChat:
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
