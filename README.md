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

The workflow in `.github/workflows/build.yml` runs on pushes to `main` or
`master`, on pull requests, and manually from the **Actions** tab. It builds a
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

## Tray menu

- Checks the server status every three seconds at
  `http://127.0.0.1:11434/api/version`.
- Starts the server by running the existing `start-ollama.cmd /hidden` script,
  preserving its environment settings and log behavior.
- Stops the `ollama.exe` process listening on port `11434`, including a server
  started outside this application. This disconnects all clients using that
  server.
- Exits the tray application without stopping Ollama.

Stopping is forceful because Ollama does not provide a server shutdown API.
This version does not integrate with the Ollama installer or configure
automatic startup with Windows.
