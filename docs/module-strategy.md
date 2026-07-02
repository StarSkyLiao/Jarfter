# Module Strategy

Jarfter uses NuGet packages as the module boundary.

## Current Modules

| Module | Role | Package |
| --- | --- | --- |
| `Jarfter.Core` | Core contracts for Jarfter modules and extensions. | `Jarfter.Core` |

## Future Modules

Future extension modules should be added only when there is a concrete feature boundary.

Recommended naming:

- `Jarfter.Extensions.<Feature>`
- `Jarfter.<Domain>` when the module is a first-class domain package rather than an optional extension.

Recommended dependency direction:

```text
Jarfter.Extensions.* -> Jarfter.Core
Jarfter.<Domain>     -> Jarfter.Core
Jarfter.Core         -> no Jarfter module dependency
```

`Jarfter.Core` should stay small and stable. Prefer adding contracts, primitives, and cross-module abstractions here. Avoid feature-specific implementation code.

## External Consumption

Other repositories should consume modules through pinned NuGet versions.

Use project references only inside this repository during local development:

```xml
<ProjectReference Include="..\Jarfter.Core\Jarfter.Core.csproj" />
```

Use package references from external repositories:

```xml
<PackageReference Include="Jarfter.Core" Version="0.1.0" />
```

This keeps external projects insulated from in-progress work on `dev` or `feature/*` branches.

