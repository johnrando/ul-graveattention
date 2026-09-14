namespace GraveAttention
{
	/// <summary>
	/// The effect itself: a prefix on <c>Audio.Manager.BroadcastPlay(Entity, string, bool)</c>, the
	/// one method every zombie vocal goes through on the authoritative side. Roam and alert come
	/// from the timer in <c>EntityAlive.OnUpdateLive</c>, the sense snarl from
	/// <c>EAISetNearestEntityAsTarget.PlaySoundSense</c>, the attack grunt from
	/// <c>EntityAlive.UseHoldingItem</c>, UL's rage roar from <c>H_RageChancePatch</c> - all via
	/// <c>Entity.PlayOneShot</c> with default arguments - and a sleeper's <c>Snore()</c> and
	/// <c>Groan()</c> call it directly. Returning false drops the sound before it is played
	/// locally or sent to any client.
	///
	/// Nothing else can be caught by mistake: animation foley uses <c>serverSignalOnly</c> and goes
	/// to <c>Manager.Play</c> instead, footsteps have their own path, and hurt, death, spawn and
	/// impact sounds are not any of the entity's vocal fields, so <see cref="Classify"/> passes them.
	/// Zombie sounds never feed the AI's noise system (<c>Manager.SignalAI</c> only listens to
	/// players), so dropping one changes nothing about aggro.
	///
	/// Everything before the decision is allocation-free: this prefix sees every entity sound in
	/// the game, and most of them are not a zombie's.
	/// </summary>
	internal static class VocalGate
	{
		internal static bool BroadcastPlayPrefix(Entity entity, string soundGroupName, bool signalOnly)
		{
			if (!Settings.Enabled || signalOnly || soundGroupName == null)
			{
				return true;
			}

			EntityAlive zombie = entity as EntityAlive;
			if (!IsZombie(zombie))
			{
				return true;
			}

			int category = Classify(zombie, soundGroupName);
			if (category < 0)
			{
				return true;
			}

			// Idle wandering is vanilla: only a zombie that is after something is held to the rule.
			if (category == (int)Category.Roam && zombie.GetAttackTarget() == null)
			{
				return true;
			}

			// The scout's alert is the horde-summoning scream, and hearing it is the point.
			if (category == (int)Category.Alert && Settings.ExemptScreamers && zombie.IsScoutZombie)
			{
				return true;
			}

			EntityPlayer player = PickPlayer(zombie);
			if (player == null)
			{
				Counters.NoPlayer++;
				return true;
			}

			if (zombie.CanSee(player))
			{
				Counters.Seen[category]++;
				Counters.Last = Describe(zombie, category, player, "seen, played");
				return true;
			}

			switch (Settings.Modes[category])
			{
			case Mode.On:
				Counters.Vanilla[category]++;
				Counters.Last = Describe(zombie, category, player, "unseen, played (on)");
				return true;

			case Mode.Quiet:
				if (zombie.rand.RandomFloat * 100f < Settings.Chance[category])
				{
					Counters.QuietPassed[category]++;
					Counters.Last = Describe(zombie, category, player, "unseen, played (quiet roll)");
					return true;
				}
				Counters.Suppressed[category]++;
				Counters.Last = Describe(zombie, category, player, "unseen, suppressed (quiet roll)");
				return false;

			default:
				Counters.Suppressed[category]++;
				Counters.Last = Describe(zombie, category, player, "unseen, suppressed");
				return false;
			}
		}

		/// <summary>
		/// The zombie gate, as Stumblr's and CrawlerGuts'. EntityFlags comes from entityclasses.xml,
		/// so unlike a type check it excludes bandits and animals and covers modded zombies. Remote
		/// entities never originate a vocal (the AI tick is authoritative-side), but the check is
		/// cheap and keeps the rule honest.
		/// </summary>
		private static bool IsZombie(EntityAlive _entity)
		{
			if (_entity == null || (_entity.entityFlags & EntityFlags.Zombie) == EntityFlags.None)
			{
				return false;
			}
			return !_entity.isEntityRemote && !_entity.IsDead();
		}

		/// <summary>
		/// Which of the entity's own vocal fields this clip is, or -1 for anything else. The callers
		/// pass the field's own string, so reference equality catches nearly every case and the
		/// ordinal comparison is the fallback. Alert is tested first so a class that reuses one clip
		/// for two roles is held to the stricter one.
		/// </summary>
		private static int Classify(EntityAlive _zombie, string _clip)
		{
			if (Same(_clip, _zombie.soundAlert))
			{
				return (int)Category.Alert;
			}
			if (Same(_clip, _zombie.soundSense))
			{
				return (int)Category.Sense;
			}
			if (Same(_clip, _zombie.soundRandom))
			{
				return (int)Category.Roam;
			}
			if (Same(_clip, _zombie.soundAttack))
			{
				return (int)Category.Attack;
			}
			if (Same(_clip, _zombie.soundSleeperGroan) || Same(_clip, _zombie.soundSleeperSnore))
			{
				return (int)Category.Sleeper;
			}
			return -1;
		}

		private static bool Same(string _clip, string _field)
		{
			if (_field == null || _field.Length == 0)
			{
				return false;
			}
			return ReferenceEquals(_clip, _field)
				|| string.Equals(_clip, _field, System.StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Who the zombie would have to see: its target if that is a player, else the nearest one.
		/// <c>aiClosestPlayer</c> is kept fresh by the world's activity update; the world lookup
		/// covers the tick before the first one.
		/// </summary>
		private static EntityPlayer PickPlayer(EntityAlive _zombie)
		{
			EntityPlayer player = _zombie.GetAttackTarget() as EntityPlayer;
			if (!Usable(player))
			{
				player = _zombie.aiClosestPlayer;
			}
			if (!Usable(player) && _zombie.world != null)
			{
				player = _zombie.world.GetClosestPlayer(_zombie.position, -1f, false);
			}
			return Usable(player) ? player : null;
		}

		private static bool Usable(EntityPlayer _player)
		{
			return _player != null && !_player.IsDead() && _player.IsSpawned();
		}

		private static string Describe(EntityAlive _zombie, int _category, EntityPlayer _player, string _outcome)
		{
			return Settings.CategoryNames[_category] + " from " + _zombie.EntityName + " #" + _zombie.entityId
				+ " toward " + _player.EntityName + ": " + _outcome;
		}
	}
}
