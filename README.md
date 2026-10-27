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

Place the published executable in a subfolder alongside the `ollama-vulkan`
directory. The application searches parent directories for
`ollama-vulkan\ollama.exe`.

## Settings

The **Settings** window lets you edit the Ollama model directory, host address,
context length, and keep-alive duration. Save stores the values per Windows user
in `%LOCALAPPDATA%\OllamaTray\settings.json`; they apply the next time Ollama is
started. 
