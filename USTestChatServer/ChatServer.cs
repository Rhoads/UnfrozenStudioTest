using LibCSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace USTestChat.Server
{
	static class ChatServer
	{
		static Telepathy.Server _server;
		static Dictionary<int, NetClient> _clients = new Dictionary<int, NetClient>();

		public static bool Start(int port, int maxMsgSize)
		{
			// create server & hook up events
			// note that the message ArraySegment<byte> is only valid until returning (allocation free)
			_server = new Telepathy.Server(maxMsgSize);

			_server.NoDelay = true;

			_server.OnConnected = OnClientConnected;
			_server.OnData = OnDataReceived;
			_server.OnDisconnected = OnClientDisconnected;

			log.Success($"Server listening on port: {port}");

			return _server.Start(port);
		}

		// tick to process incoming messages (do this in your update loop)
		public static void Tick() => _server.Tick(100);

		public static void Stop() => _server.Stop();

		// ====================================================================
		// Network callbacks
		// ====================================================================

		static void OnClientConnected(int connectionId)
		{
			log.Info($"[#{connectionId}] connected");

			_clients.Add(connectionId, new NetClient(connectionId));
		}

		static void OnDataReceived(int connectionId, ArraySegment<byte> data)
		{
			log.Trace($"[#{connectionId}] data received. Len: {data.Count} bytes. '{BitConverter.ToString(data.Array, data.Offset, data.Count)}'");

			var client = _clients[connectionId];
			string message = Encoding.UTF8.GetString(data);

			if (client.connectMsgReceived)
				HandleClientChatMessage(client, message);
			else
				HandleClientConnectMessage(client, message);
		}

		static void OnClientDisconnected(int connectionId)
		{
			log.Info($"[#{connectionId}] disconnected");

			var client = _clients[connectionId];

			if (client.connectMsgReceived)
			{
				SendBroadcast($"* '{client.name}' вышел из чата", connectionId);
				DB.InsertChatEvent(ChatEvent.Type.Disconnect, client.name, client.color, "");
			}

			_clients.Remove(connectionId);
		}

		// ====================================================================
		// Private methods
		// ====================================================================

		static string GetStringClientEnterChat(string username) => $"* '{username}' вошел в чат";
		static string GetStringClientLeaveChat(string username) => $"* '{username}' вышел из чата";
		static string GetStringClientMessage(string username, string color, string message) => $"<color=#{color}>{username}: {message}</color>";

		static void HandleClientConnectMessage(NetClient client, string message)
		{
			log.Debug("[#{0}] HandleClientConnectMessage({1})", client.connectionId, message);

			string color = message.Substring(0, 6);
			string username = message.Substring(6, message.Length - 6);

			client.connectMsgReceived = true;
			client.color = color;
			client.name = username;

			// send last 20 messages

			foreach (var chatEvent in DB.SelectLastEvents(Config.LastMessagesCount))
			{
				switch (chatEvent.type)
				{
					case ChatEvent.Type.Connect:
						SendString(client.connectionId, GetStringClientEnterChat(chatEvent.Username));
						break;
					case ChatEvent.Type.Disconnect:
						SendString(client.connectionId, GetStringClientLeaveChat(chatEvent.Username));
						break;
					case ChatEvent.Type.Message:
						SendString(client.connectionId, GetStringClientMessage(chatEvent.Username, chatEvent.Color, chatEvent.Text));
						break;
				}
			}

			SendBroadcast(GetStringClientEnterChat(client.name)); // to all

			DB.InsertChatEvent(ChatEvent.Type.Connect, username, color, "");
		}

		static void HandleClientChatMessage(NetClient client, string message)
		{
			SendBroadcast(GetStringClientMessage(client.name, client.color, message)); // to all

			DB.InsertChatEvent(ChatEvent.Type.Message, client.name, client.color, message);
		}

		static void SendBroadcast(string text, int exceptConnectionId = -1)
		{
			foreach (var kvp in _clients)
			{
				int connectionId = kvp.Key;
				if (connectionId == exceptConnectionId)
					continue;

				var client = kvp.Value;

				SendString(connectionId, text);
			}
		}

		static void SendString(int connectionId, string text)
		{
			log.Trace("SendString({0}, '{1}')", connectionId, text);

			_server.Send(connectionId, new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)));
		}

		static Logger log = Logger.GetCurrentClassLogger();
	}
}
