# Infinity Full Assist Unlock

**English** | [简体中文](README.zh-CN.md)

This is a Reloaded-II mod for *Granblue Fantasy: Relink – Endless Ragnarok*.
It removes the restriction that normally disables Full Assist in Infinity
quests, allowing the game's built-in AI to continue fighting.

The mod does not change the AI's behavior or handle special Infinity
mechanics.

## Features

- Applies only to Infinity quests.
- Requires Full Assist to be selected in the game settings.
- Works in solo and online sessions for both Hosts and Guests.
- Does not affect other difficulties or Assist modes.
- Does not modify damage, rewards, drops, save data, or network state.

## Supported version

The current release supports only the version shown as `2.0.2` on the game's
main menu:

```text
ApplicationVersion: 2.0.2
SHA-256: 63340832BCF731FBC97796F686B05C988418E83D451D4A49B2244A85D00E297F
```

The mod checks the game executable before making any changes. If the version
or file does not match, it stays inactive to avoid a crash caused by changed
addresses after a game update.

## Requirements

- [Reloaded-II](https://github.com/Reloaded-Project/Reloaded-II) `1.30.2` or
  newer
- [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Granblue Fantasy Relink Mod Manager](https://github.com/WistfulHopes/gbfrelink.utility.manager)
  `2.0.1`
- Reloaded.Memory.SigScan.ReloadedII `1.2.14`
- Reloaded Shared Lib Hooks `1.16.3`
- Reloaded Universal Redirector `1.2.9`

Reloaded-II will normally install any missing dependencies when the mod is
imported.

## Installation

1. Add `granblue_fantasy_relink.exe` to Reloaded-II.
2. Install and enable Granblue Fantasy Relink Mod Manager.
3. Drag `Infinity-Full-Assist-Unlock-1.0.0.zip` into Reloaded-II.
4. Enable **Infinity Full Assist Unlock** for the game.
5. Launch the game through Reloaded-II.
6. Set Assist Mode to **Full Assist** before entering an Infinity quest.

For a manual installation, extract the archive contents to:

```text
Reloaded-II\Mods\gbfr.qol.infinityfullassist
```

## Configuration

```json
{
  "Enabled": true,
  "DiagnosticLogging": false
}
```

- `Enabled`: turns the mod on or off.
- `DiagnosticLogging`: writes diagnostic information to the Reloaded-II log.
  Enable it only when troubleshooting.

Configuration can be changed while the game is running.

## Tested

The following Infinity quests have been tested:

- The World
- Beelzebub
- Lucilius
- Bahamut Versa

Full Assist worked in both solo and online sessions.

## How it works

The mod preserves the game's original checks. It only steps in when the game
is about to disable Full Assist, then checks whether the player selected Full
Assist and whether the current quest is an Infinity quest. It identifies
Infinity quests by quest type first. If that type cannot be read, it falls
back to a list of confirmed quests. When the necessary information is
incomplete, the game's original result is left unchanged.

Because quests are identified by their Infinity type, the mod does not depend
on their displayed names and should also work with other Infinity quests added
to a compatible game build.

## Known limitations

This mod only allows the built-in AI to run in Infinity quests. Some Infinity
mechanics require target selection, timely interrupts, or damage checks that
the AI may not complete on its own.

A game update may require the mod to be updated even if the version shown on
the main menu looks similar. Disable the mod in Reloaded-II before updating
the game.

## Troubleshooting

### The mod name is missing from the in-game notification

The notification may show only the Relink Mod Manager. Confirm that the mod is
enabled for the game in Reloaded-II, then check the latest log for
`Infinity Full Assist Unlock`.

### The game exits while loading mods

Check that all requirements are installed at the versions listed above. Old
versions of SigScan or Universal Redirector can prevent the mod from loading.

### Full Assist is still disabled in Infinity

Make sure **Full Assist**, rather than regular Assist mode, is selected in the
game settings. Then enable `DiagnosticLogging`, enter the quest again, and
check the Reloaded-II log.

### The mod stopped working after a game update

This is expected. The mod does not run on an unverified game executable.
Disable it and wait for a compatible release.

Alternatively, you can download the source, update the game-version validation
code, and compile the mod yourself.

## Building from source

The project uses .NET SDK `9.0.316`:

```powershell
dotnet restore
dotnet test .\GBFR.InfinityFullAssist.sln --configuration Release
.\build\Pack-Release.ps1 -Configuration Release
```

The release archive is written to:

```text
artifacts\release
```

## Uninstallation

Disable the mod in Reloaded-II. To remove it completely, delete:

```text
Reloaded-II\Mods\gbfr.qol.infinityfullassist
Reloaded-II\User\Mods\gbfr.qol.infinityfullassist
```

## License

This project is licensed under the
[GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.html).
