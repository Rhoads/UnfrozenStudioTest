using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace USTestChat.Server
{
	class ChatEvent
	{
		public enum Type
		{
			Message,
			Connect,
			Disconnect
		}

		public Type type;
		public string Username;
		public string Color;
		public string Text;
	}

static class DB
	{
		// ====================================================================
		// Private data
		// ====================================================================

		static SQLiteConnection _db;

		static SQLiteCommand _cmdSelectEvents;
		static SQLiteCommand _cmdInsertEvent;

		// ====================================================================
		// Public methods
		// ====================================================================

		public static bool Init(string database)
		{
			var conn_str = new SQLiteConnectionStringBuilder();
			conn_str.DataSource = database;
			conn_str.ForeignKeys = true;

			_db = new SQLiteConnection(conn_str.ConnectionString); // auto create if absent
			_db.Open(); // TODO: try / catch

			// create table if not exists
			(new SQLiteCommand("CREATE TABLE IF NOT EXISTS `events` (`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, `type`  INTEGER, `username`  TEXT, `color` TEXT, `text`  TEXT)", _db)).ExecuteNonQuery();

			//                                             0         1          2       3
			_cmdSelectEvents = new SQLiteCommand("SELECT `type`, `username`, `color`, `text` FROM `events` WHERE `id` > (SELECT COUNT(`id`) FROM `events`) - $limit ORDER BY `id` LIMIT $limit", _db);
			_cmdInsertEvent = new SQLiteCommand("INSERT INTO `events` (`type`, `username`, `color`, `text`) VALUES($type, $username, $color, $text)", _db);

			return true;
		}

		public static void Close() => _db.Close();

		public static IEnumerable<ChatEvent> SelectLastEvents(int size)
		{
			_cmdSelectEvents.Reset(); // ?
			_cmdSelectEvents.Parameters.AddWithValue("limit", size);
			var row = _cmdSelectEvents.ExecuteReader();
			while (row.Read())
			{
				yield return new ChatEvent {
					type = (ChatEvent.Type)row.GetInt32(0),
					Username = row.GetString(1),
					Color = row.GetString(2),
					Text = row.GetString(3)
				};
			}

			row.Close();
		}

		public static void InsertChatEvent(ChatEvent.Type type, string username, string color, string text)
		{
			_cmdInsertEvent.Parameters.AddWithValue("type", (int)type);
			_cmdInsertEvent.Parameters.AddWithValue("username", username);
			_cmdInsertEvent.Parameters.AddWithValue("color", color);
			_cmdInsertEvent.Parameters.AddWithValue("text", text);

			_cmdInsertEvent.ExecuteNonQuery();
		}
	}
}
