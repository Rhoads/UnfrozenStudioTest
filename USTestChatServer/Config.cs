using LibCSharp;
using System;

namespace USTestChat.Server
{
	static class Config
	{
		static public string LogFileName { get; private set; }
		static public Logger.Level LogLvlConsole { get; private set; }
		static public Logger.Level LogLvlFile { get; private set; }

		static public string DatabaseFile { get; private set; }

		static public int NetListenPort { get; private set; }
		static public int NetMaxMessageSize { get; private set; }

		static public int LastMessagesCount { get; private set; }

		static public bool Load(string conf_filename)
		{
			ConfigFile conf = new ConfigFile();
			if (!conf.Load(conf_filename))
				return false;

			try
			{
				// parse parameters

				// log

				LogFileName = conf.GetString("Log.File", "");

				LogLvlConsole = (Logger.Level)conf.GetInt("Log.LvlConsole", (int)Logger.Level.Trace);
				LogLvlFile = (Logger.Level)conf.GetInt("Log.LvlFile", (int)Logger.Level.Trace);

				// db

				DatabaseFile = conf.GetString("Database.File");

				// Network

				NetListenPort = conf.GetInt("Network.TCPListenPort");
				NetMaxMessageSize = conf.GetInt("Network.MaxMessageSize");

				// Programm

				LastMessagesCount = conf.GetInt("LastMessagesCount", 20);
			}
			catch (Exception ex)
			{
				log.Error(ex.Message);
				return false;
			}

			return true;
		}

		static Logger log = Logger.GetCurrentClassLogger();
	}
}
