# Infinity Assist Unlock

[English](README.md) | **简体中文**

这是一个适用于《Granblue Fantasy: Relink – Endless Ragnarok》的 Reloaded-II 模组。这个模组会移除游戏原本在 Infinity 副本中禁用战斗辅助模式和战斗托管模式的限制，使游戏自带的 AI 可以继续接管战斗。战斗辅助模式需要在模组设置中单独开启。

模组不会改变 AI 的行为，也不会处理 Infinity 的特殊机制。

## 功能

- 只对 Infinity 副本生效。
- 战斗辅助模式可以单独开启，战斗托管模式默认可用。
- 默认支持单人和联机，Host 与 Guest 均可使用，也可以在模组设置中关闭联机支持。
- 不影响其他难度或其他辅助模式。
- 不修改伤害、奖励、掉落、存档或网络状态。

## 支持版本

目前只支持游戏主界面显示为 `2.0.2` 的版本：

```text
ApplicationVersion: 2.0.2
SHA-256: 63340832BCF731FBC97796F686B05C988418E83D451D4A49B2244A85D00E297F
```

模组会检查游戏主程序。版本或文件不匹配时不会加载修改，以免游戏更新后因地址变化而崩溃。

## 前置要求

- [Reloaded-II](https://github.com/Reloaded-Project/Reloaded-II) `1.30.2`
  或更高版本
- [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Granblue Fantasy Relink Mod Manager](https://github.com/WistfulHopes/gbfrelink.utility.manager)
  `2.0.1`
- Reloaded.Memory.SigScan.ReloadedII `1.2.14`
- Reloaded Shared Lib Hooks `1.16.3`
- Reloaded Universal Redirector `1.2.9`

通过 Reloaded-II 导入模组时，它通常会自动安装缺少的依赖。

## 安装

1. 在 Reloaded-II 中添加 `granblue_fantasy_relink.exe`。
2. 安装并启用 Granblue Fantasy Relink Mod Manager。
3. 将 `Infinity-Assist-Unlock-1.2.0.zip` 拖入 Reloaded-II。
4. 为游戏启用 **Infinity Assist Unlock**。
5. 通过 Reloaded-II 启动游戏。
6. 在进入 Infinity 副本前，将游戏的辅助模式设为“战斗辅助模式”或“战斗托管模式”。

手动安装时，将压缩包内容解压到：

```text
Reloaded-II\Mods\gbfr.qol.infinityfullassist
```

## 配置

在 Reloaded-II 的模组列表中选中 **Infinity Assist Unlock**，点击
“Mod 配置”即可修改设置，不需要先启动游戏。保存后，Reloaded-II 会在用户配置
目录中创建或更新 `Config.json`。

```json
{
  "Enabled": true,
  "EnableAssistMode": false,
  "EnableOnlineSessions": true,
  "DiagnosticLogging": false
}
```

- `Enabled`：开启或关闭模组功能。
- `EnableAssistMode`：同时允许战斗辅助模式。默认关闭，战斗托管模式不受这个设置影响。
- `EnableOnlineSessions`：允许模组在联机 Infinity 副本中生效，Host 与 Guest 均适用。默认开启；关闭后模组只在单人副本中生效。
- `DiagnosticLogging`：在 Reloaded-II 日志中记录诊断信息。只有排查问题时才需要开启。

也可以直接编辑 `Config.json`。游戏运行期间保存配置后，模组会自动重新加载。

## 已验证

以下 Infinity 副本已经完成实际测试：

- The World
- Beelzebub
- Lucilius
- Bahamut Versa

战斗辅助模式已经完成游戏内测试。开启联机支持时，战斗托管模式在单人和联机模式下都可以正常启用。

## 工作原理

模组先保留游戏原本的判断逻辑。只有游戏准备禁用战斗辅助模式或战斗托管模式时，它才检查玩家选择的模式、是否已经按需开启 `EnableAssistMode`、当前副本是否属于 Infinity，以及 `EnableOnlineSessions` 是否允许当前会话。识别 Infinity 时优先使用副本类型；类型无法读取时，再查询已经确认的副本列表。任何信息不完整的情况都会保持游戏原本的结果。

模组按 Infinity 类型识别副本，因此如果后续版本更新了其他 Infinity 副本时，模组不需要依赖显示名称，也应该能够正常生效。

## 已知限制

这个模组只是允许原生 AI 在 Infinity 中运行。Infinity 的部分机制需要指定目标、及时打断或满足伤害检测，游戏 AI 不一定能够独立完成。

游戏更新后，即使主界面仍显示相近的版本号，也可能需要重新适配。更新游戏前建议先在 Reloaded-II 中停用模组。

## 故障排查

### 游戏内没有显示模组名称

右下角通知有时只显示 Relink Mod Manager。请在 Reloaded-II 中确认模组已经为游戏启用，并检查最新日志中是否出现 `Infinity Assist Unlock`。

### 游戏在加载模组时退出

检查前置依赖是否完整、版本是否符合要求。旧版 SigScan 或 Universal Redirector 可能导致加载失败。

### Infinity 中仍然无法使用托管模式

使用战斗辅助模式时，还需要在模组设置中打开 `EnableAssistMode`。使用战斗托管模式时，确认游戏设置中已经选择“战斗托管模式”。如果是联机副本，还需要确认 `EnableOnlineSessions` 已开启。随后可以打开 `DiagnosticLogging`，重新进入一次副本并查看 Reloaded-II 日志。

### 游戏更新后模组停止工作

这是预期行为。模组不会在未经验证的游戏主程序上继续运行，请停用模组并等待兼容版本。

或者，你也可以选择下载源码，修改关于游戏版本字段检验的代码，然后自行编译。

## 从源码构建

项目使用 .NET SDK `9.0.316`：

```powershell
dotnet restore
dotnet test .\GBFR.InfinityFullAssist.sln --configuration Release
.\build\Pack-Release.ps1 -Configuration Release
```

发布包会生成在：

```text
artifacts\release
```

## 卸载

在 Reloaded-II 中停用模组即可。需要彻底删除时，移除：

```text
Reloaded-II\Mods\gbfr.qol.infinityfullassist
Reloaded-II\User\Mods\gbfr.qol.infinityfullassist
```

## 许可证

本项目使用
[GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.html)。
