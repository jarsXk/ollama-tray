# Ollama Tray

A simple Windows system tray application for managing the adjacent
`ollama-vulkan` project.

## Requirements

- Windows x64
- .NET 10 SDK for local builds
- `ollama-vulkan\start-ollama.cmd` and `ollama-vulkan\ollama.exe`

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

## GitHub Actions

The workflow in `.github/workflows/build.yml` runs on pushes to `main`, on pull
requests, and manually from the **Actions** tab. It builds a
self-contained Windows x64 application with .NET 10 and uploads an
`ollama-tray-win-x64` archive as a workflow artifact. The artifact is available
for 14 days.

The `ollama-tray` project must be hosted in a GitHub repository for the workflow
to run. The repository root must contain `OllamaTray.csproj` and `.github`.
Currently, `ollama-tray` is a sibling of the `ollama-vulkan` repository, not part
of it; create a separate repository for this project and push its contents
there.

Place the published executable in a subfolder alongside the `ollama-vulkan`
directory. The application searches parent directories for
`ollama-vulkan\start-ollama.cmd`.

## Application icon

The app and tray use a multi-size ICO derived from Ollama's official 128x128
logo: [`assets/ollama.ico`](assets/ollama.ico). The original logo is used to
preserve its sharp details. It sits on a white circular background for better
visibility on dark Windows themes; the area outside the circle is transparent.
The [icon preview](assets/ollama-icon-preview.png) and original PNG sources are
kept in `assets`. The source is from
[`ollama/ollama`](https://github.com/ollama/ollama/blob/main/docs/images/logo.png).

## Tray menu

- Checks the server status in the background every 30 seconds at
  `http://127.0.0.1:11434/api/version`. The menu initially shows the server as
  stopped and updates its status only when the state changes.
- Starts the server by running the existing `start-ollama.cmd /hidden` script,
  preserving its environment settings and log behavior. Before starting, it
  checks whether the server is already running.
- Stops the `ollama.exe` process listening on port `11434`, including a server
  started outside this application. This disconnects all clients using that
  server. Before stopping, it checks whether the server is already stopped.
- Exits the tray application without stopping Ollama.

Stopping is forceful because Ollama does not provide a server shutdown API.
This version does not integrate with the Ollama installer or configure
automatic startup with Windows.
