# WalkieTalkie

Desenvolvimento de uma aplicação de “bate-papo” (chat) com o protocolo MQTT.

Autor: André Bonfante

## Tecnologias utilizadas

O projeto foi desenvolvimento utilizando a linguagem de programação C# e o [.NET 7](https://dotnet.microsoft.com/pt-br/download/dotnet/7.0). Além disso, para realizar a comunicação com o broker Mosquitto, foi utilizada a biblioteca [M2Mqtt](https://github.com/eclipse/paho.mqtt.m2mqtt) contemplada pelo projeto paho.

## Como executar o projeto

1. Instalar o [Mosquitto](https://mosquitto.org/download/)
2. Instalar o [.NET 7.0](https://dotnet.microsoft.com/pt-br/download/dotnet/7.0)
3. Abrir o arquivo ```src/appsettings.json``` e configurar o host e a porta do Mosquitto, assim como habilitar ou desabilitar a flag de debug (a flag de debug permite visualizar os logs gerados pela aplicação);
4. Dentro do diretório raiz do projeto, executar o seguinte comando:

> Observação: O Mosquitto deve ser configurado sem autenticação.

```
dotnet run --project src/WalkieTalkie.csproj
```

## Definições do projeto

A aplicação é um "bate-papo" (chat) que permite que os usuários troquem mensagens entre si de forma direta ou através de grupos. Para um usuário trocar mensagem com outro, ele deverá antes enviar uma solicitação para este usuário. O usuário que receber a solicitação pode aceitá-la ou não. Caso aceite, então o solicitante irá receber uma resposta e eles poderão começar a trocar mensagens. Já para trocar mensagens através de grupos, a aplicação implementa uma funcionalidade para criação de grupos. Um grupo possui um nome (definido no momento de sua criação), um líder e os membros. O líder do grupo é o usuário que realizou a sua criação - é ele também quem define o nome do grupo. Para tornar-se membro de um grupo, um usuário deve enviar uma solicitação para o lídero do grupo. O líder do grupo poderá permitir ou não que o usuário solicitante se torne membro do grupo.

Além disso, é possível visualizar algumas informações, como os usuários existentes (e se estão online ou offline), os grupos (nome, líder e membros) e também, caso o debug estiver habilitado, visualizar todas as ações que ocorreram durante a execução da aplicação, como solicitações de conversa recebidas, aceitadas, recusadas, entre outros.

Para desenvolver a aplicação algumas definições foram feitas, como por exemplo, o formato dos tópicos. As mensagens antes de serem publicadas, são codificadas como objetos JSON. No momento da publicação, o conteúdo JSON da mensagem é convertido em formato binário. Já no recebimento da mensagem, o oposto acontece: a mensagem é recebida no formato binário e convertida em texto, que é decodificado como JSON e, por fim, deserializado como uma instância de um objeto (de acordo com o formato da mensagem).

### Tópico de controle

Foi definido um tópico de controle para cada usuário. Este tópico tem o objetivo de receber novas solicitações de mensagem e aceitá-las ou recusá-las, de acordo com as ações do usuário. Como mencionado, cada usuário possui um tópico de controle, este tópico de controle possui o formato ```<username>_CONTROL```, onde ```<username>``` é o usuário executando a aplicação. Por exemplo, se o usuário X estiver executando a aplicação, seu tópico de controle será o ```X_CONTROL```.

Na aplicação, um usuário pode receber uma solicitação de conversa. Essa solicitação de conversa será publicada em seu tópico de controle. Para entendermos melhor o formato da mensagem, vamos imaginar que o usuário Y deseja conversar com o usuário X. Para solicitar uma conversa com o usuário X, o usuário Y deverá publicar a seguinte mensagem no tópico ```X_CONTROL```:

```
{
    "From": "Y",
    "To": "X",
    "Accepted": false,
    "Topic": null,
    "Messages": []
}
```

Quando o usuário X receber esta solicitação de Y, ele poderá aceitá-la ou recusá-la. Caso recuse, deverá publicar a mensagem mensagem no tópico de controle do usuário Y, ou seja, ```Y_CONTROL```. Caso contrário, deverá definir um tópico para a conversa e publicar a seguinte mensagem no tópico de controle do usuário Y, ou seja, ```Y_CONTROL```:

```
{
    "From": "Y",
    "To": "X",
    "Accepted": true,
    "Topic": "<conversation_topic>",
    "Messages": []
}
```

Onde ```<conversation_topic>``` é o nome do tópico gerado por X para que ambos troquem mensagens. A geração do tópico de conversa também segue um padrão, que é o seguinte:```<username-to>_<username-from>_<timestamp>```. No exemplo da conversa entre X e Y, o tópico de conversa teria o seguinte nome: ```X_Y_1698613579187```. Uma vez iniciada a conversa, X e Y poderão publicar no tópico ```X_Y_1698613579187``` para trocar mensagens.

### Tópico de usuários

Cada usuário publicará seu estado em um subtópico chamado ```USERS/<username>``` (onde ```<username>``` é o nome do usuário) o qual receberá mensagens que indicam o status (online ou offline) de um usuário. As mensagens publicadas no tópico ```USERS/<username>``` possuem o seguinte formato:

```
{ 
    "Username": "user", 
    "IsOnline": true 
}
```

### Tópico de grupos

Cada grupo publicará seu estado em um subtópico ```GROUPS/<name>``` (onde ```<name>``` é o nome do grupo) o qual receberá mensagens que representam a estrutura de um grupo: seu nome, seu líder e seus membros. As mensagens publicadas no tópico ```GROUPS/<name>``` possuem o seguinte formato:

```
{ 
    "Name": "My group", 
    "Leader": { 
        "Username": "Leader",
        "IsOnline": true
    },
    "Members": [
        { 
            "Username": "Member 1",
            "IsOnline": true
        },
        { 
            "Username": "Member 2",
            "IsOnline": false
        },
        { 
            "Username": "Member 3",
            "IsOnline": true
        }
    ]
}
```
