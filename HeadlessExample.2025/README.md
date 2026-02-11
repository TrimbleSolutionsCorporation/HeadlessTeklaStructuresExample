# HeadlessTeklaStructuresExample

Run Tekla Structures headlessly (no GUI) with **subscription licensing**. This extends
Trimble's original example — which only supported FlexNet (localhost) — by authenticating
via `Trimble.Auth.AuthManager` and passing OAuth tokens to `TeklaStructuresService.Initialize()`.

## Prerequisites

1. **Tekla Structures installed** (2025.0 or later recommended)
2. **Signed into Tekla Structures GUI at least once** — the AuthManager reuses the cached
   refresh token from your first interactive login
3. **Copy two DLLs** from your Tekla `bin\` folder into the `dlls\` folder in this repo
   (see [DLL Setup](#dll-setup))

## DLL Setup

The project references two DLLs that ship with Tekla but aren't available on NuGet.
Copy them from your Tekla installation into the `dlls\` folder:

```
C:\Program Files\Tekla Structures\2025.0\bin\DotNetKit.dll
C:\Program Files\Tekla Structures\2025.0\bin\Trimble.Identity.AuthManager.dll
```

These are gitignored — each developer copies from their own install.

## Build

```powershell
dotnet build HeadlessExample.sln -c Release
```

Output goes to `BuildDrop\net48\`.

**Important:** The built exe must be run from the Tekla `bin\` folder (or have `DotNetKit.dll`
co-located), because the JIT loads it before `AssemblyResolve` fires.

## Usage

```
HeadlessExample.exe <modelPath> [teklaVersion] [envIniPath] [roleIniPath] [license]
```

| Argument       | Description                                    | Default   |
|----------------|------------------------------------------------|-----------|
| `modelPath`    | Full path to Tekla model folder (**required**) | —         |
| `teklaVersion` | e.g. `2025.0`                                  | `2025.0`  |
| `envIniPath`   | Full path to environment `.ini` file           | *(none)*  |
| `roleIniPath`  | Full path to role `.ini` file                  | *(none)*  |
| `license`      | License type: `Diamond`, `Premium`, etc.       | `Diamond` |

### Example

```powershell
# Copy exe to Tekla bin, then run:
cd "C:\Program Files\Tekla Structures\2025.0\bin"
.\HeadlessExample.2025.exe "C:\TeklaStructuresModels\MyProject" 2025.0
```

## How It Works

1. **Authenticate** — `AuthManager` reads the cached refresh token from your previous
   Tekla GUI session and exchanges it for fresh OAuth tokens (Trimble ID token + access token)
2. **Initialize** — `TeklaStructuresService.Initialize()` is called with the model path,
   license type, and both tokens. This activates the subscription license headlessly.
3. **Query** — Once the license is active (polled up to 60s), the Tekla Open API is
   available: enumerate objects, read properties, create/modify geometry, etc.

## License

BSD 3-Clause — see [LICENSE](LICENSE).
