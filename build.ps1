$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
    & (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ '/win32icon:..\assets\dc3-viewer.ico' '/resource:..\assets\dc3-viewer.ico,DC3.AppIcon.ico' '/out:..\Dino Crisis 3 Model Viewer.exe' /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:System.Core.dll /reference:System.Xaml.dll "/reference:$framework\WPF\PresentationCore.dll" "/reference:$framework\WPF\PresentationFramework.dll" "/reference:$framework\WPF\WindowsBase.dll" '/resource:..\assets\dc3-logo.png,DC3.Logo.png' '/resource:..\assets\model-viewer.png,DC3.Subtitle.png' '/resource:..\assets\Theme.xaml,DC3.Theme.xaml' Model.cs Viewer.cs AssemblyInfo.cs
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
} finally { Pop-Location }
