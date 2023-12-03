# WalkieTalkie

Desenvolvimento de uma aplicação de “bate-papo” (chat) com o protocolo MQTT.

Autor: André Bonfante

## Tecnologias utilizadas

O projeto foi desenvolvimento utilizando a linguagem de programação C# e o [.NET 7](https://dotnet.microsoft.com/pt-br/download/dotnet/7.0). Além disso, para realizar a comunicação com o broker Mosquitto, foi utilizada a biblioteca [M2Mqtt](https://github.com/eclipse/paho.mqtt.m2mqtt) contemplada pelo projeto paho.

## Como executar o projeto

1. Instalar o [Mosquitto](https://mosquitto.org/download/)
2. Instalar o [.NET 7.0](https://dotnet.microsoft.com/pt-br/download/dotnet/7.0)
3. Abrir o arquivo ```src/appsettings.json``` e configurar o host e a porta do Mosquitto, assim como habilitar ou desabilitar a flag de debug (a flag de debug permite visualizar os logs gerados pela aplicação);
4. Dentro do diretório raiz do projeto, executar o seguintes comandos:

> Observação: O Mosquitto deve ser configurado sem autenticação.

```
chmod +x run.sh
./run.sh
```

Ou então executar o seguinte comando:

```
dotnet run --project src/WalkieTalkie.csproj
```

## Definições do projeto

O objetivo do trabalho é a implementação de uma aplicação de “bate-papo” (chat), baseada no protocolo MQTT. Ou seja, implementar a comunicação entre usuários, seja individualmente ou em grupo, utilizando somente troca de mensagens através do protocolo MQTT.

A aplicação implementa algumas funcionalidades básicas de aplicativos de troca de mensagens entre usuários. Dito isso, a primeira funcionalidade é permitir que o usuário se identifique. Para que isso seja possível, logo ao iniciar a aplicação o usuário deverá informar qual é o seu nome ou identificador. Além de se identificar, o usuário possui um status. Como se trata de uma aplicação de troca de mensagens, o usuário pode assumir o status online (quando está ativo, ou seja, utilizando a aplicação), ou então offline (quando está inativo, ou seja, não está utilizando a aplicação). O nome/identificador informado pelo usuário será utilizado para realizar a comunicação. Quer dizer, quando um usuário deseja se comunicar com outro usuário, ele deverá informar este nome/identificador do usuário ao qual deseja enviar a mensagem.
	
Nesse sentido, uma das funcionalidades que a aplicação implementa é a comunicação um-a-um, ou seja, a comunicação direta entre dois usuários. Antes de iniciar a conversa, o usuário deverá enviar uma solicitação de conversa. O usuário que recebeu essa solicitação pode, então, decidir se deseja aceitá-la ou não. Caso não aceite, esses dois usuários não poderão iniciar a conversa. Caso aceite, esses dois usuários poderão iniciar a conversa. 

Um usuário pode visualizar todas as conversas ativas (ou seja, conversas que foram iniciadas a partir de solicitações que enviou ou recebeu - e foram aceitas) e, a partir disso, escolher em qual delas deseja enviar mensagens. Uma vez escolhida, ele poderá enviar mensagens e também receberá as mensagens enviadas pelo outro usuário participante da conversa. Caso alguma mensagem seja enviada nessa conversa em um momento em que o usuário online (ou seja, com a aplicação aberta), porém não está com esta conversa aberta (ou seja, está realizando outra tarefa, como criar um grupo, aceitar uma solicitação, etc), ele ainda poderá visualizá-la quando acessar a conversa.
	
Para auxiliar no momento de solicitar uma nova conversa, momento em que o usuário precisa informar qual é o nome/identificador do usuário com quem deseja conversar, é possível listar todos os usuários. Nesta listagem são exibidos os nomes dos usuários e seus respectivos status (online ou offline).
	
Além da comunicação um-a-um. Os usuários também podem se comunicar através de grupos. Para que isso seja possível, no entanto, a aplicação implementa a funcionalidade de criação de grupos. Um grupo é representado por: um nome, um líder e seus membros. O líder do grupo é o usuário responsável pela criação do grupo. O nome do grupo é informado pelo seu líder (usuário) no momento da criação. O líder tem um papel de gerenciamento do grupo. Quer dizer, o grupo pode possuir membros (outros usuários). 

Para que um usuário seja membro de um grupo, ele deve enviar uma solicitação, pedindo para fazer parte daquele grupo. Essa solicitação é encaminhada ao líder do grupo. A partir dessa solicitação, o líder do grupo decide se aquele usuário pode ou não se tornar um membro. Caso o líder aceite a solicitação, o usuário poderá conversar com os membros do grupo e também com o líder ao mesmo tempo. Dessa forma, a aplicação implementa a comunicação em grupo. Caso o líder do grupo não aceite a solicitação, o usuário não poderá se comunicar com o grupo. 

Assim como nas conversas com outros usuários, um usuário pode visualizar todas as conversas ativas com grupo (ou seja, conversas com grupos que é membro) e, a partir disso, escolher em qual grupo deseja conversar. O recebimento de mensagens enquanto está realizando outra tarefa é o mesmo da comunicação um-a-um.
Para ajudar o usuário a saber de quais grupos ele pode fazer parte, a aplicação disponibiliza uma funcionalidade de listagem de grupos. Essa funcionalidade faz com que todos os grupos existentes sejam listados. Nesta listagem são exibidos: o nome do grupo, o líder do grupo e todos os membros do grupo.

Como já mencionado, somente o líder do grupo pode gerenciar - aceitar ou recusar - as solicitações de usuários que desejam se tornar membros daquele grupo. Um usuário pode criar quantos grupos desejar, ou seja, pode ser líder de diversos grupos e fazer o gerenciamento de todos eles.

Por fim, existe uma funcionalidade básica que é a operação de “sair”. Quer dizer, há uma funcionalidade onde o usuário comunica que deseja desconectar-se da aplicação. Neste momento, o status do usuário é atualizado de online para offline. Todos os usuários ficarão sabendo da atualização de status deste usuário.

### Tópico de usuários

Há um tópico também para representar o status dos usuários. Cada usuário publicar em um tópico próprio para informar se está online (ao iniciar a aplicação) ou offline (ao encerrar a aplicação). A publicação é feita de maneira que a última mensagem fica retida (retain flag é true), dessa forma sempre é possível saber o último estado de cada usuário. Este tópico possui um prefixo (“USERS”) seguido do nome do usuário. Ainda no exemplo do usuário chamado “tux”, ao iniciar a aplicação, o cliente irá publicar uma mensagem no tópico “USERS/tux” informando que o usuário está online. Ao encerrar a aplicação, outra mensagem será publicada neste tópico, agora informando que o usuário está offline. Somente a última mensagem publicada fica retida. 

As mensagens publicadas neste tópico possuem dois principais campos: o nome do usuário e uma flag que indica se ele está online ou offline. Além de publicar neste tópico, cada cliente (ou usuário) se inscreve através da utilização de um coringa (+, ou seja “USERS/+”), desta forma um usuário recebe todas as modificações de status de todos os outros usuários, tornando possível uma listagem que exibe, além do nome de cada usuário, seu status. Exemplo de mensagem publicada neste tópico: 

```
{ 
    "Username": "tux", 
    "IsOnline": true 
}
```

### Tópico de controle

O tópico de controle recebe mensagens relativas às ações que um usuário deve tomar ou saber. Por exemplo, neste tópico serão publicadas mensagens de solicitação de conversa, assim como de conversas que foram aceitas ou recusadas. Se o usuário for líder de um grupo, as solicitações de novos membros também serão publicadas neste tópico. Cada usuário possui seu tópico de controle, cujo nome é formado pelo nome do usuário e o sufixo “_CONTROL”. Vamos assumir a existência de um usuário chamado “tux”. O tópico de controle deste usuário será “tux_CONTROL”. O usuário pode se inscrever e publicar no próprio tópico de controle, porém os demais usuários podem somente publicar.

Como mencionado, as mensagens publicadas neste tópico podem possuir mais de um formato. São eles:

- Solicitação para conversar: esta mensagem possui, principalmente, os campos de “remetente” (From) e “destinatário” (To). Exemplo de mensagem publicada neste tópico:
```
{ 
    “From”: “tux”, 
    “To”: “Linus” 
}
```

- Gerenciamento de conversa (aceitar ou recusar): esta mensagem possui os mesmos campos da mensagem de solicitação para conversar, porém também possui uma flag que indica se a conversa foi aceita ou não (Accepted) e também, caso tenha sido aceita, um campo com o nome do tópico utilizado para a conversação (Topic). Exemplo de mensagem publicada neste tópico: 

```
{ 
    “From”: “tux”, 
    “To”: “Linus”, 
    “Accepted” true, 
    “Topic”: “Linus_tux_1701548615125” 
}
```

- Solicitação para participar de grupo: esta mensagem possui campos para representar o nome do usuário que deseja se juntar ao grupo (Username) e o nome do grupo (GroupName). Exemplo de mensagem publicada neste tópico: 

```
{ 
    “Username”: “tux”, 
    “GroupName”: “programadores” 
}
```


### Tópico de grupos

Da mesma forma que cada usuário publica seu status em um tópico como “USERS/nome do usuário”, cada grupo tem uma mensagem publicada no tópico “GROUPS/nome do grupo”. Vamos assumir a existência de um grupo chamado “programadores”. As mensagens sobre este grupo serão publicadas no tópico “GROUPS/programadores”. Dessa maneira cada cliente (ou usuário) assina os tópicos de grupos utilizando um coringa (+), e obtém informações de todos os grupos. Ou seja, a inscrição é feita em “GROUPS/+”. Sendo assim, é possível implementar a visualização dos grupos (com nome, líder e membros). 

As mensagens deste tópico também servem para o usuário saber se sua solicitação de se juntar ao grupo foi aceita ou não. Por exemplo, se ele enviou uma solicitação para se juntar ao grupo e ela foi aceita, ele irá receber uma mensagem, pois assinou o tópico do grupo, e verá que agora faz parte da lista de membros. Dessa forma, ele conclui que foi aceito no grupo.

Os campos existentes nas mensagens publicadas neste tópico são o nome do grupo (Name), o líder, e uma lista de membros. Exemplo de mensagem publicada neste tópico: 

```
{ 
    “Name”: “programadores”, 
    “Leader”: { 
        “Username”: “Linus” 
    }, 
    “Members”: [ 
        { 
            “Username”: “Tux” 
        }, 
        { 
            “Username”: “João”  
        } 
    ] 
}
```

### Tópico de conversa um-a-um

Cada conversa entre dois usuários acontece por meio de um tópico específico. Esse tópico é definido pelo usuário que aceitou a conversa. Esses tópicos seguem um padrão em sua nomenclatura, que é “nome do usuário_nome do outro usuário_milisegundos de 01/01/1970 às 00:00 até hoje”. Vamos assumir que haja uma conversa entre os usuários “tux” e “Linus” e que o usuário “tux” enviou uma solicitação ao usuário “Linus”. 
Ao aceitar a conversa, o usuário “Linus” irá gerar (de forma automática) o tópico para a conversa, e ele será “Linus_tux_1701548615125”. 

As mensagens publicadas neste tópico possuem os campos de remetente (From), conteúdo (Content) e a data e hora de envio (SendedAt). Exemplo de mensagem publicada neste tópico: 

```
{ 
    “From”: “Tux”, 
    “Content”: “Olá, Mundo!”, 
    “SendedAt”: “12/2/2023 8:57:22 PM +00:00” 
}
```

### Tópico de conversa em grupo

O tópico de conversa em grupo tem o mesmo objetivo do tópico de conversa um-a-um, com a diferente que ele recebe mensagens de conversa em grupo, é claro. A nomenclatura desses tópicos também é diferente, cada grupo possui um tópico desse tipo e seu nome é composto pelo prefixo “GROUP_MESSAGES/nome do grupo”. Voltando ao nosso exemplo (do grupo chamado “programadores”), o nome do tópico para conversar neste grupo seria “GROUP_MESSAGES/programadores”. As mensagens publicadas neste tópico possuem a mesma estrutura das mensagens publicadas nos tópicos de conversa um-a-um.

### Tópico de histórico de conversas

Este tópico serve apenas para manter um registro de quais conversas já foram negociadas (isto é, solicitadas e aceitas) entre dois usuários, desta maneira os usuários não precisam negociar uma nova conversa a cada sessão. Os tópicos de histórico começam com o prefixo “HISTORY”, seguido do nome do usuário, seguido do tópico da conversa (tópico de conversa um-a-um). Cada usuário assina os tópicos relativos a si através da utilização de um coringa (+), ou seja, da seguinte maneira: “HISTORY/nome do usuário/+”.

As mensagens publicadas neste tópico possuem os mesmos campos da mensagem de solicitação para conversar, porém também possui um campo com o nome do tópico utilizado para a conversação (Topic).
