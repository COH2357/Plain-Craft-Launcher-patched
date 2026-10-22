# PCL 代码功能研究：常量折叠、IL 结构与调用链还原

本仓库是一份**个人技术研究记录**，研究对象是 [Plain Craft Launcher 2](https://github.com/Meloong-Git/PCL)（PCL）
2.99.0.0 版本的公开源代码与官方发布程序。关注的问题是：

> 当 VB.NET 编译器遇到 `Const RestrictMcDownload As Boolean = True` 这类编译期常量时，
> 它到底删掉了什么、保留了什么？被删掉的分支还能不能在 IL 层面被识别和还原？

研究结论与实验代码在此公开，**本仓库不发布、也不再分发任何可执行构建产物**。
这不是 PCL 官方仓库，与龙腾猫跃（LTCat）及 PCL 官方团队没有任何关系。

## 研究问题与结论

| 问题 | 结论 |
|---|---|
| `Const` 内联后死代码消除的粒度是什么？ | 粒度是**方法体内部的语句**：`If Const Then Throw` 之后的整段代码连同局部变量初始化被移除，但同一类型里**未被调用的方法体不会被删除** |
| 被"删掉"的下载/安装逻辑真的消失了吗？ | 没有。`InstallPackCurseForge` / `InstallPackModrinth` / `InstallPackHMCL` / `InstallPackMMC` / `InstallPackMCBBS` / `InstallPackLauncherPack` / `InstallPackCompress` 七个安装器方法体完整保留，只是失去了调用者 |
| 分支条件被折叠成常量后，IL 还剩什么线索？ | 只剩一条 `ldstr` + `newobj Exception` + `throw`；原先的 `Select Case` 跳转表整体消失，可通过分支目标数量与局部变量残留反推原结构 |
| Lambda 被删除后闭包类型还能复用吗？ | 不能。闭包类会被清成只剩 `.ctor` 的空壳（`ModpackInstall()` 的 `_Closure$__0-0`），必须改用静态字段 + 委托来重建回调 |
| 异常处理结构在 IL 补丁里有什么硬约束？ | `try` 区域的**最后一条指令必须是 `leave` / `throw` / `endfinally`**。把原 `throw` 改成 `nop` 会让控制流"掉进" finally handler，运行时抛 `InvalidProgramException`——这是本次研究踩到的最深的坑 |
| 如何在无 GUI 环境下验证补丁的结构正确性？ | `RuntimeHelpers.PrepareMethod` 可以触发 JIT 编译而不执行方法，配合 `COMPlus_JitNoInline=1` 能有效暴露非法 IL |

## 研究方法

- **静态分析**：用 Mono.Cecil 0.11.5（net40）读取元数据、方法体、局部变量与异常处理表；用自写的 dump 工具输出可读 IL。
- **对照分析**：把二进制 IL 与公开 VB 源码逐句对照，确认"源码里写了什么 → 编译后剩了什么"。
- **重建实验**：在本地副本上按源码语义重建被裁剪的入口与分发，用 IL dump + JIT 校验判断结构是否合法。
- **边界**：所有实验都在本地对官方文件的一次性副本上进行；仓库中只保留工具源码与分析记录。

## 实验代码

`patching/` 目录包含本次研究用到的全部工具与记录：

- [patching/README.md](patching/README.md)：研究环境、分析流程与工具清单
- [patching/PATCH-README.md](patching/PATCH-README.md)：逐条 IL 改造记录（含失败实验与原因）
- `patching/tools/*.cs`：元数据 dump、方法查找、JIT 校验、补丁实验等 C# 工具源码

其中 `PatchExp.cs`、`PatchGC.cs`、`PatchTriv.cs` 等是定位 `InvalidProgramException` 过程中的失败实验，
保留它们是为了记录"哪些假设被证伪"，而非可用的补丁。

## 关于可执行文件

仓库**不提供**任何构建产物、修改版程序或从官方程序中解出的依赖 DLL。
如果你出于研究目的自行复现，请自行获取官方发布文件、自行承担合规责任，并遵守
[LICENCE](LICENCE) 与 [免责声明](DISCLAIMER.md)。

---

<details>
<summary>上游仓库原始 README（版权归原作者，点击展开）</summary>

# Plain Craft Launcher

[![Stars](https://img.shields.io/github/stars/Meloong-Git/PCL?style=flat&logo=data:image/svg%2bxml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZlcnNpb249IjEiIHdpZHRoPSIxNiIgaGVpZ2h0PSIxNiI+PHBhdGggZD0iTTggLjI1YS43NS43NSAwIDAgMSAuNjczLjQxOGwxLjg4MiAzLjgxNSA0LjIxLjYxMmEuNzUuNzUgMCAwIDEgLjQxNiAxLjI3OWwtMy4wNDYgMi45Ny43MTkgNC4xOTJhLjc1MS43NTEgMCAwIDEtMS4wODguNzkxTDggMTIuMzQ3bC0zLjc2NiAxLjk4YS43NS43NSAwIDAgMS0xLjA4OC0uNzlsLjcyLTQuMTk0TC44MTggNi4zNzRhLjc1Ljc1IDAgMCAxIC40MTYtMS4yOGw0LjIxLS42MTFMNy4zMjcuNjY4QS43NS43NSAwIDAgMSA4IC4yNVoiIGZpbGw9IiNlYWM1NGYiLz48L3N2Zz4=&logoSize=auto&label=Stars&labelColor=444444&color=eac54f)](https://github.com/Meloong-Git/PCL/)
[![Issues](https://img.shields.io/github/issues/Meloong-Git/PCL?style=flat&label=Issues&labelColor=444444&color=1F883D)](https://github.com/Meloong-Git/PCL/issues)
[![哔哩哔哩](https://img.shields.io/badge/动态-BiliBili-00A4DB?style=flat&labelColor=444444&logoSize=auto)](https://space.bilibili.com/11343203/dynamic)
[![爱发电](https://img.shields.io/badge/赞助-爱发电-946ce6?style=flat&labelColor=444444&logoSize=auto)](https://meloong.com/afd/a/LTCat)

包含 PCL 的大部分源代码，包括 UI 库、动画模块、下载模块、Minecraft 启动模块等！<br/>
代码并不是实时更新的，它会在每次 PCL 发布更新时同步一次。<br/>
非常感谢社区的大力支持！

<br/>

### 相关资源
- [PCL 下载](https://meloong.com/pcl)：下载可免费使用的正式版 PCL！
- [PCL 功能投票](https://github.com/Meloong-Git/PCL/discussions/2)：来参加投票吧，开发者会优先处理票数较高的项目！
- [帮助文档库](https://github.com/LTCatt/PCL2Help)：PCL 内置帮助文档的存储库

</details>
