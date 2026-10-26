$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Show-InstallationWarning {
    param([string]$Details)

    Add-Type -AssemblyName System.Windows.Forms
    if ([System.Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq 'ru') {
        $caption = 'Ollama Vulkan не установлена'
        $message = "Не удалось автоматически скачать или установить Ollama Vulkan.`r`n`r`n$Details`r`n`r`nУстановка Ollama Tray будет продолжена. Ollama Vulkan можно установить позже: https://github.com/jarsXk/ollama-vulkan/releases"
    }
    else {
        $caption = 'Ollama Vulkan was not installed'
        $message = "Ollama Vulkan could not be downloaded or installed automatically.`r`n`r`n$Details`r`n`r`nOllama Tray setup will continue. You can install Ollama Vulkan later: https://github.com/jarsXk/ollama-vulkan/releases"
    }

    [System.Windows.Forms.MessageBox]::Show(
        $message,
        $caption,
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    ) | Out-Null
}

$installerPath = Join-Path $env:TEMP "ollama-vulkan-installer-$PID.exe"

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $headers = @{ 'User-Agent' = 'OllamaTrayInstaller' }
    $release = Invoke-RestMethod `
        -Uri 'https://api.github.com/repos/jarsXk/ollama-vulkan/releases/latest' `
        -Headers $headers `
        -TimeoutSec 30

    $asset = @($release.assets | Where-Object {
        $_.name -match '^ollama-vulkan-.+-win-amd64\.exe$'
    } | Select-Object -First 1)[0]
    if ($null -eq $asset -or [string]::IsNullOrWhiteSpace($asset.browser_download_url)) {
        throw 'The latest GitHub release does not contain a Windows amd64 installer.'
    }
    if (!$asset.browser_download_url.StartsWith(
        'https://github.com/jarsXk/ollama-vulkan/releases/download/',
        [StringComparison]::OrdinalIgnoreCase
    )) {
        throw 'The latest release returned an unexpected installer download URL.'
    }

    Invoke-WebRequest `
        -Uri $asset.browser_download_url `
        -OutFile $installerPath `
        -UseBasicParsing `
        -TimeoutSec 1200

    if (!(Test-Path $installerPath) -or (Get-Item $installerPath).Length -lt 1MB) {
        throw 'The downloaded installer is missing or incomplete.'
    }

    if ($asset.digest -match '^sha256:([0-9a-fA-F]{64})$') {
        $actualDigest = (Get-FileHash -Path $installerPath -Algorithm SHA256).Hash
        if ($actualDigest -ine $Matches[1]) {
            throw 'The downloaded installer failed its SHA-256 integrity check.'
        }
    }

    $installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\Ollama Vulkan'
    $arguments = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /DIR="{0}"' -f $installDirectory
    $process = Start-Process `
        -FilePath $installerPath `
        -ArgumentList $arguments `
        -Wait `
        -PassThru
    if ($process.ExitCode -ne 0) {
        throw "The Ollama Vulkan installer exited with code $($process.ExitCode)."
    }
}
catch {
    Show-InstallationWarning -Details $_.Exception.Message
}
finally {
    Remove-Item -LiteralPath $installerPath -Force -ErrorAction SilentlyContinue
}
