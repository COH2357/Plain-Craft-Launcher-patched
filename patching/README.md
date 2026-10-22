# PCL 修改版补丁工具（patching）

本目录是 `Plain Craft Launcher 2`（PCL）2.99.0.0 版本的**第三方修改**的完整补丁来源与构建产物。
上游：<https://github.com/Meloong-Git/PCL>

> 本仓库不是官方仓库。修改版仅用于解除 PCL 中因合规原因关闭的 MC 下载/整合包安装功能，
> 遵守上游《PCL 分发有限许可》与《PCL 存储库合理使用指南》中的"重度使用"条款：
> 明确标注第三方修改、名称以 Plain Craft Launcher 开头并带修改后缀、保留 LICENCE、
> 公开源码、保留官方关于页署名与正版购买劝导逻辑、界面主色相保持蓝色。

## 修改内容（相对官方 2.99.0.0）

| # | 位置 | 官方行为 | 修改后 |
|---|---|---|---|
| 1 | `Modules/ModSecret.vb:18` | `RestrictMcDownload = True` | `= False`（源码级修改，已提交） |
| 2 | `PageDownloadInstall::LoaderInit` | 只构造一个"关闭下载"的占位 LoaderTask（正常分支被常量折叠删除） | 改为取 `ModDownload.DlClientListLoader`，下载页恢复源列表 |
| 3 | `PageDownloadInstall::LoadInstanceFinished` | MC 下载卡片 `Visibility = Collapsed` | `= Visible` |
| 4 | `ModModpack::ModpackInstall(String,String,String)` | 卫语句恒真 → 直接 `Throw`，后面的 `Select Case` 分发被删除 | 新增加分发方法 `$PCLPatch$DispatchModpackInstall`，按 `PackType` 调回 7 个安装器 |
| 5 | `ModModpack::ModpackInstall()`（无参） | 只剩 `Hint("为遵守相关法律法规…")`，闭包类被清空 | 重建 `SelectFile` + `RunInThread` 流程（静态字段传参） |
| 6 | `PageDownloadResourceDetail::InitializeStackResourceVersions` | 只挂 `Save_Click`（下载） | 整合包行额外挂 `Install_Click`（安装），条件按源码恢复 |

要点：常量 `Const` 被编译期内联后，编译器把 `If RestrictMcDownload Then Throw` 之后的代码
当作不可达代码删除，因此**安装器本体（InstallPackCurseForge / Modrinth / HMCL / MMC / MCBBS /
LauncherPack / Compress）仍完整存在于官方 exe 中**，补丁只需重建被剪掉的"入口与分发接线"，
不需要重新实现整合包格式解析。详细 IL 记录见 [PATCH-README.md](PATCH-README.md)。

## 产物

- `../build/Plain Craft Launcher 2（deepseek修改）.exe`：最终构建（5,976,064 字节），
  版本信息仅在原值后追加"（deepseek修改）"，其余字段保持官方值。

## 复现步骤

环境：Windows + .NET Framework 4.8 自带的 `csc.exe`、Python 3、[Mono.Cecil 0.11.5 (net40)](https://www.nuget.org/packages/Mono.Cecil/0.11.5)、
`pip install pefile`、[rcedit](https://github.com/electron/rcedit/releases)。

```bat
:: 0) 从官方仓库检出 2.99.0.0，并把 ModSecret.vb 的 RestrictMcDownload 改为 False
:: 1) 原始字节补丁（下载页）
python patch.py "Plain Craft Launcher 2.exe" step1.exe

:: 2) 重建分发（把 Cecil 与 Resolver 一起编译）
csc -nologo -out:PatchCecil.exe -r:Mono.Cecil.dll PatchCecil.cs Resolver.cs
PatchCecil.exe step1.exe patched.exe <从 exe 内嵌资源解出的依赖目录>

:: 3) 重建无参安装与资源页安装按钮
csc -nologo -out:PatchFull.exe -r:Mono.Cecil.dll PatchFull.cs Resolver.cs
PatchFull.exe patched.exe "Plain Craft Launcher 2（deepseek修改）.exe" <依赖目录>

:: 4) 写入版本信息（注意 rcedit 必须用绝对路径）
rcedit.exe "%CD%\Plain Craft Launcher 2（deepseek修改）.exe" ^
  --set-version-string FileDescription "Plain Craft Launcher 启动器（deepseek修改）" ^
  --set-version-string Comments "deepseek 修改版：恢复下载源列表、MC 下载卡片可见性、整合包安装分发与安装入口"
```

依赖目录可由 `tools/ExtractRes.cs` 从官方 exe 的 `PCL.Resources.resources` 中解出
（PCL 在 `Application.xaml.vb` 里以 `AssemblyResolve` 动态加载 PCLCS、MeloongCore、Newtonsoft.Json 等）。
本仓库不附带这些第三方 DLL。

## 工具清单（`tools/`）

| 文件 | 用途 |
|---|---|
| `PatchCecil.cs` | 分发补丁：新增 `$PCLPatch$DispatchModpackInstall` 并改写 3 参 `ModpackInstall` |
| `PatchFull.cs` | 无参 `ModpackInstall` 重建 + 资源页安装按钮条件分支 |
| `PatchTips.cs` | 曾经移除两处 ToolTip 的实验补丁（**最终产物未应用**，已回滚） |
| `Resolver.cs` | Cecil 解析器（优先依赖目录，再回退 `Assembly.Load`） |
| `Probe2.cs` / `Probe3.cs` / `Probe4.cs` | IL dump / 方法查找 / 字符串查找 |
| `JitCheck.cs` / `CtorJit.cs` / `VerifyJit.cs` | 用 `RuntimeHelpers.PrepareMethod` 做 JIT 校验 |
| `ExtractRes.cs` | 从官方 exe 解出内嵌依赖 |
| `DumpMethod.cs` / `DumpFull.cs` / `HandlerDump.cs` / `TokenDump.cs` / `Inspector.cs` / `FindMethod.cs` / `ListRes.cs` / `ResCount.cs` / `ResList.cs` | 各类 IL / 元数据检查小工具 |
| `PatchExp.cs` / `PatchMin2.cs` / `PatchGC.cs` / `PatchGC2.cs` / `PatchTriv.cs` / `PatchT.cs` / `PatchDiag.cs` | 定位 `InvalidProgramException` 过程中的实验代码（保留供参考，不参与最终构建） |
| `TestWrite.cs` / `TestWrite2.cs` / `Smoke.cs` | Cecil 读写连通性 / 冒烟测试 |

所有 C# 工具用 .NET Framework 4.8 的 `csc.exe` 编译，需引用 Mono.Cecil 0.11.5 的 **net40** 版本
（netstandard 版本在 .NET Framework 下不可用）。

## 验证状态

- IL dump 结构逐条比对；六处补丁均已确认出现在最终产物中。
- `RuntimeHelpers.PrepareMethod`：`ModpackInstall`（无参 / 3 参）、`$PCLPatch$DispatchModpackInstall`、
  `$PCLPatch$InstallPendingModpack`、`InitializeStackResourceVersions`、`LoaderInit`、
  `LoadInstanceFinished` 全部 JIT 通过。
- 内嵌资源（`Plain Craft Launcher 2.g.resources`、`PCL.Resources.resources`）与官方一致。
- **未做**真实 GUI 的端到端安装验证：构建在无交互会话中完成，请在实机点击确认。
