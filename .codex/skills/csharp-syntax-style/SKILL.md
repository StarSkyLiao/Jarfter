---
name: csharp-syntax-style
description: 统一 C# 语法样式. 当新增或修改 C# 代码, 审查 C# 风格.
---

# C# 语法样式规范

## 执行流程

1. 先读取本次涉及的 `.cs` 文件和相邻同类文件, 以现有局部风格为准.
2. 只修正当前任务相关代码, 不做无关格式化和跨模块重排.
3. 涉及中文文本写回时, 优先使用 Python 等工具而非 Powershell 操作, 文件要求 UTF-8 无 BOM 格式.

## 核心规则

- `new()` 表达式必须写出目标类型, 例如 `new MyClass()`.
- 集合初始化优先使用集合表达式, 例如 `[]`, `[1, 2]`, `[..source]`.
- 集合表达式不得改变容量, comparer, 枚举时机, 引用复用或线程安全语义. 需要指定容量, object initializer, 或特定构造函数行为时保留显式构造.
- 属性可行时优先使用 `field` 隐式字段, 例如 `Property => field ??= CreateValue();`.
- 不因为样式调整改变异常类型, nullability, public API 行为或性能路径.

## 命名

- 局部变量和参数使用小驼峰, 例如 `currentIndex`, `cancellationToken`.
- 私有实例字段使用 `m_` 前缀加大驼峰, 例如 `m_Buffer`, `m_Collectors`.
- 私有静态字段使用 `s_` 前缀加大驼峰, 例如 `s_DefaultRunner`, `s_EmptyArray`.
- 私有常量不加前缀, 使用大驼峰, 例如 `MaxCalibrationAttempts`.
- 其他成员全部使用大驼峰, 包含类型, 方法, 属性, 事件, 委托, 枚举成员, 常量, 非私有字段.
- 类型参数使用 `T` 前缀加大驼峰, 例如 `T`, `TIn`, `TOut`.
- 接口名称使用 `I` 前缀加大驼峰, 例如 `INumber`, `ISerializable`.
- 异步的方法名称使用大驼峰加 `Async` 后缀, 例如 `GetStringAsync`, `SendStringAsync`.
- 自定义Attribute类型的名称使用大驼峰加 `Attribute` 后缀, 例如 `SerializableAttribute`, 但是使用时 `Attribute` 后缀 应当省略, 例如 `SerializableAttribute` 应该写为 `[Serializable]`.
- 源生成器或字符串模板中的被测代码片段可以保留其自身命名风格.

## 文件和类型

- 使用文件范围命名空间, `using` 放在文件顶部.
- partial 类型可以按行为拆分文件, 文件名使用主类型加功能后缀, 例如 `LinkedArray.ICollection.cs`.
- 一个文件优先承载一个主要类型或一个 partial 分片. 私有嵌套类型保留在使用它的类型内部.
- 可继承性不需要开放时, class 优先 `sealed`.
- 只承载不可变数据的轻量结果类型可用 `readonly record struct` 或 `sealed record`.
- 扩展方法（C# extension block）, receiver 的命名为 `self`.

## 表达式和控制流

- 简单转发, 简单属性, 单表达式计算可以使用表达式体成员.
- 包含校验, 分支, 临时变量, 多步骤构造, 资源释放的逻辑使用块体.
- 简短 guard clause 可以单行书写, 例如 `if (samples.IsEmpty) return default;`.
- 参数校验优先使用 BCL helper, 例如 `ArgumentNullException.ThrowIfNull` 和 `ArgumentOutOfRangeException.ThrowIfNegative`. 需要自定义消息或复合条件时使用显式 `throw new ...`.
- lambda 不捕获外部变量时加 `static`.
- 优先写出明确局部变量类型. 显式类型过长, 来自 Roslyn 或 LINQ 链式 API, 或右侧类型已经清晰时可以使用 `var`.
- 高性能路径可以使用 `ref struct`, `Span<T>`, `ArrayPool<T>`, `[MethodImpl(AggressiveOptimized)]`, 但不要为了统一样式改变性能语义.

## 测试代码

- xUnit 测试使用 `[Fact]`.
- 测试类使用大驼峰并以被测类型或能力命名, 通常为 `sealed class`.
- 测试方法使用 `Subject_WhenCondition_ShouldResult` 命名.
- 测试数据集合优先使用集合表达式, 除非要验证特定构造函数或集合类型行为.

# 使用 Resharper 检查代码
- 在收尾阶段, 可以考虑使用 Resharper 引擎检查一次代码样式问题.
- 如果本地不支持 Resharper 引擎, 可以跳过这一步.
- 该检查耗时较长, 尽可能缩小检查范围( `Jarfter.sln` 可以更换为具体的项目), 减少检查次数(仅在收尾阶段执行).
- 使用下面的代码进行检查, 输出文件位于 `output.txt` 中.
- 可以使用 `--format=Xml` 选项来指定生成体积更小的 `xml` 输出, 也可以使用默认更规范的 `SARIF` 格式输出.
- 并非强制处理所有给出的问题, 优先处理极其明显且容易修正的即可. 未处理的问题需要告知用户.
```powershell
$cache = Join-Path $env:TEMP ("jarfter-inspect-cache-" + [guid]::NewGuid().ToString("N"))
jb inspectcode Jarfter.sln --no-build --output=output.txt --caches-home=$cache --severity=SUGGESTION
```
