# Infinity Full Assist Unlock

**English** | [简体中文](README.zh-CN.md)

Infinity Full Assist Unlock is a Reloaded-II mod for *Granblue Fantasy:
Relink – Endless Ragnarok*. Infinity quests normally disable Full Assist;
this mod removes that restriction and lets the game's built-in AI continue
fighting.

It does not change the AI or add scripts for Infinity mechanics.

## Features

- Applies only to Infinity quests.
- Requires Full Assist to be selected in the game settings.
- Works in solo and online sessions for both Hosts and Guests.
- Leaves other quest difficulties and Assist modes unchanged.
- Does not modify damage, rewards, drops, save data, or network state.

## Supported version

The current release supports the game version shown as `2.0.2` on the main
menu:

```text
ApplicationVersion: 2.0.2
SHA-256: 63340832BCF731FBC97796F686B05C988418E83D451D4A49B2244A85D00E297F
```

The mod checks the game executable before making any changes. If the version
or executable does not match, it stays inactive rather than risk a crash
after a game update.

## Requirements

- [Reloaded-II](https://github.com/Reloaded-Project/Reloaded-II) `1.30.2` or
  newer
- [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Granblue Fantasy Relink Mod Manager](https://github.com/WistfulHopes/gbfrelink.utility.manager)
  `2.0.1`
- Reloaded.Memory.SigScan.ReloadedII `1.2.14`
- Reloaded Shared Lib Hooks `1.16.3`
- Reloaded Universal Redirector `1.2.9`

Reloaded-II will normally install missing dependencies when the mod archive is
imported.

## Installation

1. Add `granblue_fantasy_relink.exe` to Reloaded-II.
2. Install and enable Granblue Fantasy Relink Mod Manager.
3. Drag `Infinity-Full-Assist-Unlock-1.0.0.zip` into Reloaded-II.
4. Enable **Infinity Full Assist Unlock** for the game.
5. Launch the game through Reloaded-II.
6. Select **Full Assist** in the game settings before entering an Infinity
   quest.

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
- `DiagnosticLogging`: writes additional information to the Reloaded-II log.
  Leave it off unless you are troubleshooting.

Configuration changes are reloaded while the game is running.

## Tested quests

The mod has been tested in:

- The World
- Beelzebub
- Lucilius
- Bahamut Versa

Full Assist was confirmed in both solo and online sessions. Quests are
identified by their Infinity type rather than their displayed name, so the
mod is not tied to this four-name list.

## How it works

The mod keeps the game's original decision unless Full Assist is about to be
disabled. It then checks whether the player selected Full Assist and whether
the current quest is an Infinity quest. Quest type is used first, with a list
of confirmed quests as a fallback when the type cannot be read. If the needed
information is unavailable, the original game behavior is left unchanged.

## Limitations

This mod only allows the built-in AI to run in Infinity quests. Some Infinity
mechanics require target selection, interrupts, or damage checks that the AI
may not complete on its own.

A game update can require a new compatible release even if the visible
version number looks similar. Disable the mod before updating the game.

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
game settings. If the problem remains, enable `DiagnosticLogging`, enter the
quest once, and inspect the Reloaded-II log.

### The mod stopped working after a game update

This is expected on an unverified executable. Disable the mod and wait for a
compatible release.

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
