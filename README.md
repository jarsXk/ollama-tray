# Ollama Tray

A simple Windows system tray application for managing the adjacent
`ollama-vulkan` project.

## Requirements

- Windows x64
- .NET 10 Desktop Runtime (x64) to run the application:
  [download the installer](https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe)
  or choose another .NET 10 release from the
  [official downloads page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
- .NET 10 SDK for local builds
- `ollama-vulkan\ollama.exe`

## Build and run

To run locally from this directory:

```powershell
dotnet run
```

To publish a small, framework-dependent executable:

```powershell
dotnet publish OllamaTray.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  --output publish
```

The published application requires the .NET 10 Desktop Runtime (x64) to be
installed on the target computer. It does not bundle the .NET runtime.

## GitHub Actions

The workflow in `.github/workflows/build.yml` runs on pushes to `main`, on pull
requests, and manually from the **Actions** tab. It builds a
framework-dependent Windows x64 application with .NET 10 and uploads an
`ollama-tray-win-amd64` archive as a workflow artifact. The artifact is available
for 14 days.

The `ollama-tray` project must be hosted in a GitHub repository for the workflow
to run. The repository root must contain `OllamaTray.csproj` and `.github`.
Currently, `ollama-tray` is a sibling of the `ollama-vulkan` repository, not part
of it; create a separate repository for this project and push its contents
there.

Place the published executable in a subfolder alongside the `ollama-vulkan`
directory. The application searches parent directories for
`ollama-vulkan\ollama.exe`.

## Application icon

The tray switches between multi-size icons derived from Ollama's official
128x128 logo. The stopped and running icons are
[`assets/ollama-stopped.ico`](assets/ollama-stopped.ico) and
[`assets/ollama-running.ico`](assets/ollama-running.ico), with gray and green
status badges respectively. An error-state icon is also prepared at
[`assets/ollama-error.ico`](assets/ollama-error.ico), but is not currently used
by the application. The logo sits on a white circle for visibility on dark
Windows themes; the area outside it is transparent. The original PNG sources
are in `assets`. The logo source is from
[`ollama/ollama`](https://github.com/ollama/ollama/blob/main/docs/images/logo.png).

## Tray menu

The application uses Russian when the Windows display language is Russian and
English for all other Windows display languages. The language is selected at
startup.

- Checks the server status in the background every 30 seconds at
  `http://127.0.0.1:11434/api/version`. The menu initially shows the server as
  stopped and updates its status only when the state changes.
- Shows `http://127.0.0.1:11434` as the status menu item while the server is
  running; clicking it copies the API base URL to the clipboard. When stopped,
  the item displays "Server stopped" and is disabled.
- Starts `ollama.exe serve` directly as a hidden process. Before starting, it
  checks whether the server is already running. The executable is searched for
  in the adjacent `ollama-vulkan` folder, then in
  `%LOCALAPPDATA%\Programs\Ollama Vulkan`; if it is not found, the app tries to
  start `ollama.exe` through `PATH`. It sets `OLLAMA_MODELS` to
  `%USERPROFILE%\.ollama\models`, `OLLAMA_HOST` to `127.0.0.1:11434`,
  `OLLAMA_CONTEXT_LENGTH` to `8192`, and `OLLAMA_KEEP_ALIVE` to `5m`, matching
  `start-ollama.cmd`. Standard output and errors are appended to
  `logs\ollama.log` beside `ollama.exe`. The app waits up to 15 seconds for the
  server to respond and reports an error if it does not.
- Stops the `ollama.exe` process listening on port `11434`, including a server
  started outside this application. This disconnects all clients using that
  server. Before stopping, it checks whether the server is already stopped.
- Opens the saved `logs\ollama.log` in the default Windows application for log
  files. The log is created when the tray app starts Ollama; an error is shown
  if no log file exists yet or Windows cannot open it.
- Exits the tray application without stopping Ollama.

Stopping is forceful because Ollama does not provide a server shutdown API.
This version does not integrate with the Ollama installer or configure
automatic startup with Windows.
