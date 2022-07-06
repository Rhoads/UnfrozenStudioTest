# UnfrozenStudioTest

**USTestChatClient** - клиент чата (Unity 2020.3)  
**USTestChatServer** - сервер чата (C# net core 2.0). Настройки - в файле [server.cfg](https://github.com/Rhoads/UnfrozenStudioTest/blob/master/USTestChatServer/server.cfg).  
**ClientBuild** - скомпилированный клиент (коннектится к серверу на localhost).  

Для обмена данными по сети используется библиотека [Telepathy](https://github.com/vis2k/Telepathy).  
Данные и сообщения клиентов сервер хранит в БД SQLite.  
