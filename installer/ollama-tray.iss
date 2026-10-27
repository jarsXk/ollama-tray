#ifndef MyAppVersion
  #error MyAppVersion must be specified when compiling the installer.
#endif

#ifndef PublishDir
  #error PublishDir must be specified when compiling the installer.
#endif

#ifndef InstallerOutputDir
  #error InstallerOutputDir must be specified when compiling the installer.
#endif

[Setup]
AppId={{A9B16403-2560-42D9-91DD-2979C20C2F66}
AppName=Ollama Tray
AppVersion={#MyAppVersion}
AppPublisher=jarsXk
DefaultDirName={localappdata}\Programs\Ollama Tray
; User settings are stored under %LOCALAPPDATA%\OllamaTray, outside {app}.
; Do not add uninstall cleanup rules for that user-data directory.
DefaultGroupName=Ollama Tray
DisableProgramGroupPage=no
UsePreviousGroup=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName=Ollama Tray
UninstallDisplayIcon={app}\OllamaTray.exe
OutputDir={#InstallerOutputDir}
OutputBaseFilename=ollama-tray-v{#MyAppVersion}-win-amd64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UsePreviousTasks=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
english.AdditionalOptions=Additional options:
russian.AdditionalOptions=Дополнительные параметры:
english.InstallLatestVulkanTask=Download and install the latest Ollama Vulkan release
russian.InstallLatestVulkanTask=Скачать и установить последнюю версию Ollama Vulkan
english.AddToStartupTask=Start Ollama Tray when I sign in to Windows
russian.AddToStartupTask=Запускать Ollama Tray при входе в Windows
english.DownloadingVulkan=Downloading and installing the latest Ollama Vulkan release...
russian.DownloadingVulkan=Загрузка и установка последней версии Ollama Vulkan...
english.LaunchTray=Launch Ollama Tray
russian.LaunchTray=Запустить Ollama Tray

[Tasks]
Name: "installvulkan"; Description: "{cm:InstallLatestVulkanTask}"; GroupDescription: "{cm:AdditionalOptions}"; Flags: unchecked
Name: "startup"; Description: "{cm:AddToStartupTask}"; GroupDescription: "{cm:AdditionalOptions}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\OllamaTray.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "install-latest-ollama-vulkan.ps1"; Flags: dontcopy

[Icons]
Name: "{group}\Ollama Tray"; Filename: "{app}\OllamaTray.exe"; WorkingDir: "{app}"
Name: "{userstartup}\Ollama Tray"; Filename: "{app}\OllamaTray.exe"; WorkingDir: "{app}"; Tasks: startup

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{tmp}\install-latest-ollama-vulkan.ps1"""; StatusMsg: "{cm:DownloadingVulkan}"; Flags: waituntilterminated runhidden; Tasks: installvulkan
Filename: "{app}\OllamaTray.exe"; Description: "{cm:LaunchTray}"; Flags: postinstall nowait skipifsilent

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if WizardIsTaskSelected('installvulkan') then
    ExtractTemporaryFile('install-latest-ollama-vulkan.ps1');
end;
