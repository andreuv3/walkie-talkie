# WalkieTalkie

Desenvolvimento de uma aplicação de “bate-papo” (chat) com o protocolo MQTT.

Autor: André Bonfante

## Tecnologias utilizadas

O projeto foi desenvolvimento utilizando a linguagem de programação C# e o [.NET 7](https://dotnet.microsoft.com/pt-br/download/dotnet/7.0). Além disso, para realizar a comunicação com o broker Mosquitto, foi utilizada a biblioteca [M2Mqtt](https://github.com/eclipse/paho.mqtt.m2mqtt) contemplada pelo projeto paho.

## Como executar o projeto

1. Instalar o [Mosquitto](https://mosquitto.org/download/)
2. Instalar o [.NET 7.0](https://dotnet.microsoft.com/pt-br/download/dotnet/7.0)
3. Dentro do diretório raiz do projeto, executar o seguinte comando:

> Observação: Os Mosquitto deve ser configurado sem autenticação.

```
dotnet run --project src/WalkieTalkie.csproj
```

## Definições do projeto

Para desenvolver a aplicação algumas definições foram feitas, como por exemplo, o formato dos tópicos. As mensagens antes de serem publicadas, são codificadas como objetos JSON. No momento da publicação, o conteúdo JSON da mensagem é convertido em formato binário. Já no recebimento da mensagem, o oposto acontece: a mensagem é recebida no formato binário e convertida em texto, que é decodificado como JSON e, por fim, deserializado como uma instância de um objeto (de acordo com o formato da mensagem).

### Tópico de control

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

Foi definido um tópico chamado ```USERS``` o qual receberá mensagens que indicam o status (online ou offline) de um usuário. As mensagens publicadas no tópico ```USERS``` possuem o seguinte formato:

```
{ 
    "Username": "user", 
    "IsOnline": true 
}
```

### Tópico de grupos

Foi definido um tópico chamado ```GROUPS``` o qual receberá mensagens que representam a estrutura de um grupo: seu nome, seu líder e seus membros. As mensagens publicadas no tópico ```GROUPS``` possuem o seguinte formato:

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
