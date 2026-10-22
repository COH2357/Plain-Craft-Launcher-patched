# 常量折叠与 IL 结构改动记录（分析笔记）

> 本文件是**分析结论记录**，不是操作指南。为降低可被直接套用的程度，这里不列出
> 方法偏移、元数据 token、字节值等具体参数；相关工具源码仅用于说明验证方法。

## 样本

- 对象：官方 PCL 2.99.0.0 发布程序（单个 exe，约 5.9 MB，无强名称、无 Authenticode 签名）。
- 手段：Mono.Cecil 读取元数据、方法体、局部变量与异常处理表，与公开 VB 源码逐句对照。

## 观察与改动思路（原理级）

### 1. `PageDownloadInstall::LoaderInit`

- **观察**：`If RestrictMcDownload` 一侧保留，`Else` 一侧的加载器构造被整体折叠，只剩“新建一个必抛异常的占位任务”。
- **思路**：把该位置改为读取类型中**已存在**的加载器静态字段——等价于源码里“不进入占位分支、直接使用真实加载器”。
  该字段在 `ModDownload` 的静态构造中初始化，因此无需额外初始化代码。
- **推导线索**：占位任务构造之后紧接 `throw`，且方法体尾部没有任何加载器引用；静态字段在其它页面仍被引用（否则早已 NRE）。

### 2. `PageDownloadModify::LoadInstanceFinished`

- **观察**：卡片 `Visibility` 被折叠成常量，取值对应 `Collapsed`，之后没有任何分支。
- **思路**：把该常量改为 `Visible` 对应取值。
- **推导线索**：赋值前紧邻 `ldfld` 取卡片字段，调用的是 `set_Visibility`；源码中此处正是三元选择。

### 3. `ModModpack::ModpackInstall(String, String, String)`

- **观察**：格式识别（`mcbbs.packmeta`、`mmc-pack.json`、`modrinth.index.json`、`manifest.json`、`modpack.json`、
  `modpack.zip/mrpack`、默认压缩包）**完整保留**，但所有分支在汇合点统一落到
  `ldstr 提示语; newobj Exception; throw`；局部变量表里仍留着 `PackType`、`Archive` 等变量，说明原结构是 `Select Case`。
- **思路**：在汇合点插入“压入 6 个实参 → 调用分发方法 → 存回返回值 → `leave` 到方法末尾新增的返回指令”。
  分发方法按 `packType` 映射到七个仍然完整的安装器（CurseForge / HMCL / MMC / MCBBS / Modrinth / LauncherPack / Compress）。
- **关键约束**：`try` 区域的**最后一条指令必须是 `leave` / `throw` / `endfinally`**。
  最初把原 `throw` 改成 `nop`，控制流会顺序落入 finally 处理器，运行时报 `InvalidProgramException`；
  正确做法是把旧的 `newobj` 与 `throw` 都改写为 `leave`，并把 `leave` 目标放在 finally 之后。

### 4. `ModModpack::ModpackInstall()`（无参）

- **观察**：`Dialogs.SelectFile` + `RunInThread(Sub() ...)` 整段被折叠，连闭包类型都退化成只有构造函数的空壳。
- **思路**：用“静态字段传参 + 无捕获静态方法 + `ldftn` 构造委托”替代闭包，语义等价：
  选择文件 → 判空 → 写入静态字段 → `RunInThread`；回调内 `try` 调用三参版本，异常走
  `ThrowIfCanceled` + `Logger.Error`。筛选器按源码重建为 `{("rar","zip","mrpack"), "整合包文件"}`，
  复用二进制中已有的 `SelectFile` 重载与 `OneOf` / `Nullable` 构造引用，避免手工构造签名。

### 5. `PageDownloadResourceDetail::InitializeStackResourceVersions`

- **观察**：版本行一律只挂“另存为”回调，源码中的
  `If Not RestrictMcDownload AndAlso TargetResourceType = ModPack Then ... Install_Click ...` 分支被丢弃。
- **思路**：对 `ModPack` 类型选择三参 `ToListItem` 重载并挂上安装回调，其余类型保持原有两参调用。

## 未处理项

`FormMain` 拖拽安装里的 `If RestrictMcDownload AndAlso ex.Message = "..."` 特判仍然存在；
由于分发已恢复，`ModpackInstall` 不再抛这条消息，该特判不会触发，因此未改动。

## 验证方法

- **结构比对**：IL dump 逐条检查分支目标、局部变量与异常处理表边界。
- **JIT 校验**：`RuntimeHelpers.PrepareMethod` 可在不执行方法的前提下触发编译，从而在无 GUI 环境中判断 IL 是否合法；
  需要被引用的依赖可被解析，否则会以 `FileNotFoundException` 掩盖真正的 IL 错误。
- **资源比对**：内嵌资源清单、PE 节与版本资源与官方样本对照。
- **未做**：真实 GUI 的端到端点击验证（分析在无交互会话中完成），结论仅代表结构层面正确。

## 工具说明

`tools/` 下为分析用途的源码：元数据 dump、方法查找、JIT 校验、内嵌资源解出、以及若干**失败实验留档**
（`PatchExp`、`PatchGC`、`PatchTriv` 等，用于记录哪些假设被证伪）。它们不是成品构建脚本，
也不保证对其它版本可用。
