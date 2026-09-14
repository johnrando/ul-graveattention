namespace GraveAttention
{
	/// <summary>
	/// Live counters behind <c>ga info</c>: the startup log proves the patch was installed, these
	/// prove zombie vocals are reaching it and what happened to each. All indexed by
	/// <see cref="Category"/>. No locking: all writes happen on the main thread.
	/// </summary>
	internal static class Counters
	{
		/// <summary>Governed vocals that played because the zombie could see the player.</summary>
		internal static readonly int[] Seen = new int[Settings.CategoryCount];

		/// <summary>Governed vocals dropped: unseen, and either off or a failed quiet roll.</summary>
		internal static readonly int[] Suppressed = new int[Settings.CategoryCount];

		/// <summary>Unseen vocals that played anyway on a quiet roll.</summary>
		internal static readonly int[] QuietPassed = new int[Settings.CategoryCount];

		/// <summary>Unseen vocals let through because the category is on.</summary>
		internal static readonly int[] Vanilla = new int[Settings.CategoryCount];

		/// <summary>Governed vocals let through because no player could be found to be unseen by.</summary>
		internal static int NoPlayer;

		/// <summary>The last decision, for <c>ga info</c>.</summary>
		internal static string Last = "no zombie vocal has reached the hook yet";

		internal static int Total(int[] _counter)
		{
			int total = 0;
			for (int i = 0; i < _counter.Length; i++)
			{
				total += _counter[i];
			}
			return total;
		}

		internal static void Reset()
		{
			for (int i = 0; i < Settings.CategoryCount; i++)
			{
				Seen[i] = 0;
				Suppressed[i] = 0;
				QuietPassed[i] = 0;
				Vanilla[i] = 0;
			}
			NoPlayer = 0;
			Last = "counters reset";
		}
	}
}
