# tools/：分析用途源码

本目录中的 `.cs` 是研究过程中写的辅助程序源码，用于**观察与验证**，不是成品构建脚本：

| 类别 | 代表文件 | 作用 |
|---|---|---|
| 元数据 dump | `DumpMethod.cs`、`DumpFull.cs`、`HandlerDump.cs`、`Probe*.cs` | 输出方法体、局部变量、异常处理表 |
| 方法/字符串查找 | `FindMethod.cs`、`Inspector.cs`、`TokenDump.cs` | 按名称或字面量定位类型与方法 |
| JIT 校验 | `JitCheck.cs`、`CtorJit.cs`、`VerifyJit.cs` | 用 `PrepareMethod` 判断 IL 是否可编译 |
| 资源检查 | `ExtractRes.cs`、`ListRes.cs`、`ResCount.cs`、`ResList.cs` | 解出/列出内嵌依赖与资源 |
| 读写与冒烟 | `TestWrite*.cs`、`Smoke.cs` | 验证 Cecil 能否在该程序集上读写 |
| 实验留档 | `PatchExp.cs`、`PatchMin2.cs`、`PatchGC*.cs`、`PatchTriv.cs`、`PatchT.cs`、`PatchDiag.cs` | 定位 `InvalidProgramException` 的失败实验，用于说明哪些假设被证伪 |

约定：

- 编译需 .NET Framework 4.x 的 `csc.exe`，并引用 **net40** 版的 Mono.Cecil。
- 这些源码针对的是单一版本的样本，**不保证**可迁移到其它版本或其它程序。
- 本仓库不附带任何被操作的程序二进制；请勿用本目录内容从事再分发。
