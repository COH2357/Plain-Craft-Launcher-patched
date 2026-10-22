# 修改版构建产物

本目录只存放 **第三方修改版** 的可执行文件，**不是** PCL 官方发布渠道。

- 文件：`Plain Craft Launcher 2（deepseek修改）.exe`
- 大小：5,976,064 字节
- SHA-256：`48f241fc70717c004a1d4b094601152a96329aa0fd0ce275cd7facbe94d51990`
- 基于：PCL 2.99.0.0 正式版（官方仓库 <https://github.com/Meloong-Git/PCL> 的 `2.99.0.0` tag）
- 版本信息：`FileDescription` 为 `Plain Craft Launcher 启动器（deepseek修改）`，其余元数据保持官方值

## 改动

恢复官方因合规原因关闭的功能：

1. 下载页的下载源列表（`DlClientListLoader`）；
2. MC 下载卡片显示（原为 `Collapsed`）；
3. 整合包安装分发（原 `Select Case` 被常量折叠删除，已按源码重建）；
4. "安装已有整合包"按钮（无参 `ModpackInstall` 重建）；
5. 资源详情页整合包行的"安装"入口。

源码级改动为 `Plain Craft Launcher 2/Modules/ModSecret.vb` 中 `RestrictMcDownload = False`。
技术细节与复现步骤见 [../patching/README.md](../patching/README.md)。

## 注意

- **非官方修改版**，与龙腾猫跃（LTCat）及 PCL 官方团队无关，请勿向官方反馈本文件的问题。
- 使用前请阅读 [../DISCLAIMER.md](../DISCLAIMER.md)。
- 校验哈希后再运行；若哈希不一致，请勿使用。
