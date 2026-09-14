using System.Collections.Generic;

namespace GraveAttention
{
	/// <summary>
	/// <c>ga</c> (or <c>graveattention</c>). The bare command prints the settings block and changes
	/// nothing; every line of the block names the command that changes it, so it doubles as the
	/// menu. <c>ga info</c> adds the diagnostics and counters that answer "is this thing working".
	/// </summary>
	public class ConsoleCmdGraveAttention : ConsoleCmdAbstract
	{
		public override bool IsExecuteOnClient => false;

		public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
		{
			string command = _params.Count > 0 ? _params[0].ToLowerInvariant() : string.Empty;

			switch (command)
			{
			case "":
				OutputMenu("GraveAttention is " + OnOff(Settings.Enabled));
				return;

			case "on":
			case "off":
				SetEnabled(command == "on");
				return;

			case "all":
				SetAll(_params);
				return;

			case "screamers":
				Settings.ExemptScreamers = !Settings.ExemptScreamers;
				Config.Save();
				Output(Settings.ExemptScreamers
					? "Screamers exempt - a screamer's scream always plays."
					: "Screamers governed - a screamer's scream follows the alert setting.");
				return;

			case "info":
				OutputInfo();
				return;

			case "reset":
				Counters.Reset();
				Output("Counters reset.");
				return;

			default:
				int category = Settings.CategoryIndex(command);
				if (category >= 0)
				{
					SetCategory(category, _params);
					return;
				}
				Output("Unknown option '" + _params[0]
					+ "'. Try: ga [on|off|roam|alert|sense|attack|sleeper|all|screamers|info|reset]");
				return;
			}
		}

		private static void OutputMenu(string _header)
		{
			Output(_header);
			Switch("ga on|off", EnabledChoices(), "a zombie that cannot see you keeps its voice down");
			for (int i = 0; i < Settings.CategoryCount; i++)
			{
				CategoryLine(i);
			}
			Switch("ga screamers", Choices(Mark("on", Settings.ExemptScreamers), Mark("off", !Settings.ExemptScreamers)),
				"a screamer's scream always plays");
		}

		/// <summary>One category: its command, the mode choices with the live one marked, the
		/// quiet chance, and what the category is.</summary>
		private static void CategoryLine(int _category)
		{
			string name = Settings.CategoryNames[_category];
			Line("ga " + name, ModeChoices(_category).PadRight(22) + ChanceText(_category).PadRight(5)
				+ " - " + Config.Purpose(_category));
		}

		private static string ModeChoices(int _category)
		{
			Mode live = Settings.Modes[_category];
			return Choices(Mark("off", live == Mode.Off), Mark("quiet", live == Mode.Quiet), Mark("on", live == Mode.On));
		}

		private static string ChanceText(int _category)
		{
			return Config.Number(Settings.Chance[_category]) + "%";
		}

		/// <summary>
		/// 'ga {cat}' alone is a read. 'ga {cat} off|quiet|on' sets the mode; 'ga {cat} {pct}' sets
		/// the quiet chance and, if the category was off, switches it to quiet so the number means
		/// something straight away.
		/// </summary>
		private static void SetCategory(int _category, List<string> _params)
		{
			string name = Settings.CategoryNames[_category];
			if (_params.Count < 2)
			{
				CategoryLine(_category);
				return;
			}

			string arg = _params[1].ToLowerInvariant();
			if (Settings.TryMode(arg, out Mode mode))
			{
				Settings.Modes[_category] = mode;
				Config.Save();
				Output(Explain(_category));
				return;
			}

			if (Config.TryPercent(arg, out float chance))
			{
				Settings.Chance[_category] = chance;
				if (Settings.Modes[_category] == Mode.Off)
				{
					Settings.Modes[_category] = Mode.Quiet;
				}
				Config.Save();
				Output(Explain(_category));
				return;
			}

			Output("'" + _params[1] + "' is not a mode or a chance. Usage: ga " + name
				+ " off|quiet|on, or ga " + name + " {pct} - currently: " + ModeChoices(_category)
				+ " " + ChanceText(_category));
		}

		private static void SetAll(List<string> _params)
		{
			if (_params.Count < 2 || !Settings.TryMode(_params[1].ToLowerInvariant(), out Mode mode))
			{
				Output("Usage: ga all off|quiet|on - sets every category at once.");
				return;
			}
			for (int i = 0; i < Settings.CategoryCount; i++)
			{
				Settings.Modes[i] = mode;
			}
			Config.Save();
			OutputMenu("Every category is now " + Settings.NameOf(mode));
		}

		/// <summary>What a category's setting means in play.</summary>
		private static string Explain(int _category)
		{
			string name = Settings.CategoryNames[_category];
			string what = Config.Purpose(_category);
			switch (Settings.Modes[_category])
			{
			case Mode.Off:
				return name + " OFF - " + what + ": never while it cannot see you.";
			case Mode.Quiet:
				return name + " QUIET - " + what + ": " + ChanceText(_category)
					+ " chance while it cannot see you.";
			default:
				return name + " ON - " + what + ": as vanilla, whether it sees you or not.";
			}
		}

		/// <summary>The header says whether anything moved: typing the state you were already in
		/// should not read like a change.</summary>
		private static void SetEnabled(bool _on)
		{
			bool changed = Settings.Enabled != _on;
			Settings.Enabled = _on;
			if (changed)
			{
				Config.Save();
			}
			OutputMenu("GraveAttention is " + (changed ? "now " : "already ") + OnOff(_on));
		}

		private static string OnOff(bool _on)
		{
			return _on ? "ON" : "OFF";
		}

		/// <summary>The menu, with the read-only lines appended in the same column.</summary>
		private static void OutputInfo()
		{
			OutputMenu("GraveAttention is " + OnOff(Settings.Enabled));
			Line("settings file", Config.Status);
			Line("Undead Legacy", UndeadLegacyInfo.Status);
			Line("vocal hook", Patches.VocalHookStatus);
			for (int i = 0; i < Settings.CategoryCount; i++)
			{
				Line(Settings.CategoryNames[i] + " vocals", "seen " + Counters.Seen[i]
					+ ", suppressed " + Counters.Suppressed[i]
					+ ", quiet-passed " + Counters.QuietPassed[i]
					+ ", vanilla " + Counters.Vanilla[i]);
			}
			Line("no player nearby", Counters.NoPlayer.ToString());
			Line("last event", Counters.Last);

			int reached = Counters.Total(Counters.Seen) + Counters.Total(Counters.Suppressed)
				+ Counters.Total(Counters.QuietPassed) + Counters.Total(Counters.Vanilla);
			if (reached == 0)
			{
				Output("Note: no zombie vocal has reached the hook yet. If every count stays at zero");
				Output("while a zombie is chasing you, the hook is not live.");
			}
		}

		/// <summary>Labels padded to the longest one ("no player nearby") so the block shares a column.</summary>
		private static void Line(string _label, string _value)
		{
			Output("  " + _label.PadRight(16) + ": " + _value);
		}

		/// <summary>A switch line: the choices padded to the widest set, then what the switch is for.</summary>
		private static void Switch(string _label, string _choices, string _note)
		{
			Line(_label, _choices.PadRight(27) + " - " + _note);
		}

		/// <summary>The choice list for a toggle, with the live value marked.</summary>
		private static string Choices(params string[] _options)
		{
			return "[ " + string.Join(" | ", _options) + " ]";
		}

		private static string Mark(string _option, bool _live)
		{
			return _live ? ">" + _option + "<" : _option;
		}

		private static string EnabledChoices()
		{
			return Choices(Mark("on", Settings.Enabled), Mark("off", !Settings.Enabled));
		}

		private static void Output(string _line)
		{
			SdtdConsole.Instance.Output(_line);
		}

		public override string[] getCommands()
		{
			return new string[2] { "ga", "graveattention" };
		}

		public override string getDescription()
		{
			return "Reports GraveAttention's settings; 'ga on' and 'ga off' switch it.";
		}

		public override string getHelp()
		{
			return "Usage: ga [on|off|roam|alert|sense|attack|sleeper {off|quiet|on|pct}|all {off|quiet|on}"
				+ "|screamers|info|reset]"
				+ "\r\n\r\nZombies are far too vocal for the undead, and worst when they cannot see "
				+ "you: roaring at a wall, snarling at your scent from the next room, grunting at a "
				+ "door you are behind. With this mod a zombie only sounds off when it actually has "
				+ "you in sight - its own sight, the same range, cone and line-of-sight check the "
				+ "game gives it. Out of sight, each kind of vocal is muted, made rare or left alone, "
				+ "as you set it. Hurt and death cries, footsteps, and the impact of a swing landing "
				+ "are never touched, and the AI does not hear its own vocals, so nothing about "
				+ "aggro changes."
				+ "\r\n\r\n'ga' on its own prints the settings and changes nothing - it is the "
				+ "status read, so it is safe to type when you only want to look. Each line names "
				+ "the command that changes it, so the settings block is also the menu."
				+ "\r\n\r\n'ga on' and 'ga off' are the master switch. With it off the hook returns "
				+ "immediately and every other setting here is inert. Saying which state you want "
				+ "rather than toggling it means the command reads the same whichever state you "
				+ "were in, and repeating it is harmless."
				+ "\r\n\r\nFive categories, one per kind of vocal. 'roam' is the periodic groan, "
				+ "governed only while the zombie is after something - a zombie wandering with no "
				+ "target groans as vanilla, so the ambient horde soundscape is untouched. 'alert' is "
				+ "the roar on acquiring a target, and Undead Legacy's rage roar. 'sense' is the "
				+ "snarl on smelling or hearing you, which vanilla plays with no sight check at all. "
				+ "'attack' is the grunt on a swing; the swing landing is a separate sound and still "
				+ "plays. 'sleeper' is a sleeper's snores and groans."
				+ "\r\n\r\n'ga {category} off|quiet|on' sets what that vocal does while the zombie "
				+ "cannot see you. 'off' means never. 'quiet' means it plays with a percent chance, "
				+ "rolled per vocal. 'on' is vanilla. 'ga {category} {pct}' sets the quiet chance "
				+ "and, if the category was off, switches it to quiet. 'ga {category}' alone prints "
				+ "that line. 'ga all off|quiet|on' sets every category at once."
				+ "\r\n\r\nWhile the zombie can see you, every vocal plays as vanilla whatever the "
				+ "setting. 'Can see' is the zombie's own test: the player it is targeting, or the "
				+ "nearest player if it has no target, within its sight range and view cone with "
				+ "nothing solid in the way. A sleeper's view is its frozen pose, so a sleeper "
				+ "facing the wall cannot see you - which is why sleepers default to quiet rather "
				+ "than off."
				+ "\r\n\r\n'ga screamers' toggles whether a screamer is exempt, on by default. A "
				+ "screamer's horde-summoning scream is its alert sound, and hearing it is the point."
				+ "\r\n\r\nEvery setting here takes effect immediately and is written straight to a "
				+ "settings file, so it survives a restart - and survives updating the mod, because "
				+ "the file lives in the game's user data folder next to Saves rather than in Mods. "
				+ "'ga info' prints its full path. It is plain 'key = value' text and can be edited "
				+ "by hand with the game closed; a line that will not parse is ignored rather than "
				+ "fatal, and deleting the file goes back to the built-in defaults in Settings.cs."
				+ "\r\n\r\n'ga info' prints the same block with the patch state and the counters "
				+ "added: per category, how many vocals played because the zombie saw you, how many "
				+ "were suppressed, how many passed a quiet roll, and how many were let through by "
				+ "an 'on' setting. The startup log only proves the hook was installed; those "
				+ "numbers prove zombie vocals are reaching it. 'ga reset' zeroes them so one "
				+ "scenario can be measured on its own."
				+ "\r\n\r\n'graveattention' is an alias for 'ga'.";
		}
	}
}
