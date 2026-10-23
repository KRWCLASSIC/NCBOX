# NCBOX --- Nginx & Certificate Toolbox

NCBOX is a cross-platform, C#/.NET 10 NativeAOT project that makes
hosting websites with Nginx and managing TLS certificates with Certbot
easier through simple, lightweight CLI tools.

It is organized as a high-performance monorepo containing **ngc** (Nginx
Configuration) and **cbc** (Certbot Configuration), backed by shared
infrastructure and an integrated C# NativeAOT builder pipeline (modeled
after the architecture in `kutils`). Each tool works independently, but
they seamlessly integrate when installed together.

The philosophy: **make server administration easier without reinventing
Nginx, Certbot, or ACME.** No giant web panel, no mandatory daemon, no
unnecessary dependencies.

---

## 1. Project architecture

Modeled after the modular, zero-bloat architecture in `kutils`:

``` text
NCBOX/
├── .temp/                     # Centralized intermediate artifacts (keeps repo pristine)
├── dist/                      # Final structured output by target RID
│   ├── linux-x64/
│   │   ├── ngc                # Standalone NativeAOT executable
│   │   ├── cbc                # Standalone NativeAOT executable
│   │   └── Intermediate/      # PDB symbols, build logs, and manifests
│   └── win-x64/
│       ├── ngc.exe
│       ├── cbc.exe
│       └── Intermediate/
├── builder/                   # Dedicated C# build pipeline tool
│   ├── Apps/                  # App-specific build configurations
│   │   ├── AppRegistry.cs     # App registration and discovery
│   │   ├── IAppBuilder.cs     # Modular builder interface
│   │   ├── NgcAppBuilder.cs   # ngc publishing and artifact staging
│   │   └── CbcAppBuilder.cs   # cbc publishing and artifact staging
│   ├── Pipeline/              # Core build pipeline engine
│   │   ├── BuildContext.cs    # Repository root, target RID, and configuration state
│   │   ├── CompileConfig.cs   # Maps compile-config.json to NativeAOT MSBuild flags
│   │   ├── DistManager.cs     # Artifact staging, layout guarantee, and tree visualizer
│   │   ├── DotnetPublisher.cs # Invokes dotnet publish with isolated args
│   │   └── ProcessRunner.cs   # Process executor with exit codes and streaming output
│   ├── compile-config.json    # Declarative AOT compiler & optimization profile
│   ├── Program.cs             # Builder CLI entry point (ANSI UI, commands, timings)
│   └── builder.csproj
├── src/
│   ├── Apps/
│   │   ├── ngc/               # Nginx CLI application
│   │   │   ├── Cli/            # Typed commands, argument parser, and terminal UI
│   │   │   ├── Engine/         # Executor, Nginx, site config/catalog/link, and settings services
│   │   │   ├── Program.cs      # Thin entry point and exit-code handling
│   │   │   └── ngc.csproj
│   │   └── cbc/               # Certbot CLI application
│   │       ├── Cli/            # Typed commands, argument parser, and terminal UI
│   │       ├── Engine/         # Command executor and command results
│   │       ├── Services/       # Certbot and renewal scheduler operations
│   │       ├── Program.cs      # Thin entry point and exit-code handling
│   │       └── cbc.csproj
│   └── NCBOX.Core/            # Shared infrastructure library
│       ├── Paths/             # Standard directories and executable discovery
│       ├── Config/            # Settings management (JSON)
│       ├── Process/           # Process runners (nginx, certbot, systemctl)
│       ├── Platform/          # Linux/Windows detection, service controllers
│       ├── Logging/           # Typed terminal messages and adaptive color output
│       ├── Validation/        # Domain, port, path, and syntax validation
│       └── NCBOX.Core.csproj
├── Directory.Build.props      # Monorepo-wide settings, versioning & UseArtifactsOutput
├── Directory.Build.targets    # Shared MSBuild targets
├── ncbox.slnx                 # Modern .NET solution format
├── build.bat                  # Windows builder launcher: dotnet run --project "%~dp0builder" -- %*
└── build.sh                   # Linux builder launcher:   dotnet run --project "$SCRIPT_DIR/builder" -- "$@"
```

### Component breakdown

-   **ngc (`src/Apps/ngc`)** --- Nginx configuration generation, website setup,
    reverse proxies, static sites, symlink management, and service control.
-   **cbc (`src/Apps/cbc`)** --- certificate discovery, issuance, inspection,
    multi-domain consolidation (merge), and automatic renewal via Certbot.
-   **NCBOX.Core (`src/NCBOX.Core`)** --- shared abstractions used by both tools:
    executable discovery, process execution, config schemas, platform detection, and terminal output.
-   **Builder (`builder/`)** --- independent, fast C# build runner that handles
    targeted builds, aggressive NativeAOT trimming, symbol stripping, and artifact layout.

ngc and cbc compile into standalone, single-file native binaries with zero external
runtime dependencies.

---

## 2. The Builder Pipeline

The build system avoids complex Makefiles or fragile shell scripts. Both `build.bat`
and `build.sh` forward arguments directly to the compiled C# builder project:

```bash
# Windows
build.bat [target] [options]

# Linux
./build.sh [target] [options]
```

### Builder commands

| Command | Action |
| :--- | :--- |
| `build all` | Compiles the full suite (ngc + cbc) in optimized Release NativeAOT (default) |
| `build project:ngc` | Targeted rebuild of ngc only |
| `build project:cbc` | Targeted rebuild of cbc only |
| `build dev` / `-d` | Fast debug build with portable symbols and disabled AOT linking |
| `build clean` | Completely cleans `dist/`, `.temp/`, and all recursive `bin/` and `obj/` folders |
| `build version <ver>` | Inspects or bumps the suite version across `Directory.Build.props` |
| `build win-x64` / `linux-x64` | Cross-compiles for a specific target Runtime Identifier (RID) |

### Declarative NativeAOT configuration (`builder/compile-config.json`)

All compiler optimization flags are managed in a single, clean JSON profile rather
than polluting project files:

```json
{
  "optimization": {
    "preference": "Size",
    "foldIdenticalMethodBodies": true,
    "trimMetadata": true,
    "stripSymbols": true,
    "generateStackTraceData": false,
    "invariantGlobalization": true,
    "eventSourceSupport": false,
    "debuggerSupport": false,
    "useSystemResourceKeys": true,
    "httpActivityPropagationSupport": false,
    "enableUnsafeBinaryFormatterSerialization": false,
    "enableUnsafeUTF7Encoding": false,
    "trimMode": "full"
  },
  "debug": {
    "keepIntermediatePdbFiles": true
  }
}
```

The pipeline dynamically maps these into `IlcFoldIdenticalMethodBodies`, `IlcTrimMetadata`,
`StripSymbols`, and related MSBuild arguments when invoking `dotnet publish`.

### Distribution manager (`DistManager`) & `UseArtifactsOutput`

- `.temp/artifacts/`: All intermediate MSBuild files and compiler caches are isolated here,
  keeping `src/` completely clean.
- `dist/<rid>/`: Final executables are cleanly placed here, while `.pdb` files and
  intermediates are automatically redirected to `dist/<rid>/Intermediate/`.
- Once built, `DistManager` prints an ASCII directory tree with exact binary file sizes
  and build elapsed times.

---

## 3. ngc --- Nginx Configuration

ngc turns common Nginx configuration tasks into straightforward commands.
It combines modern template-based configuration generation with the proven
site-management workflows from the original `ngc.sh`.

### Website types

-   **Reverse proxy:** Forward incoming domain traffic to a local service/port
    (e.g., `localhost:3000`).
-   **Static website:** Serve a directory containing static assets with proper MIME
    types and caching headers.
-   **Redirects:** Redirect HTTP to HTTPS, canonicalize domain names (e.g., apex to `www`),
    or route paths to a new destination.
-   **Custom configurations:** Allow arbitrary Nginx directives and user-supplied server
    blocks without forcing rigid presets.

### CLI commands

#### Configuration generation
```bash
# Reverse proxy
ngc proxy app.example.com --port 3000

# Static website
ngc static example.com --root /var/www/example

# Redirect a domain
ngc redirect example.com www.example.com

# Request HTTPS integration (delegates to cbc if installed)
ngc proxy app.example.com --port 3000 --certbot

# Explicit HTTP-only configuration
ngc proxy app.example.com --port 3000 --http-only
```

#### Site management & symlinks (from ngc.sh)
```bash
# List all configured domains, link status, and Nginx daemon status
ngc list

# Enable a site (creates symlink: sites-available -> sites-enabled)
ngc enable example.com

# Disable a site (safely removes symlink from sites-enabled)
ngc disable example.com

# Rename a site (updates both available and enabled)
ngc ren old.example.com new.example.com

# Remove a site (safely moves config to .bak and removes symlink)
ngc rm example.com

# List backed-up configurations
ngc listbak

# Restore a configuration from a .bak file
ngc restore example.com

# Re-link all available configs to enabled cleanly
ngc clean
```

#### Verification & service control
```bash
# Validate Nginx configuration with nginx -t
ngc test

# Test configs and reload Nginx (starts service if stopped)
ngc run

# Ensure symlinks exist for all configs, test once, and reload
ngc fullrun
```

---

## 4. cbc --- Certbot Configuration

cbc is a dedicated, zero-friction interface for Certbot and ACME certificate
lifecycles. It can be used completely independently of ngc.

By default, cbc utilizes Certbot's standard **Nginx plugin** (`--nginx` /
`python3-certbot-nginx`) for zero-downtime challenge validation and automated SSL
binding, while supporting webroot and standalone methods where needed.

### CLI commands

| Command | Purpose |
| :--- | :--- |
| `cbc list` | List installed certificates, covered domains, and expiration dates |
| `cbc inspect <domain>` | Find certificates covering a domain |
| `cbc create <domain...>` | Obtain a new certificate via Certbot (defaults to `--nginx` plugin) |
| `cbc merge <domain...>` | Replace separate certificates with one multi-domain (SAN) certificate |
| `cbc renew` | Run certificate renewal dry-run or actual renewal |
| `cbc autorenew` | Configure and verify automatic renewal (systemd timer or cron) |
| `cbc check` | Check Certbot installation, certificate health, and renewal setup |
| `cbc remove <domain>` | Remove a selected certificate lineage after confirmation |

### The `cbc merge` feature

Consolidates multiple independent certificates into a single multi-domain SAN
certificate:

```bash
cbc merge alpha.example.com beta.example.com gamma.example.com
```

1. Inspects existing certificate coverage across all specified domains.
2. Explains the planned changes and domain mappings.
3. Requests a unified replacement certificate via Certbot.
4. Updates Nginx `ssl_certificate` directives safely.
5. Verifies configuration with `nginx -t` before removing or archiving old lineages.
6. Guarantees that existing certificates remain intact if issuance fails.

### The `cbc autorenew` feature

```bash
cbc autorenew
```

1. Inspects active renewal schedulers (systemd timer, cron job, or Windows Task Scheduler).
2. Verifies certificate health and tests a renewal dry-run.
3. Automatically sets up or repairs the renewal timer if missing or broken, without
   creating duplicate jobs.

---

## 5. ngc + cbc integration

The tools remain fully modular. ngc does not require cbc to function.

When `--certbot` is specified in ngc:
```bash
ngc proxy app.example.com --port 3000 --certbot
```

1. ngc checks whether cbc is present on the system.
2. cbc checks Certbot installation and discovers existing domain coverage.
3. The user can select an existing matching certificate, issue a new one, or expand coverage.
4. ngc receives verified certificate file paths and writes the SSL server blocks.
5. ngc runs `nginx -t` validation and reloads Nginx.

If cbc is not installed, ngc provides a clean notification and allows manual certificate
paths or fallback to HTTP-only mode.

---

## 6. Shared Core (`NCBOX.Core`)

Shared functionality lives in a common library to eliminate code duplication:

| Module | Responsibility |
| :--- | :--- |
| **Paths** | Config/data directories (`~/.config/` / `%APPDATA%`), binary resolution (`where`/`which`) |
| **Configuration** | Strongly-typed JSON settings load/save/validate |
| **Process execution** | Asynchronous execution of `nginx`, `certbot`, `systemctl`, capturing stdout/stderr and exit codes |
| **Platform** | OS detection (Linux vs. Windows), init system detection (systemd, sysv, windows services) |
| **Logging** | Colorized ANSI terminal formatter, progress indicators, verbose debug traces |
| **Validation** | Domain syntax, IP/port checks, directory accessibility, and Nginx directive linting |

---

## 7. Cross-platform support

NCBOX compiles natively for Linux and Windows using .NET 10 NativeAOT:

| Feature | Windows | Linux |
| :--- | :--- | :--- |
| **Executables** | `ngc.exe`, `cbc.exe` | `ngc`, `cbc` (ELF 64-bit) |
| **Nginx paths** | Configurable installation directories | Automatic detection (`/etc/nginx/sites-available`, `/etc/nginx/conf.d`) |
| **Privileges** | Standard CLI or elevated prompt | `sudo` / root execution checks |
| **Service control** | Windows Service controls / process signaling | `systemctl reload/start/status nginx` |
| **Certbot plugin** | Certbot for Windows (or manual certs) | `python3-certbot-nginx` plugin |

---

## 8. CI/CD & Release Pipeline

GitHub Actions leverages the integrated builder to publish deterministic release bundles:

```yaml
# Simplified workflow concept
- name: Build NCBOX Linux x64
  run: ./build.sh all linux-x64

- name: Build NCBOX Windows x64
  run: ./build.bat all win-x64
```

Release packages generated in `dist/`:
```text
NCBOX-v1.0.0-linux-x64.tar.gz
NCBOX-v1.0.0-win-x64.zip
```

Each package contains `ngc` and `cbc` as standalone, self-contained native executables
ready to drop into `/usr/local/bin/` or `PATH`.
