using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace LibCSharp
{
	public class Logger
	{
		const int MAX_CAPACITY = 2000;

		// logs writes per second (RELEASE/DEBUG - minimal difference at all modes < 5%)

		// file + console | console | file (SSD) |    none
		//      2300      |  2400   |  230 000   | 14 000 000

		public enum Level
		{
			Trace,
			Debug,
			Info,
			Warn,
			Success,
			Error,
			No_log
		}

#if UNITY_5 || UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020 || UNITY_2021 || UNITY_EDITOR
		public static bool UseUnityDebugLog
#if UNITY_EDITOR
			= true;
#else
			= false;
#endif
#endif

		readonly string _classNameLogFormatted;
		readonly string _className;

		public static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";

		public static void Init(string filename, Level console_log_level, Level file_log_level, bool append = false, string timestampFormat = null, bool showTreadId = false, bool showClassName = true, HashSet<string> excludeClasses = null, HashSet<string> includeClasses = null)
		{
			_console_log_level = console_log_level;
			_file_log_level = file_log_level;

			if (timestampFormat != null)
				TimestampFormat = timestampFormat;

			if (filename != null && filename != "" && file_log_level < Level.No_log)
				_sw = new StreamWriter(filename, append, System.Text.Encoding.Default);

			_showTreadId = showTreadId;
			_showClassName = showClassName;

			_isTerminalConsole = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

			if (includeClasses != null)
			{
				_excludeClassesList = false;
				_classesList = includeClasses;
			}

			if (excludeClasses != null)
			{
				if (includeClasses != null)
					throw new Exception("Setting both excludeClasses and includeClasses are meaningless");

				_excludeClassesList = true;
				_classesList = excludeClasses;
			}
		}

		protected Logger(string className)
		{
			_className = className;

			if (className != null)
				_classNameLogFormatted = $"[{className}] ";
			else
				_classNameLogFormatted = "";
		}

		public static Logger GetLogger(string loggerName = null)
		{
			return new Logger(loggerName);
		}

		public static Logger GetCurrentClassLogger()
		{
			return new Logger(_NameOfCallingClass());
		}

		public bool IsLogged(Level lvl)
		{
			return (lvl >= _console_log_level) || (lvl >= _file_log_level);
		}

		public void Trace(string format, params object[] args)
		{
			Log(Level.Trace, format, args);
		}

		public void Debug(string format, params object[] args)
		{
			Log(Level.Debug, format, args);
		}

		public void Info(string format, params object[] args)
		{
			Log(Level.Info, format, args);
		}

		public void Warn(string format, params object[] args)
		{
			Log(Level.Warn, format, args);
		}

		public void Error(string format, params object[] args)
		{
			Log(Level.Error, format, args);
		}

		public void Success(string format, params object[] args)
		{
			Log(Level.Success, format, args);
		}

		public void Log(Level lvl, string format, params object[] args)
		{
			if (!IsLogged(lvl))
				return;

			if (_classesList != null)
			{
				if (_excludeClassesList)
				{
					if (_classesList.Contains(_className))
						return;
				}
				else
				{
					if (!_classesList.Contains(_className))
						return;
				}
			}

			lock (mutex) // thread lock
			{
				if (_sb == null || _sb.Length > MAX_CAPACITY)
					_sb = new StringBuilder(MAX_CAPACITY);
				else
					_sb.Clear();

				_sb.Append(DateTime.Now.ToString(TimestampFormat));
				if (_showTreadId)
					_sb.Append('[').Append(System.Threading.Thread.CurrentThread.ManagedThreadId).Append("] ");
				_sb.Append(_classNameLogFormatted);
				if (args.Length > 0)
					_sb.AppendFormat(format, args);
				else
					_sb.Append(format);

				string log_str = _sb.ToString();

				if (lvl >= _console_log_level)
				{
#if UNITY_5 || UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020 || UNITY_2021 || UNITY_EDITOR
					if (UseUnityDebugLog)
						_WriteUnityLog(lvl, log_str);
					else
#endif
					_WriteConsole(lvl, log_str);
				}

				if (lvl >= _file_log_level && _sw != null)
				{
					_sw.WriteLine(log_str);
					_sw.Flush();
				}
			}
		}

#if UNITY_5 || UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020 || UNITY_2021 || UNITY_EDITOR
		static void _WriteUnityLog(Level lvl, string log_str)
		{
			switch (lvl)
			{
				case Level.Error:
					UnityEngine.Debug.LogError(log_str);
					break;
				case Level.Warn:
					UnityEngine.Debug.LogWarning(log_str);
					break;
				default:
					UnityEngine.Debug.Log(log_str);
					break;
			}
		}
#endif

		static void _WriteConsole(Level lvl, string log_str)
		{
			ConsoleColor lvl_clr = _color[(int)lvl];

			if (!_isTerminalConsole)
			{
				ConsoleColor old_clr = Console.ForegroundColor;
				if (old_clr != lvl_clr)
					Console.ForegroundColor = lvl_clr;

				Console.WriteLine(log_str);

				if (old_clr != lvl_clr)
					Console.ForegroundColor = old_clr;
			}
			else
			{
				// Set foreground color, write text with EOL, resets color to default
				Console.Out.WriteAsync($"\x1B[{_ansiColorsCode[(int)lvl_clr]}m{log_str}{_EOL}\x1B[39m");
			}
		}

		// https://stackoverflow.com/questions/48570573/how-to-get-class-name-that-is-calling-my-method
		static string _NameOfCallingClass()
		{
			string name;
			Type declaringType;
			int skipFrames = 2;
			do
			{
				MethodBase method = new StackFrame(skipFrames, false).GetMethod();
				declaringType = method.DeclaringType;
				if (declaringType == null)
				{
					return method.Name;
				}
				skipFrames++;
				name = declaringType.Name;
			}
			while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

			return name;
		}

		static bool _isTerminalConsole;
		static bool _showTreadId;
		static bool _showClassName;
		static Level _console_log_level;
		static Level _file_log_level;

		static StreamWriter _sw;
		static StringBuilder _sb;

		static bool _excludeClassesList = true;
		static HashSet<string> _classesList;

		static readonly ConsoleColor[] _color = new ConsoleColor[Enum.GetNames(typeof(Level)).Length];

		static Logger()
		{
			_color[(int)Level.Trace] = ConsoleColor.DarkGray;
			_color[(int)Level.Debug] = ConsoleColor.Gray;
			_color[(int)Level.Info] = ConsoleColor.White;
			_color[(int)Level.Warn] = ConsoleColor.Yellow;
			_color[(int)Level.Error] = ConsoleColor.Red;
			_color[(int)Level.Success] = ConsoleColor.Green;
		}

		readonly static string _EOL = Environment.NewLine;

		static byte[] _ansiColorsCode = new byte[17] // ConsoleColor => ANSI terminal code
		{
			//      ANSI		| ConsoleColor
			30, // Black		| Black
			34, // Blue			| DarkBlue
			32, // Green		| DarkGreen
			36, // Cyan			| DarkCyan
			31, // Red			| DarkRed
			35, // Magenta		| DarkMagenta
			33, // Yellow		| DarkYellow
			37, // Light gray	| Gray
			90, // Dark gray	| DarkGray
			94, // Light blue	| Blue
			92, // Light green	| Green
			96, // Light cyan	| Cyan
			91, // Light red	| Red
			95, // Light magenta| Magenta
			93, // Light yellow	| Yellow
			97, // White		| White
			
			39, // Default foreground color
		};

		static readonly object mutex = new object();
	}
}
