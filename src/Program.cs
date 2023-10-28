using Microsoft.Extensions.Configuration;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false, false)
        .Build();
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
