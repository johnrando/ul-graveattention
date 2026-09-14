namespace GraveAttention
{
	/// <summary>What a governed vocal does when the zombie cannot see the player.</summary>
	internal enum Mode
	{
		/// <summary>Never plays while unseen.</summary>
		Off,

		/// <summary>Plays while unseen with a per-category percent chance.</summary>
		Quiet,

		/// <summary>Vanilla: always plays.</summary>
		On
	}

	/// <summary>
	/// The kinds of zombie vocal this mod governs, in the order they are indexed everywhere. Each
	/// maps to one of the entity's own sound fields, so "which category is this clip" is an
	/// equality test against the zombie rather than a list of names.
	/// </summary>
	internal enum Category
	{
		/// <summary><c>soundRandom</c>: the periodic groan. Governed only while the zombie has a target.</summary>
		Roam,

		/// <summary><c>soundAlert</c>: the roar on acquiring a target, and UL's rage roar.</summary>
		Alert,

		/// <summary><c>soundSense</c>: the "smelled or heard something" snarl, which vanilla plays with no sight check at all.</summary>
		Sense,

		/// <summary><c>soundAttack</c>: the grunt on a swing. The impact itself is a different sound and is never touched.</summary>
		Attack,

		/// <summary><c>soundSleeperGroan</c> and <c>soundSleeperSnore</c>: a sleeper's ambient noises.</summary>
		Sleeper
	}

	/// <summary>
	/// Runtime knobs, all switchable from the <c>ga</c> console command. The values here are the
	/// built-in defaults; <see cref="Config"/> reads the player's file over them at startup.
	/// </summary>
	internal static class Settings
	{
		internal const int CategoryCount = 5;

		/// <summary>Master switch. When off, the hook returns on its first line.</summary>
		internal static bool Enabled = true;

		/// <summary>
		/// Per-category unseen behaviour, indexed by <see cref="Category"/>. Everything starts quiet:
		/// alert, sense and attack are the through-the-wall offenders, so their chance is low. Roam is
		/// governed only while chasing, and a groan from a chaser that lost you is a fair tell. A
		/// sleeper's sight is its frozen pose, so off would silence nearly every snore; a coin flip
		/// keeps the ambience.
		/// </summary>
		internal static Mode[] Modes = { Mode.Quiet, Mode.Quiet, Mode.Quiet, Mode.Quiet, Mode.Quiet };

		/// <summary>Percent chance an unseen vocal plays anyway, per category, when its mode is quiet.</summary>
		internal static float[] Chance = { 25f, 10f, 10f, 10f, 50f };

		/// <summary>
		/// Leave a screamer's alert alone. The horde-summoning scream is played as the scout's
		/// alert sound, and hearing it is the whole point of a screamer.
		/// </summary>
		internal static bool ExemptScreamers = true;

		internal static readonly string[] CategoryNames = { "roam", "alert", "sense", "attack", "sleeper" };

		internal static string NameOf(Category _category)
		{
			return CategoryNames[(int)_category];
		}

		internal static string NameOf(Mode _mode)
		{
			switch (_mode)
			{
			case Mode.Off:
				return "off";
			case Mode.Quiet:
				return "quiet";
			default:
				return "on";
			}
		}

		/// <summary>The category a name means, or -1. Shared by the file and the console command.</summary>
		internal static int CategoryIndex(string _name)
		{
			for (int i = 0; i < CategoryNames.Length; i++)
			{
				if (CategoryNames[i] == _name)
				{
					return i;
				}
			}
			return -1;
		}

		internal static bool TryMode(string _value, out Mode _mode)
		{
			switch (_value)
			{
			case "off":
				_mode = Mode.Off;
				return true;
			case "quiet":
				_mode = Mode.Quiet;
				return true;
			case "on":
				_mode = Mode.On;
				return true;
			default:
				_mode = Mode.On;
				return false;
			}
		}
	}
}
