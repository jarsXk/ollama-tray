# Ollama Tray

A simple Windows system tray application for managing the adjacent
`ollama-vulkan` project.

## Requirements

- Windows 10/11 x64
- Ollama
- .NET 10 SDK for local builds

## Build and run

To run locally from this directory:

```powershell
dotnet run
```

To publish a self-contained executable:

```powershell
dotnet publish OllamaTray.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output publish
```

The published application includes the .NET runtime and does not require a
separate .NET installation on the target computer.

The installer uses per-user installation and does not require administrator
rights. It lets you choose the Start Menu folder for the Ollama Tray shortcut.
Its optional, unchecked task downloads and silently installs the latest
published Ollama Vulkan Windows amd64 release from GitHub; if that download or
installation fails, Tray setup continues and shows a warning. A separate,
unchecked task adds Ollama Tray to the current user's Windows startup. This
starts the tray app only; it does not automatically start the Ollama server.
The final installer page offers a checked-by-default option to launch Ollama
Tray immediately.

The `ollama-tray` project must be hosted in a GitHub repository for the workflow
to run. The repository root must contain `OllamaTray.csproj` and `.github`.
Currently, `ollama-tray` is a sibling of the `ollama-vulkan` repository, not part
of it; create a separate repository for this project and push its contents
there.

Place the published executable in a subfolder alongside the `ollama-vulkan`
directory. The application searches parent directories for
`ollama-vulkan\ollama.exe`.

## Settings

The **Settings** window lets you edit the Ollama model directory, host address,
context length, and keep-alive duration. Save stores the values per Windows user
in `%LOCALAPPDATA%\OllamaTray\settings.json`; they apply the next time Ollama is
started. Cancel discards edits. The settings file is outside the application
installation directory, so uninstalling or reinstalling Ollama Tray does not
remove or replace it. Only saving new values in the Settings window changes it.

The English setting labels follow the Ollama server configuration terminology
in the [Ollama FAQ](https://docs.ollama.com/faq#how-do-i-configure-ollama-server).
