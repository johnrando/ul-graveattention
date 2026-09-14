using System;
using System.Reflection;
using HarmonyLib;

namespace GraveAttention
{
	/// <summary>
	/// Installs the one Harmony patch. Resolved late and gated on its prerequisite, so a game
	/// update that moves the method degrades to a log line rather than an exception at init.
	///
	/// No load order needs declaring: UL applies its patches as a BepInEx plugin before any
	/// IModApi.InitMod runs, and ModManager loads every mod assembly before calling any InitMod.
	/// </summary>
	internal static class Patches
	{
		internal const string LogPrefix = "[GraveAttention] ";

		private const string HarmonyId = "GraveAttention";

		/// <summary>Outcome of the patch, as reported by <c>ga info</c>.</summary>
		internal static string VocalHookStatus = "not applied - mod init has not run";

		private static bool applied;

		internal static void Apply()
		{
			if (applied)
			{
				return;
			}
			applied = true;
			try
			{
				ApplyPatches();
			}
			catch (Exception e)
			{
				Log.Error(LogPrefix + "Failed to apply patches; zombies will be as loud as ever.");
				Log.Exception(e);
			}
		}

		private static void ApplyPatches()
		{
			UndeadLegacyInfo.Report();

			Harmony harmony = new Harmony(HarmonyId);
			ApplyVocalHook(harmony);
		}

		/// <summary>
		/// The one method every entity sound is broadcast through on the authoritative side. UL
		/// has no patch on Audio.Manager at all, and the sibling mods hook EntityAlive.OnUpdateLive,
		/// so nothing contends for it.
		/// </summary>
		private static void ApplyVocalHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(Audio.Manager), "BroadcastPlay", new[] { typeof(Entity), typeof(string), typeof(bool) });
			if (target == null)
			{
				VocalHookStatus = "NOT APPLIED - Audio.Manager.BroadcastPlay(Entity, string, bool) not found";
				Log.Error(LogPrefix + "Vocal hook NOT applied: Audio.Manager.BroadcastPlay(Entity, "
					+ "string, bool) could not be found, so zombie vocals are untouched.");
				return;
			}

			_harmony.Patch(target, prefix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(VocalGate), nameof(VocalGate.BroadcastPlayPrefix))));

			VocalHookStatus = "applied - prefix on Audio.Manager.BroadcastPlay(Entity, string, bool)";
			Log.Out(LogPrefix + "Vocal hook applied: a zombie that cannot see you keeps its voice down"
				+ " (roam " + Summary(Category.Roam) + ", alert " + Summary(Category.Alert)
				+ ", sense " + Summary(Category.Sense) + ", attack " + Summary(Category.Attack)
				+ ", sleeper " + Summary(Category.Sleeper) + ").");
		}

		private static string Summary(Category _category)
		{
			int i = (int)_category;
			return Settings.Modes[i] == Mode.Quiet
				? "quiet " + Config.Number(Settings.Chance[i]) + "%"
				: Settings.NameOf(Settings.Modes[i]);
		}
	}
}
