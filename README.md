> [!WARNING]
> **本仓库是第三方修改版（非官方）。**
> 它由社区成员基于 PCL 2.99.0.0 的源码与官方二进制独立修改，**与龙腾猫跃（LTCat）及 PCL 官方团队没有任何关系**。
> 官方仓库：<https://github.com/Meloong-Git/PCL> ｜ 官方下载：<https://meloong.com/pcl>
>
> - **修改版程序**：[`Plain Craft Launcher 2（deepseek修改）.exe`](<build/Plain Craft Launcher 2（deepseek修改）.exe>)（5,976,064 字节，SHA-256 见 [build/README.md](build/README.md)）
> - **改了什么**：把 `RestrictMcDownload` 置为 `False`，并恢复被常量折叠删掉的下载源列表、MC 下载卡片、整合包安装分发与安装入口
> - **怎么做的 / 如何复现**：[patching/README.md](patching/README.md)、[patching/PATCH-README.md](patching/PATCH-README.md)
> - **使用前必读**：[免责声明](DISCLAIMER.md)
>
> 本修改版仅供个人学习、研究与技术交流，请支持官方版本。下方是上游仓库的原始 README 内容，版权归原作者所有。

---

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
