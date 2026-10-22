# Plain Craft Launcher 修改版（PCL-patched）

> [!WARNING]
> **本仓库是第三方修改版，不是官方仓库。**
> 它由社区成员基于 PCL 2.99.0.0 的公开源码与官方二进制独立修改，**与龙腾猫跃（LTCat）及 PCL 官方团队没有任何关系**，
> 未经官方授权或背书。请勿将本仓库误认为官方渠道，也不要向官方反馈这里的问题。
>
> 官方仓库：<https://github.com/Meloong-Git/PCL> ｜ 官方下载：<https://meloong.com/pcl>
>
> 本修改版仅供个人学习、研究与技术交流，请支持官方版本；使用前请阅读 [免责声明](DISCLAIMER.md)。

## 这是什么

PCL 2.99.0.0 正式版出于合规原因关闭了 MC 下载与整合包安装相关功能。本仓库是该版本的**第三方修改版**：

- 在源码层面把 `Plain Craft Launcher 2/Modules/ModSecret.vb` 里的 `RestrictMcDownload` 改为 `False`；
- 由于它是编译期常量，官方二进制里被常量折叠删掉的分支，通过 IL 层补丁按上游源码逻辑重建；
- 所有补丁源码、IL 记录与复现步骤一并公开，便于审计。

## 修改内容

| # | 位置 | 官方行为 | 修改后 |
|---|---|---|---|
| 1 | `ModSecret.vb:18` | `RestrictMcDownload = True` | `= False`（源码级） |
| 2 | `PageDownloadInstall::LoaderInit` | 只生成"关闭下载"的占位任务 | 改用 `ModDownload.DlClientListLoader`，恢复下载源列表 |
| 3 | `PageDownloadInstall::LoadInstanceFinished` | MC 下载卡片 `Collapsed` | `Visible` |
| 4 | `ModModpack::ModpackInstall(String,String,String)` | 卫语句恒真，直接 `Throw` | 重建 `Select Case` 分发，调回 7 个安装器 |
| 5 | `ModModpack::ModpackInstall()` | 只剩合规提示 | 重建 `SelectFile` + `RunInThread` 流程 |
| 6 | `PageDownloadResourceDetail::InitializeStackResourceVersions` | 只挂下载按钮 | 整合包行额外挂"安装"按钮 |

安装器本体（CurseForge / Modrinth / HMCL / MMC / MCBBS / LauncherPack / Compress）本就完整保留在官方 exe 中，
补丁只重建了被剪掉的入口与分发接线，未重新实现整合包格式解析。

## 下载构建产物

- 文件：[`build/Plain Craft Launcher 2（deepseek修改）.exe`](<build/Plain Craft Launcher 2（deepseek修改）.exe>)
- 大小：5,976,064 字节
- SHA-256：`48f241fc70717c004a1d4b094601152a96329aa0fd0ce275cd7facbe94d51990`
- 校验值与版本信息说明见 [build/README.md](build/README.md)

## 如何复现

补丁工具、构建步骤与逐条 IL 说明在 [patching/README.md](patching/README.md)；
改造过程的详细记录在 [patching/PATCH-README.md](patching/PATCH-README.md)。

## 免责与许可

- [免责声明（DISCLAIMER.md）](DISCLAIMER.md)：非官方、无担保、使用风险自负、合法使用与下架承诺。
- 本仓库遵循上游 [LICENCE](LICENCE)（《PCL 分发有限许可》）中"重度使用"的要求：标注第三方修改、
  名称以 `Plain Craft Launcher (PCL)` 开头并带修改后缀、保留 LICENCE、公开源码、
  保留官方关于页署名与正版购买劝导逻辑、界面主色相保持蓝色。

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
