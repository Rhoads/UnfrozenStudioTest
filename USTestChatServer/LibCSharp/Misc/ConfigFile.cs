using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LibCSharp
{
	// Класс для чтения настроек из INI файла (упрощенный формат без секций)
	class ConfigFile
	{
		public string GetString(string key, string defval)
		{
			return HasVal(key) ? GetVal(key) : defval;
		}

		public string GetString(string key)
		{
			return GetVal(key);
		}

		public int GetInt(string key, int defval)
		{
			return HasVal(key) ? int.Parse(GetVal(key)) : defval;
		}

		public uint GetUInt(string key)
		{
			return uint.Parse(GetVal(key));
		}

		public uint GetUInt(string key, uint defval)
		{
			return HasVal(key) ? uint.Parse(GetVal(key)) : defval;
		}

		public int GetInt(string key)
		{
			return int.Parse(GetVal(key));
		}

		public float GetFloat(string key, float defval)
		{
			return HasVal(key) ? float.Parse(GetVal(key)) : defval;
		}

		public float GetFloat(string key)
		{
			return float.Parse(GetVal(key));
		}

		public bool GetBool(string key, bool defval)
		{
			return HasVal(key) ? GetBool(key) : defval;
		}

		public bool GetBool(string key)
		{
			string val = GetVal(key).ToLower();
			if (val.Equals("on") || val.Equals("yes") || val.Equals("true") || val.Equals("1"))
				return true;
			if (val.Equals("off") || val.Equals("no") || val.Equals("false") || val.Equals("0"))
				return false;

			throw new System.Exception("[ConfigFile] Bool parameter '" + key + "' has incorrect value");
		}

		public bool Load(string file_name)
		{
			try
			{
				using (StreamReader sr = new StreamReader(file_name, System.Text.Encoding.Default))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						var m = _regex.Match(line);
						if (!m.Success)
							continue;

						string param_name = m.Groups[1].Value;
						string param_value = m.Groups[2].Value;
						
						AddVal(param_name, param_value);
					}
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		string GetVal(string key)
		{
			key = key.ToLower();
			if (_values.TryGetValue(key, out string val))
				return val;

			throw new System.Exception("[ConfigFile] Parameter '" + key + "' not found");
		}

		bool HasVal(string key)
		{
			return _values.ContainsKey(key.ToLower());
		}

		void AddVal(string key, string value)
		{
			_values.Add(key.ToLower(), value);
		}

		Dictionary<string, string> _values = new Dictionary<string, string>();
		Regex _regex = new Regex(@"^([\w\.]+)\s*=\s*(.+)$");
	}
}
