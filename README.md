# Infinity Assist Unlock

**English** | [简体中文](README.zh-CN.md)

This is a Reloaded-II mod for *Granblue Fantasy: Relink – Endless Ragnarok*.
It removes the restrictions that normally disable Assist Mode and Full Assist Mode
in Infinity quests, allowing the game's built-in AI to continue fighting.
Assist Mode must be enabled separately in the mod settings.

The mod does not change the AI's behavior or handle special Infinity
mechanics.

## Features

- Applies only to Infinity quests.
- Assist Mode can be enabled separately; Full Assist Mode is available by
  default.
- Works in solo and online sessions for both Hosts and Guests by default.
  Online-session support can be disabled in the mod settings.
- Does not affect other difficulties or Assist modes.
- Does not modify damage, rewards, drops, save data, or network state.

## Supported version

The current release supports Endless Ragnarok with `2.0.2` shown on the
game's main menu. Development and in-game testing used this executable:

```text
ApplicationVersion: 2.0.2
SHA-256: 63340832BCF731FBC97796F686B05C988418E83D451D4A49B2244A85D00E297F
```

The SHA-256 identifies the exact build that was verified, but it is not an
allowlist. Another Endless Ragnarok 2.0.2 executable can run the mod when each
required signature has exactly one match and the surrounding instruction
structure passes validation. Missing, duplicate, or incompatible signatures
leave the game's original behavior unchanged.

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
3. Drag `Infinity-Assist-Unlock-1.2.1.zip` into Reloaded-II.
4. Enable **Infinity Assist Unlock** for the game.
5. Launch the game through Reloaded-II.
6. Set Assist Mode to **Assist Mode** or **Full Assist Mode** before
   entering an Infinity quest.

For a manual installation, extract the archive contents to:

```text
Reloaded-II\Mods\gbfr.qol.infinityfullassist
```

## Configuration

Select **Infinity Assist Unlock** in Reloaded-II and choose
**Configure Mod** to change these settings without launching the game.
Reloaded-II creates or updates `Config.json` in the user configuration
directory when the settings are saved.

```json
{
  "Enabled": true,
  "EnableAssistMode": false,
  "EnableOnlineSessions": true,
  "DiagnosticLogging": false
}
```

- `Enabled`: turns the mod on or off.
- `EnableAssistMode`: also allows Assist Mode. It is off by default
  and does not affect Full Assist Mode.
- `EnableOnlineSessions`: allows the mod to work in online Infinity quests for
  both Hosts and Guests. It is on by default. Turn it off to limit the mod to
  solo quests.
- `DiagnosticLogging`: writes diagnostic information to the Reloaded-II log.
  Enable it only when troubleshooting.

You can also edit `Config.json` directly. Changes saved while the game is
running are reloaded automatically.

## Tested

The following Infinity quests have been tested:

- The World
- Beelzebub
- Lucilius
- Bahamut Versa

Assist Mode has been confirmed in game. With online-session support enabled,
Full Assist Mode worked in both solo and online sessions.

## How it works

The mod preserves the game's original checks. It only steps in when the game
is about to disable Assist Mode or Full Assist Mode, then checks which mode
the player selected, whether `EnableAssistMode` is turned on when
needed, whether the current quest is an Infinity quest, and whether the
current session is allowed by `EnableOnlineSessions`. It identifies Infinity
quests by quest type first. If that type cannot be read, it falls back to a
list of confirmed quests. When the necessary information is incomplete, the
game's original result is left unchanged.

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
`Infinity Assist Unlock`.

### The game exits while loading mods

Check that all requirements are installed at the versions listed above. Old
versions of SigScan or Universal Redirector can prevent the mod from loading.

### An Assist mode is still disabled in Infinity

For Assist Mode, also turn on `EnableAssistMode` in the mod settings. For Full
Assist Mode, make sure **Full Assist Mode** is selected in the game settings.
If the quest is online, also make sure `EnableOnlineSessions` is turned on.
You can then enable `DiagnosticLogging`, enter the quest again, and check the
Reloaded-II log.

### The mod stopped working after a game update

If the displayed application version has changed, the mod remains inactive.
For a different 2.0.2 executable, check the Reloaded-II log: a SHA-256 warning
is informational, while a missing, duplicate, or incompatible required
signature prevents the Hook from being installed. Disable the mod and wait
for a compatible release if runtime validation fails.

Alternatively, you can download the source, update the game-version validation
and runtime signatures, then compile the mod yourself.

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
