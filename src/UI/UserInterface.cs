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
            Console.WriteLine("1. Solicitar conversa com um usuário");
            Console.WriteLine("2. Gerenciar solicitações de mensagem");
            Console.WriteLine("3. Enviar uma mensagem");
            Console.WriteLine("4. Participar de um grupo");
            Console.WriteLine("5. Enviar uma mensagem em grupo");
            Console.WriteLine("6. Sair");
        }

        public ChatAction RequestAction()
        {
            int option = -1;
            bool validOption = false;
            while (!validOption)
            {
                Console.Write(":");
                string? optionAsText = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(optionAsText))
                {
                    Console.WriteLine("Você precisa selecionar uma opção.");
                    continue;
                }

                if (!int.TryParse(optionAsText, out option))
                {
                    Console.WriteLine("A opção precisa ser um número entre 1 e 6");
                    continue;
                }

                if (option < 1 || option > 6)
                {
                    Console.WriteLine("A opção precisa ser um número entre 1 e 6");
                    continue;
                }
                
                validOption = true;
            }

            return (ChatAction) option;
        }
    }
}
