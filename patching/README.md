# 常量折叠与 IL 改造实验记录（patching）

本目录是一份关于 VB.NET **编译期常量、死代码消除与 IL 结构**的研究记录。
研究对象：`Plain Craft Launcher 2`（PCL）2.99.0.0 的公开源码与官方发布程序。
上游：<https://github.com/Meloong-Git/PCL>

> 这不是官方仓库。本目录**不包含**任何构建产物或修改版程序，只做静态分析与可复现的实验记录；
> 所有实验均在本地对官方文件的一次性副本上进行。详见仓库根目录的 [README](../README.md) 与
> [免责声明](../DISCLAIMER.md)。

## 研究对象：被常量折叠裁掉的位置

PCL 用 `Public Const RestrictMcDownload As Boolean = True` 控制下载相关功能是否开放。
由于它是 `Const`，编译器会把值内联到每个使用点并做**常量折叠**，等价于把 `If RestrictMcDownload Then`
直接替换成 `True`，于是分支一侧被判定为不可达并从 IL 中移除。可观察到的裁剪点：

| # | 位置 | 裁剪后的形态 |
|---|---|---|
| 1 | `ModSecret.vb:18` | 源码级常量，`True` / `False` 即“开关”本体 |
| 2 | `PageDownloadInstall::LoaderInit` | 只剩构造“关闭下载”占位 `LoaderTask` 的分支，`Else` 里的 `DlClientListLoader` 被删 |
| 3 | `PageDownloadInstall::LoadInstanceFinished` | 卡片 `Visibility` 恒为 `Collapsed`，没有分支 |
| 4 | `ModModpack::ModpackInstall(String,String,String)` | 卫语句恒真，方法体止于 `ldstr msg; newobj Exception; throw`，先前 `Select Case` 分发与 7 个调用点全部消失 |
| 5 | `ModModpack::ModpackInstall()` | 只剩 `Hint(msg)`；`SelectFile` 与 `RunInThread(Sub() ...)` 连同闭包内容被删，闭包类退化为空壳 |
| 6 | `PageDownloadResourceDetail::InitializeStackResourceVersions` | 只剩挂 `Save_Click` 的分支，整合包行的 `Install_Click` 委托创建被删 |

关键观察：**编译器只删“不可达的语句”，不删“未被引用的方法体”**。因此七 个安装器
（`InstallPackCurseForge` / `InstallPackModrinth` / `InstallPackHMCL` / `InstallPackMMC` /
`InstallPackMCBBS` / `InstallPackLauncherPack` / `InstallPackCompress`）在官方程序里依然完整，
实验只需按源码语义重建丢失的调用点，无需重新实现整合包格式解析。

## 实验方法与关键技术点

1. **元数据读取**：Mono.Cecil 0.11.5（net40）读模块、类型、方法体、局部变量与异常处理表。
   官方程序把依赖（PCLCS、MeloongCore、Newtonsoft.Json、NAudio 等）内嵌在 `PCL.Resources.resources`，
   用 `AssemblyResolve` 动态加载，因此写回时需要自定义 `IAssemblyResolver`（见 `tools/Resolver.cs`），
   否则会抛 `AssemblyResolutionException`。
2. **调用点重建**：新增一个 `switch (packType)` 分发方法，按原 `Select Case` 的参数顺序调用七个安装器；
   泛型/委托/可选参数类型（`OneOf`、`Nullable`、`ValueTuple`、`Action`）直接复用二进制中已有的
   `MethodReference`，避免手工构造带来的签名偏差。
3. **异常结构约束**：改写 `try` 区域时，**最后一条指令必须是 `leave` / `throw` / `endfinally`**。
   早期实验把原 `throw` 改成 `nop`，控制流会落入 finally handler，运行时报
   `InvalidProgramException: 公共语言运行时检测到无效的程序。`——这是整项研究中最容易踩的坑。
4. **闭包重建**：被删的 lambda 对应的闭包类型已退化为空壳，无法复用；改用“静态字段传参 + 静态方法回调 +
   `ldftn`/`Action::.ctor`”替代，语义等价且不依赖闭包。
5. **结构验证**：`RuntimeHelpers.PrepareMethod` 可在不执行方法的前提下触发 JIT，从而在无 GUI 环境中
   判断 IL 是否合法；配合 IL dump 逐条比对分支目标与 EH 表边界。

## 分析流程（原理性描述）

1. **读取元数据**：解析 PE/CLI 头与方法体，列出目标方法的指令、局部变量与异常处理表。
2. **源码对照**：把方法体与公开 VB 源码逐句比对，标出被常量折叠移除的语句、以及残留但不再使用的局部变量。
3. **重建调用点**：按源码语义在被裁剪的位置插入调用或分支，类型与方法引用尽量复用二进制中已有的元数据。
4. **结构校验**：IL dump 检查分支目标与异常处理表边界，再用 `PrepareMethod` 触发 JIT 判断结构是否合法。
5. **差异比对**：把内嵌资源清单、PE 节与官方样本对照，确认改动只落在预期位置。

`tools/` 中的程序分别服务于上述步骤；本仓库不提供偏移表、一键补丁脚本或可直接套用的构建流程。

## 工具清单（`tools/`）

| 文件 | 用途 |
|---|---|
| `PatchCecil.cs` | 分发调用点重建（`$PCLPatch$DispatchModpackInstall`） |
| `PatchFull.cs` | 无参 `ModpackInstall` 重建 + 资源页安装按钮条件分支 |
| `PatchTips.cs` | 移除 ToolTip 的实验（**最终未采用**，仅作记录） |
| `Resolver.cs` | 自定义 Cecil 解析器 |
| `Probe2.cs` / `Probe3.cs` / `Probe4.cs` | IL dump / 方法查找 / 字符串查找 |
| `JitCheck.cs` / `CtorJit.cs` / `VerifyJit.cs` | `PrepareMethod` JIT 校验 |
| `ExtractRes.cs` | 解出内嵌依赖 |
| `DumpMethod.cs` / `DumpFull.cs` / `HandlerDump.cs` / `TokenDump.cs` / `Inspector.cs` / `FindMethod.cs` / `ListRes.cs` / `ResCount.cs` / `ResList.cs` | 各类元数据检查 |
| `PatchExp.cs` / `PatchMin2.cs` / `PatchGC.cs` / `PatchGC2.cs` / `PatchTriv.cs` / `PatchT.cs` / `PatchDiag.cs` | 失败实验留档（用于说明哪些假设被证伪） |
| `TestWrite.cs` / `TestWrite2.cs` / `Smoke.cs` | 读写连通性与冒烟测试 |

C# 工具用 .NET Framework 4.8 的 `csc.exe` 编译，需引用 Mono.Cecil 0.11.5 的 **net40** 版本。

## 验证状态

- IL 结构与 EH 表边界逐条比对；`ModpackInstall`（无参 / 3 参）、分发方法、无参回调方法、
  `InitializeStackResourceVersions`、`LoaderInit`、`LoadInstanceFinished` 均通过 `PrepareMethod` JIT 校验。
- 内嵌资源清单与官方一致。
- **未做**真实 GUI 的端到端操作验证：研究在无交互会话中完成，结论仅代表结构层面正确。

详细逐条记录见 [PATCH-README.md](PATCH-README.md)。
