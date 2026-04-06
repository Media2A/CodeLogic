# Localization

CodeLogic provides per-component locale files with culture fallback, format string support, and hot-reload. Each library manages its own strings independently.

---

## LocalizationModelBase

All localization classes inherit from `LocalizationModelBase`:

```csharp
public abstract class LocalizationModelBase { }
```

Define string properties with default values as the fallback (usually English):

```csharp
[LocalizationSection("ui")]
public class UiStrings : LocalizationModelBase
{
    [LocalizedString]
    public string WelcomeMessage { get; set; } = "Welcome, {0}!";

    [LocalizedString]
    public string LogoutConfirm { get; set; } = "Are you sure you want to log out?";

    [LocalizedString]
    public string ItemCount { get; set; } = "You have {0} items.";
}
```

---

## LocalizationSectionAttribute

Controls the file name for a localization model, similar to `ConfigSectionAttribute`:

```csharp
[LocalizationSection("ui")]
public class UiStrings : LocalizationModelBase { ... }
```

Files are placed in `{LibraryDirectory}/localization/`:

| Attribute | Culture | File |
|-----------|---------|------|
| `[LocalizationSection("ui")]` | `en` (default) | `localization/ui.en.json` |
| `[LocalizationSection("ui")]` | `da` | `localization/ui.da.json` |
| `[LocalizationSection("ui")]` | `de` | `localization/ui.de.json` |

---

## LocalizedStringAttribute

Mark each property that should be localized:

```csharp
[LocalizedString]
public string WelcomeMessage { get; set; } = "Welcome, {0}!";
```

Properties without the attribute are not serialized into locale files.

---

## Registering Localization Models

Register in `OnConfigureAsync`:

```csharp
public Task OnConfigureAsync(LibraryContext context)
{
    context.Localization.Register<UiStrings>();
    return Task.CompletedTask;
}
```

---

## Reading Localized Strings

Read in `OnInitializeAsync` or later:

```csharp
private UiStrings _strings = null!;

public Task OnInitializeAsync(LibraryContext context)
{
    _strings = context.Localization.Get<UiStrings>();
    return Task.CompletedTask;
}

public string GetWelcome(string username)
    => string.Format(_strings.WelcomeMessage, username);
```

---

## ILocalizationManager API

```csharp
public interface ILocalizationManager
{
    void Register<T>() where T : LocalizationModelBase, new();
    T Get<T>() where T : LocalizationModelBase;
    Task ReloadAsync();
    Task SetCultureAsync(string cultureName);
    string CurrentCulture { get; }
}
```

---

## Culture Fallback

When a locale file for the requested culture is missing, CodeLogic falls back through the chain:

```
da-DK  →  da  →  en  →  property default value
```

This means you can ship `en` locale files and add translations incrementally without breaking anything.

---

## Locale File Format

Locale files are plain JSON:

```json
// localization/ui.da.json
{
  "WelcomeMessage": "Velkommen, {0}!",
  "LogoutConfirm": "Er du sikker på, at du vil logge ud?",
  "ItemCount": "Du har {0} elementer."
}
```

Use `{0}`, `{1}`, etc. as placeholders and format with `string.Format`.

---

## Hot Reload

Reload localization files at runtime:

```csharp
await context.Localization.ReloadAsync();
```

Useful when locale files are updated without restarting the application.

---

## Switching Culture at Runtime

```csharp
await context.Localization.SetCultureAsync("da-DK");
var strings = context.Localization.Get<UiStrings>();
// now returns Danish strings (or falls back to en)
```
