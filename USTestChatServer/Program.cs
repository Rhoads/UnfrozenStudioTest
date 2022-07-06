using LibCSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace USTestChat.Server
{
	class Program
	{
		enum ExitCodes
		{
			CANNOT_LOAD_CONFIG = 1,
			CANNOT_START_NETWORK,
			CANNOT_OPEN_DB,
			MANUAL_EXIT,
			PROGRAM_FINISHED,
			RUNTIME_ERROR = 100,
		}

		static void Exit(ExitCodes code, string exit_message = null)
		{
			if (exit_message != null)
			{
				ConsoleColor old_clr = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(exit_message);
				Console.ForegroundColor = old_clr;
			}

			System.Environment.Exit((int)code);
		}

		static void Main(string[] args)
		{
#if (DEBUG)
			Start();
#else
			try
			{
				Start();
			}
			catch (Exception ex)
			{
				log.Error("ERROR! Caught exception: {0}{1}{2}", ex.ToString(), System.Environment.NewLine, ex.Message);
				Exit(ExitCodes.RUNTIME_ERROR);
			}
#endif

			Exit(ExitCodes.PROGRAM_FINISHED);
		}

		static void TelepathyLogInfo(string text) => log.Debug(text);
		static void TelepathyLogWarning(string text) => log.Warn(text);
		static void TelepathyLogError(string text) => log.Error(text);

		static void Start()
		{
			const string conf_filename = "server.cfg";
			if (!Config.Load(conf_filename))
				Exit(ExitCodes.CANNOT_LOAD_CONFIG, $"Cannot load config from file '{conf_filename}'");

			Logger.Init(Config.LogFileName, Config.LogLvlConsole, Config.LogLvlFile, append: true);

			log.Info("===================== Log started =====================");

			if (!DB.Init(Config.DatabaseFile))
				Exit(ExitCodes.CANNOT_OPEN_DB);

			Telepathy.Log.Info = TelepathyLogInfo;
			Telepathy.Log.Warning = TelepathyLogWarning;
			Telepathy.Log.Error = TelepathyLogError;

			if (!ChatServer.Start(Config.NetListenPort, Config.NetMaxMessageSize))
				Exit(ExitCodes.CANNOT_START_NETWORK);

			while (true)
			{
				ChatServer.Tick();

				// выход из программы по нажатию 'Q'

				if (!Console.KeyAvailable)
					continue;

				ConsoleKeyInfo key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Q)
				{
					ChatServer.Stop();
					DB.Close();

					log.Info("Manual exit");
					Exit(ExitCodes.MANUAL_EXIT);
				}
			}
		}

		static Logger log = Logger.GetCurrentClassLogger();
	}
}
