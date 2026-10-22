# PCL 2.99.0.0 补丁说明（二进制改造）

原始文件：仓库根目录 `Plain Craft Launcher 2.exe`（5,980,160 字节，官方正式版 2.99.0.0）。
产物：`../build/Plain Craft Launcher 2（deepseek修改）.exe`（5,976,064 字节，Cecil 重写 + 版本信息后布局有位移，属正常）。

## 改动内容

1. `PCL.PageDownloadInstall::LoaderInit`
   原版在 RestrictMcDownload 为 True 时把“下载源列表加载器”整段删掉，改为新建一个必抛异常的占位任务。
   补丁：把占位任务的构造替换为 `ldsfld PCL.ModDownload::DlClientListLoader`（token 0x0400040F），
   即直接复用真正的加载器（Bmclapi 优先、Mojang 兜底，Main 由 ModDownload 静态构造初始化）。

2. `PCL.PageDownloadModify::LoadInstanceFinished`
   原版把 MC 下载卡片的可见性编译成了 `Collapsed`（ldc.i4.2）。
   补丁：改为 `ldc.i4.0`（Visible），恢复“实例修改页 → 安装 MC”。

3. `PCL.ModModpack::ModpackInstall(String, String, String)`（本次新增）
   原版在 `If RestrictMcDownload Then Throw ...` 之后的分发分支被整体删掉，
   所有格式识别完成后统一落到 `Throw New Exception("为遵守相关法律法规……")`。
   补丁：
   - 新增私有静态方法 `$PCLPatch$DispatchModpackInstall(String file, ZipArchive archive, String baseFolder,
     String instanceName, String logo, Int32 packType)`，按 packType 分发：
     0→InstallPackCurseForge、1→InstallPackHMCL、2→InstallPackMMC、3→InstallPackMCBBS、
     4→InstallPackModrinth、9→InstallPackLauncherPack、默认→InstallPackCompress。
   - 在 `ModpackInstall` 的汇合点（原 throw 处）改为：压入 6 个实参 → 调用分发方法 → `stloc.0` → `leave`
     到方法末尾新增的 `ldloc.0; ret`；原来的 `newobj Exception` 改为 `leave`，
     原来的 `throw` 改为 `leave`（保持 try 块以 leave 结尾，避免顺序执行“掉进” finally 处理器——
     这正是最初 JIT 报 InvalidProgramException 的原因）。

   效果：整合包页（社区资源 → 整合包）从搜索结果点击“安装”时，按魔改包类型正常进入安装流程。

## 再次恢复的部分（v3，按源码逻辑重建）

4. `PCL.ModModpack::ModpackInstall()`（无参，"安装已有整合包"）
   原版把整段 `Dialogs.SelectFile` + `RunInThread` 折叠成空 lambda + 提示框，连闭包类都是空的。
   补丁按源码 `ModModpack.vb:10-26` 重建 IL：
   - 复用二进制中仍存在的 `Dialogs.SelectFile(String, Boolean, String, Nullable(Of OneOf(Of IEnumerable(Of (String(), String)), String)))`
     引用，用 `op_Implicit` + `Nullable` 构造 `{("rar","zip","mrpack"), "整合包文件"}` 筛选器；
   - `Where(File <> "")` 用 `String.IsNullOrEmpty` 取反实现；
   - 新增静态字段 `$PCLPatch$PendingModpack` 与静态方法 `$PCLPatch$InstallPendingModpack()`（`ModpackInstall(file, Nothing, Nothing)`
     外包 try/catch：`ThrowIfCanceled` + `Logger.Error(..., "手动安装整合包失败", LogBehavior.Alert, "ModModpack.vb")`）；
   - 正文改为 `SelectFile -> FirstOrDefault -> 判空 -> stsfld -> new Action(ldftn 静态方法) -> ModBase.RunInThread`。
   因为编译期把闭包类删空了，这里用"静态字段 + 无捕获委托"绕开闭包，不需要重建 lambda 类。

5. `PCL.PageDownloadResourceDetail::InitializeStackResourceVersions`（资源详情页版本列表）
   原版丢弃了 `If Not RestrictMcDownload AndAlso TargetResourceType = ResourceTypes.ModPack` 分支，
   所有条目都只挂 `Save_Click`。补丁按源码 `PageDownloadCompDetail.xaml.vb:274-279` 重建条件：
   - `TargetResourceType = 2`（ModPack）时调用 3 参 `ToListItem(Install_Click, Save_Click, IsBadDisplay)`；
   - 其它类型保持 `ToListItem(Save_Click, Nothing, IsBadDisplay)`。
   这样整合包详情页的"安装"入口恢复，其它资源页不受影响。

6. （已回滚，未应用）曾尝试移除两处整合包相关 ToolTip，后确认官方原版本就有这些提示，
   为保持一致已撤掉该补丁，最终产物中这两处提示与官方一致。

## 未恢复的部分
无功能性缺口（下载源列表、MC 下载卡片、整合包安装分发、安装入口均已恢复）。
仅有 `PCL.FormMain` 拖拽安装函数里 `If RestrictMcDownload AndAlso ex.Message = "为遵守相关法律法规……"` 的
特判残留：由于分发已恢复，`ModpackInstall` 不再抛这条消息，该特判不会触发，因此未改。

## 验证情况

- IL 结构 dump 正确；五处补丁均按预期出现在最终产物中（ToolTip 补丁已回滚，不计入）。
- `RuntimeHelpers.PrepareMethod` 对 `ModpackInstall`（无参/3 参）、`$PCLPatch$DispatchModpackInstall`、
  `$PCLPatch$InstallPendingModpack`、`InitializeStackResourceVersions` 全部编译通过（JIT OK）。
- 内嵌资源（`Plain Craft Launcher 2.g.resources`、`PCL.Resources.resources`）与原版一致；
  版本信息只在原值后追加"（deepseek修改）"，其余字段保持官方值。
- 未在真实 GUI 中跑通端到端安装（当前会话无法可靠启动/操作 GUI），请在实机点击验证。

## 工具与中间产物

- `patch.py`：第一阶段的原始字节补丁（生成 step1.exe）。
- `tools/PatchCecil.cs`：第二阶段 Cecil 补丁（整合包分发表 `$PCLPatch$DispatchModpackInstall`）。
- `tools/PatchFull.cs`：第三阶段 Cecil 补丁（无参 `ModpackInstall` 重建 + 资源页安装按钮条件分支）。
- `tools/PatchTips.cs`：ToolTip 补丁的实验留档（**未应用到最终产物**，已回滚）。
- 两个 C# 补丁都用系统自带 `csc.exe`（.NET Framework 4.8）编译，引用 Mono.Cecil 0.11.5 的 net40 版本。
- `tools/` 下其余 .cs 为 IL dump / JIT 校验 / 方法查找等辅助工具（部分为踩坑过程留档，不参与最终构建）。
- 最终版本信息用 `rcedit.exe` 写入（**必须传绝对路径**，相对路径会报 `Unable to commit changes`）。
