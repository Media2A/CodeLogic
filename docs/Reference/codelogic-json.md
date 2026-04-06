# CodeLogic.json Reference

The framework configuration file. Located at `{FrameworkRoot}/Framework/CodeLogic.json`.

---

## Files and Load Order

| File | When loaded |
|------|-------------|
| `CodeLogic.json` | Always (production config) |
| `CodeLogic.Development.json` | When `DEBUG` build OR debugger attached — overlays the base config |

Both files have the same schema. `CodeLogic.Development.json` only needs to contain the sections you want to override — other sections inherit their defaults.

**Add `CodeLogic.Development.json` to `.gitignore`** — it is per-machine.

---

## Complete Schema with All Options

```json
{
  "framework": {
    "name": "CodeLogic",
    "version": "3.0.0"
  },

  "logging": {
    "mode": "singleFile",
    "maxFileSizeMb": 10,
    "maxRolledFiles": 5,
    "fileNamePattern": "{date:yyyy}/{date:MM}/{date:dd}/{level}.log",
    "globalLevel": "Warning",
    "enableConsoleOutput": false,
    "consoleMinimumLevel": "Debug",
    "enableDebugMode": false,
    "centralizedDebugLog": false,
    "centralizedLogsPath": null,
    "includeMachineName": true,
    "timestampFormat": "yyyy-MM-dd HH:mm:ss.fff"
  },

  "localization": {
    "defaultCulture": "en-US",
    "supportedCultures": ["en-US"],
    "autoGenerateTemplates": true
  },

  "libraries": {
    "discoveryPattern": "CL.*",
    "enableDependencyResolution": true
  },

  "healthChecks": {
    "enabled": true,
    "intervalSeconds": 30
  }
}
```

---

## Section Reference

### framework

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `name` | string | `"CodeLogic"` | Display name. Informational only. |
| `version` | string | `"3.0.0"` | Framework version. Informational only. |

---

### logging

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `mode` | string | `"singleFile"` | `"singleFile"` or `"dateFolder"` |
| `maxFileSizeMb` | int | `10` | Max log file size before rolling (singleFile only) |
| `maxRolledFiles` | int | `5` | Number of old files to keep (singleFile only) |
| `fileNamePattern` | string | see below | File path pattern for dateFolder mode |
| `globalLevel` | string | `"Warning"` | Minimum level written to disk: Trace, Debug, Info, Warning, Error, Critical |
| `enableConsoleOutput` | bool | `false` | Write logs to console as well as files |
| `consoleMinimumLevel` | string | `"Debug"` | Minimum level for console output |
| `enableDebugMode` | bool | `false` | Enable verbose debug output |
| `centralizedDebugLog` | bool | `false` | Write all library logs to a shared debug file |
| `centralizedLogsPath` | string? | `null` | Path for centralized log. Null = default location |
| `includeMachineName` | bool | `true` | Prepend machine name to each log entry |
| `timestampFormat` | string | `"yyyy-MM-dd HH:mm:ss.fff"` | Timestamp format for log entries |

#### fileNamePattern tokens

| Token | Example output |
|-------|----------------|
| `{date:yyyy}` | `2026` |
| `{date:MM}` | `04` |
| `{date:dd}` | `06` |
| `{level}` | `warning` |

Default: `"{date:yyyy}/{date:MM}/{date:dd}/{level}.log"` → `2026/04/06/warning.log`

---

### localization

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `defaultCulture` | string | `"en-US"` | Fallback culture when requested one is unavailable |
| `supportedCultures` | string[] | `["en-US"]` | Cultures to generate templates and load for |
| `autoGenerateTemplates` | bool | `true` | Auto-generate missing localization template files |

---

### libraries

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `discoveryPattern` | string | `"CL.*"` | Glob pattern for library subdirectory discovery under `Libraries/` |
| `enableDependencyResolution` | bool | `true` | Sort libraries by dependencies before startup |

---

### healthChecks

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `enabled` | bool | `true` | Enable scheduled health checks |
| `intervalSeconds` | int | `30` | How often to run health checks |

---

## Development Override Example

Typical `CodeLogic.Development.json` — only override what differs from production:

```json
{
  "logging": {
    "globalLevel": "Debug",
    "enableConsoleOutput": true,
    "consoleMinimumLevel": "Debug",
    "enableDebugMode": true
  },
  "localization": {
    "supportedCultures": ["en-US", "da-DK"]
  },
  "healthChecks": {
    "intervalSeconds": 10
  }
}
```

---

## Production Example

A more realistic production `CodeLogic.json`:

```json
{
  "framework": {
    "name": "HomePoint",
    "version": "3.0.0"
  },
  "logging": {
    "mode": "dateFolder",
    "fileNamePattern": "{date:yyyy}/{date:MM}/{date:dd}/{level}.log",
    "globalLevel": "Info",
    "enableConsoleOutput": false,
    "maxRolledFiles": 30,
    "includeMachineName": true,
    "timestampFormat": "yyyy-MM-dd HH:mm:ss.fff"
  },
  "localization": {
    "defaultCulture": "en-US",
    "supportedCultures": ["en-US", "da-DK", "de-DE"]
  },
  "libraries": {
    "discoveryPattern": "CL.*",
    "enableDependencyResolution": true
  },
  "healthChecks": {
    "enabled": true,
    "intervalSeconds": 60
  }
}
```

---

## File Location

The exact path to each config file is returned by `CodeLogicOptions`:

```csharp
var opts = CodeLogic.GetOptions();

opts.GetCodeLogicConfigPath();
// → C:\MyApp\CodeLogic\Framework\CodeLogic.json

opts.GetCodeLogicDevelopmentConfigPath();
// → C:\MyApp\CodeLogic\Framework\CodeLogic.Development.json
```

Both files are in `{FrameworkRoot}/Framework/`. The `Framework/` subdirectory also contains the framework's own log file.
