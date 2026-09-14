# GraveAttention

A 7 Days To Die mod. Zombies are far too vocal for the undead, and worst when they cannot see
you: roaring at a wall, snarling at your scent from the next room, grunting at a door you are
behind. With GraveAttention **a zombie only sounds off when it actually has you in sight** — its
own sight, the same range, cone and line-of-sight check the game gives it. Out of sight, each kind
of vocal is muted, made rare or left alone, as you set it.

Hurt and death cries, footsteps, and the impact of a swing landing are never touched. The AI does
not hear its own vocals, so nothing about aggro changes: a zombie that would have found you still
finds you, it just does not announce it through the wall.

## Installing

Extract the release zip into the game's `Mods/`. The mod folder is the root of the archive, so it
lands as:

```
Mods/GraveAttention/
├── ModInfo.xml
└── GraveAttention.dll
```

Load order does not matter, and nothing needs building.

## Console commands

`ga` prints the menu and changes nothing — `graveattention` is an alias. Every line names the
command that changes it and says what it is for, so the menu is also the reference:

```
GraveAttention is ON
  ga on|off       : [ >on< | off ]               - a zombie that cannot see you keeps its voice down
  ga roam         : [ off | >quiet< | on ]  25%   - groans while chasing you
  ga alert        : [ off | >quiet< | on ]  10%   - the roar on spotting you
  ga sense        : [ off | >quiet< | on ]  10%   - the snarl on smelling or hearing you
  ga attack       : [ off | >quiet< | on ]  10%   - the grunt on a swing (the impact still sounds)
  ga sleeper      : [ off | >quiet< | on ]  50%   - a sleeper's snores and groans
  ga screamers    : [ >on< | off ]               - a screamer's scream always plays
```

`ga on` and `ga off` are the master switch — with it off the hook returns immediately, so every
other setting is inert. They say which state you want rather than toggling, so the command reads
the same whichever state you were in and repeating it is harmless.

Each category line is set with `ga {category} off|quiet|on`, or `ga {category} {pct}` to set the
quiet chance (which also switches a category that was off to quiet). `ga all off|quiet|on` sets
every category at once, and `ga {category}` alone prints that line. **Changes are saved** — see
[Settings file](#settings-file).

Two more: `ga info` prints the same block with the patch state and counters added, and `ga reset`
zeroes those counters.

## What is governed

Five kinds of vocal, each with its own setting for **what it does while the zombie cannot see
you**:

| Category | What it is | Notes |
|---|---|---|
| `roam` | the periodic groan | governed only while the zombie is after something; a zombie wandering with no target groans as vanilla, so the ambient horde soundscape is untouched |
| `alert` | the roar on acquiring a target | also Undead Legacy's rage roar |
| `sense` | the snarl on smelling or hearing you | vanilla plays this with no sight check at all |
| `attack` | the grunt on a swing | the swing landing is a separate sound and still plays |
| `sleeper` | a sleeper's snores and groans | |

A category's mode is: **off** — never plays while unseen; **quiet** — plays while unseen with a
percent chance, rolled per vocal; **on** — vanilla.

**While the zombie can see you, every vocal plays as vanilla whatever the setting.** "Can see" is
the zombie's own test: the player it is targeting, or the nearest player if it has no target,
within its sight range and view cone with nothing solid in the way. A sleeper's view is its frozen
pose, so a sleeper facing the wall cannot see you — which is why sleepers default to quiet rather
than off.

**Screamers** are exempt by default (`ga screamers`): a screamer's horde-summoning scream is its
alert sound, and hearing it is the point.

## What is never touched

Hurt and death cries, footsteps, animation foley, the impact of a hit landing on you or a block,
spawn sounds, and the demolisher's warning beep. The mod recognises a vocal by matching it against
the zombie's own configured sound for each category, so only those five kinds of sound can ever be
affected — whatever names a mod gives them.

## Defaults

All settable in-game, and all written back to the settings file as soon as you set them:

| Category | Default | Why |
|---|---|---|
| roam | quiet, 25% | a chaser that lost you still occasionally groans — some tell, not constant |
| alert | quiet, 10% | the roar on targeting you through a wall is the loudest offender, so it is rare |
| sense | quiet, 10% | the "smelled you" snarl from behind a wall is precisely the complaint |
| attack | quiet, 10% | grunting at a door you are behind |
| sleeper | quiet, 50% | snores are the classic "sleeper in this room" cue; off would silence nearly all of them |
| screamers exempt | on | |

## Settings file

Every setting survives a restart. A change made with `ga` is written straight out to:

```
%APPDATA%/7DaysToDie/GraveAttention/settings.txt
```

— the game's own user data folder, next to `Saves`, rather than `Mods/GraveAttention/`, so
updating the mod does not take your settings with it. `ga info` prints the full path and whether
the last read or write worked.

It is plain `key = value` text, one line per setting, each naming the command that sets it:

```
enabled         = on       # ga on|off
roam            = quiet    # ga roam off|quiet|on - groans while chasing you
roam.chance     = 25       # ga roam {pct} - chance to play unseen when quiet
alert           = quiet    # ga alert off|quiet|on - the roar on spotting you
alert.chance    = 10       # ga alert {pct} - chance to play unseen when quiet
sense           = quiet    # ga sense off|quiet|on - the snarl on smelling or hearing you
sense.chance    = 10       # ga sense {pct} - chance to play unseen when quiet
attack          = quiet    # ga attack off|quiet|on - the grunt on a swing (the impact still sounds)
attack.chance   = 10       # ga attack {pct} - chance to play unseen when quiet
sleeper         = quiet    # ga sleeper off|quiet|on - a sleeper's snores and groans
sleeper.chance  = 50       # ga sleeper {pct} - chance to play unseen when quiet
screamers       = on       # ga screamers - leave a screamer's scream alone
```

Edit it by hand with the game closed — it is rewritten whenever a `ga` command changes something.
A line that will not parse is logged and ignored rather than fatal, and deleting the file brings
back the defaults above (which live in `Settings.cs`).

## Undead Legacy

**Not required** — the mod works fine on a plain install, and is built to sit alongside UL without
modifying anything of UL's. Tested against **UL 2.7.32**. UL renames the zombie sound groups; the
mod matches by the zombie's own configured sounds, so that makes no difference.

**Rage.** UL's rage roar is the zombie's alert sound. With `alert` quiet at 10%, a zombie you enrage from
cover usually rages silently. That is the rule working as intended, but it is worth knowing.

## Limitations

- **Host or server side only.** Zombie vocals are decided where the zombie's AI runs and sent out
  from there, so one copy on the host or dedicated server covers everyone. A copy on a client
  does nothing, and **clients on a dedicated server cannot use `ga`** — the settings live on the
  server.
- **Blood moon** is not special-cased. Horde zombies mostly have a target and lose sight of you
  constantly while pathing around a base, so expect a much quieter horde night.
- A quiet roll is per vocal, not per zombie.

## Building

Requires the .NET SDK; there are no NuGet dependencies. The mod builds in place inside the game
install, against the game's own assemblies.

```
dotnet build src/GraveAttention/GraveAttention.csproj -c Release
```

That restages `dist/GraveAttention/`, ready to copy into `Mods/`. To also build the release archive:

```
dotnet build src/GraveAttention/GraveAttention.csproj -c Release -t:Package
```

That writes `release/GraveAttention-v<version>-<date>.zip`, taking the version from `ModInfo.xml`.
Neither `dist/` nor `release/` is tracked.

## License

MIT — see `LICENSE`.
