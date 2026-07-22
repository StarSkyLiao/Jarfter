---
name: run-xunit-v3-tests
description: 在 Jarfter .NET 10 仓库运行 xUnit v3 与 Microsoft Testing Platform 单元测试. 当需要执行全量测试, Hexagonal 模块测试或 Cobertura 覆盖率采集时使用.
---

# 运行 xUnit v3 测试

从仓库根目录执行. `global.json` 已选择 Microsoft Testing Platform.

## 全量测试

```powershell
dotnet test Jarfter.sln --configuration Release
```

## 六边形模块

```powershell
dotnet test tests/Jarfter.Hexagonal.xUnit/Jarfter.Hexagonal.xUnit.csproj --no-restore --configuration Release
```

## 六边形覆盖率

```powershell
dotnet test tests/Jarfter.Hexagonal.xUnit/Jarfter.Hexagonal.xUnit.csproj --no-restore --configuration Release --coverlet --coverlet-output-format cobertura --results-directory artifacts/coverage
```

先前修改了包引用时, 先执行 `dotnet restore Jarfter.sln`. 覆盖率报告会写入 `artifacts/coverage`.
