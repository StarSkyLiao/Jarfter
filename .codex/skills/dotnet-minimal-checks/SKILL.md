---
name: dotnet-minimal-checks
description: 提供 .NET 项目的最小构建与测试命令. 当需要快速执行 build, test, 或针对特定用例过滤测试时使用.
---

# dotnet 最小检查

## 最小命令集

```powershell
# 解决方案构建
dotnet build Jarfter.sln -v minimal

# 解决方案测试
dotnet test Jarfter.sln -v minimal

# 仅运行 xUnit 项目
dotnet test Jarfter.xUnit/Jarfter.xUnit.csproj -v minimal
```

## 执行顺序

1. 先 `dotnet build` 确认编译通过.
2. 再 `dotnet test` 执行回归.
3. 仅排查局部问题时, 使用 `--filter` 缩小范围.

## 输出要求

1. 仅汇报关键结果, 包含失败测试名称与错误摘要.
2. 出现失败时, 给出可复现命令.
