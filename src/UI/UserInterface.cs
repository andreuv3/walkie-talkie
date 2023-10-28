using WalkieTalkie.Chat;

namespace WalkieTalkie.UI
{
    public class UserInterface
    {
        public string RequestUsername()
        {
            string? username = null;
            while (string.IsNullOrWhiteSpace(username))
            {
                Console.Write("Informe seu usário: ");
                username = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Você precisa informar seu usuário");
                }
            }
            
            Console.WriteLine($"Bem-vindo, {username}!");
            return username;
        }

        public void ShowTitle()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("             WALKIE TALKIE            ");
            Console.WriteLine("--------------------------------------");
        }

        public void ShowMenu()
        {
            Console.WriteLine("1. Visualizar usuários");
            Console.WriteLine("2. Solicitar conversa com um usuário");
            Console.WriteLine("3. Gerenciar solicitações de mensagem");
            Console.WriteLine("4. Enviar uma mensagem");
            Console.WriteLine("5. Visualizar grupos");
            Console.WriteLine("6. Criar grupo");
            Console.WriteLine("7. Participar de um grupo");
            Console.WriteLine("8. Enviar uma mensagem em grupo");
            Console.WriteLine("0. Sair");
        }

        public ChatAction RequestAction()
        {
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

                if (option < (int) ChatAction.Exit || option > (int) ChatAction.SendGroupMessage)
                {
                    Console.WriteLine("Opção inválida");
                    continue;
                }
                
                validOption = true;
            }

            return (ChatAction) option;
        }
    }
}
