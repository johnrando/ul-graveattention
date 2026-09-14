using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace GraveAttention
{
	/// <summary>
	/// Reads <see cref="Settings"/> back at startup and writes it out whenever a <c>ga</c> command
	/// changes something. The file lives in the game's user data folder rather than in the mod
	/// folder, so it survives a mod update. Plain <c>key = value</c> text; every line names the
	/// console command that writes it. Nothing here can stop the mod working: any failure degrades
	/// to a log line and the defaults.
	/// </summary>
	internal static class Config
	{
		private const string FolderName = "GraveAttention";

		private const string FileName = "settings.txt";

		/// <summary>What the last load or save did, as reported by <c>ga info</c>.</summary>
		internal static string Status = "not loaded - mod init has not run";

		/// <summary>Where the file is, once resolved. Null means it never was.</summary>
		private static string filePath;

		/// <summary>
		/// Called once from <see cref="ModApi.InitMod"/>, before the patch goes in, so the startup
		/// log reports the player's settings rather than the defaults. A missing file is a first
		/// run: writing the defaults out is what makes the file discoverable.
		/// </summary>
		internal static void Load()
		{
			if (!Resolve())
			{
				return;
			}

			if (!File.Exists(filePath))
			{
				Save();
				return;
			}

			try
			{
				int applied = 0;
				int rejected = 0;
				foreach (string line in File.ReadAllLines(filePath))
				{
					switch (Parse(line))
					{
					case LineResult.Applied:
						applied++;
						break;
					case LineResult.Rejected:
						rejected++;
						break;
					}
				}

				Status = applied + " settings loaded"
					+ (rejected > 0 ? ", " + rejected + " line(s) ignored" : "")
					+ " - " + filePath;
				Log.Out(Patches.LogPrefix + "Settings loaded from " + filePath + ".");
			}
			catch (Exception e)
			{
				Status = "NOT LOADED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not read " + filePath + ", so the built-in "
					+ "defaults are in force: " + e.Message);
			}
		}

		/// <summary>
		/// Writes the whole file, which is what keeps the comments and ordering intact. Called by
		/// every <c>ga</c> command that changes a setting.
		/// </summary>
		internal static void Save()
		{
			if (!Resolve())
			{
				return;
			}

			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				File.WriteAllText(filePath, Compose());
				Status = "saved - " + filePath;
			}
			catch (Exception e)
			{
				Status = "NOT SAVED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not write " + filePath + ", so this change "
					+ "will not survive a restart: " + e.Message);
			}
		}

		/// <summary>Works out where the file goes, once.</summary>
		private static bool Resolve()
		{
			if (filePath != null)
			{
				return true;
			}

			try
			{
				string dir = GameIO.GetUserGameDataDir();
				if (string.IsNullOrEmpty(dir))
				{
					Status = "unavailable - the game reported no user data folder";
					return false;
				}
				filePath = Path.Combine(Path.Combine(dir, FolderName), FileName);
				return true;
			}
			catch (Exception e)
			{
				Status = "unavailable - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not work out where to keep settings, so they "
					+ "will not persist: " + e.Message);
				return false;
			}
		}

		private static string Compose()
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine("# GraveAttention settings.");
			text.AppendLine("#");
			text.AppendLine("# Read once when the game starts and rewritten whenever a 'ga' command changes");
			text.AppendLine("# something, so edit this with the game closed. Every line names the command that");
			text.AppendLine("# sets it; anything after a '#' is a comment, and a line that will not parse is");
			text.AppendLine("# ignored rather than fatal.");
			text.AppendLine("#");
			text.AppendLine("# A category's mode is what its vocal does when the zombie cannot see you:");
			text.AppendLine("# off = never plays, quiet = plays with the given percent chance, on = vanilla.");
			text.AppendLine();
			Setting(text, "enabled", OnOff(Settings.Enabled), "ga on|off");
			for (int i = 0; i < Settings.CategoryCount; i++)
			{
				string name = Settings.CategoryNames[i];
				Setting(text, name, Settings.NameOf(Settings.Modes[i]), "ga " + name + " off|quiet|on - " + Purpose(i));
				Setting(text, name + ".chance", Number(Settings.Chance[i]), "ga " + name + " {pct} - chance to play unseen when quiet");
			}
			Setting(text, "screamers", OnOff(Settings.ExemptScreamers), "ga screamers - leave a screamer's scream alone");
			return text.ToString();
		}

		/// <summary>What each category is, for the file and the menu.</summary>
		internal static string Purpose(int _category)
		{
			switch ((Category)_category)
			{
			case Category.Roam:
				return "groans while chasing you";
			case Category.Alert:
				return "the roar on spotting you";
			case Category.Sense:
				return "the snarl on smelling or hearing you";
			case Category.Attack:
				return "the grunt on a swing (the impact still sounds)";
			default:
				return "a sleeper's snores and groans";
			}
		}

		/// <summary>One setting, padded so the values and the commands each share a column.</summary>
		private static void Setting(StringBuilder _text, string _key, string _value, string _command)
		{
			_text.AppendLine(_key.PadRight(16) + "= " + _value.PadRight(8) + " # " + _command);
		}

		private enum LineResult
		{
			/// <summary>Blank or a comment.</summary>
			Skipped,

			Applied,

			Rejected
		}

		/// <summary>
		/// One line of the file. An unknown key is a warning rather than an error: that is what a
		/// file written by a newer version of the mod looks like to an older one.
		/// </summary>
		private static LineResult Parse(string _line)
		{
			int comment = _line.IndexOf('#');
			string text = (comment >= 0 ? _line.Substring(0, comment) : _line).Trim();
			if (text.Length == 0)
			{
				return LineResult.Skipped;
			}

			int split = text.IndexOf('=');
			if (split <= 0)
			{
				Log.Warning(Patches.LogPrefix + "Ignoring a settings line that is not 'key = value': "
					+ _line.Trim());
				return LineResult.Rejected;
			}

			string key = text.Substring(0, split).Trim().ToLowerInvariant();
			string value = text.Substring(split + 1).Trim().ToLowerInvariant();
			if (Apply(key, value))
			{
				return LineResult.Applied;
			}

			Log.Warning(Patches.LogPrefix + "Ignoring settings line '" + _line.Trim()
				+ "' - unknown setting or unusable value.");
			return LineResult.Rejected;
		}

		private static bool Apply(string _key, string _value)
		{
			switch (_key)
			{
			case "enabled":
				return TryBool(_value, ref Settings.Enabled);
			case "screamers":
				return TryBool(_value, ref Settings.ExemptScreamers);
			}

			// <category> = mode, or <category>.chance = pct.
			bool chance = _key.EndsWith(".chance");
			int category = Settings.CategoryIndex(chance ? _key.Substring(0, _key.Length - 7) : _key);
			if (category < 0)
			{
				return false;
			}
			if (chance)
			{
				if (!TryPercent(_value, out float parsed))
				{
					return false;
				}
				Settings.Chance[category] = parsed;
				return true;
			}
			if (!Settings.TryMode(_value, out Mode mode))
			{
				return false;
			}
			Settings.Modes[category] = mode;
			return true;
		}

		/// <summary>Accepts what the file writes plus the obvious hand-edit synonyms.</summary>
		private static bool TryBool(string _value, ref bool _target)
		{
			switch (_value.ToLowerInvariant())
			{
			case "on":
			case "true":
			case "yes":
			case "1":
				_target = true;
				return true;
			case "off":
			case "false":
			case "no":
			case "0":
				_target = false;
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// A percentage that may be fractional, 0 to 100, against the invariant culture so a file
		/// written on one machine means the same on one whose decimal separator is a comma. Shared
		/// with the console command. <c>!(x &gt;= 0)</c> rather than <c>x &lt; 0</c> so NaN is rejected too.
		/// </summary>
		internal static bool TryPercent(string _value, out float _parsed)
		{
			return float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out _parsed)
				&& _parsed >= 0f && _parsed <= 100f;
		}

		private static string OnOff(bool _on)
		{
			return _on ? "on" : "off";
		}

		/// <summary>Written the way it is parsed, so a reported value can be typed back in.</summary>
		internal static string Number(float _value)
		{
			return _value.ToString(CultureInfo.InvariantCulture);
		}
	}
}
