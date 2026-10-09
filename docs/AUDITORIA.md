# Auditoría de la base — 9 de octubre de 2026

- Solución real: `YoutubeWallpaper.slnx`; proyecto `YoutubeWallpaper/YoutubeWallpaper.csproj`.
- C#, WinForms, `net8.0-windows`; namespace y nombre del ejecutable: `YoutubeWallpaper`.
- Única dependencia: Microsoft.Web.WebView2 **1.0.4258.31**, conservada.
- `Program.cs` iniciaba `Form1`; el diseñador contenía `webView21` con Dock=Fill.
- `Form1.cs` navegaba a `https://www.youtube.com/`. No existían host del escritorio, bandeja, parser ni persistencia.
- Sin Git, remoto o instrucciones AGENTS.md al inspeccionar la carpeta y sus padres.
- Restore de la base y build Debug correctos usando la caché NuGet existente. Una advertencia MSB3277 proviene de la referencia WPF transitiva de WebView2 en un proyecto WinForms.
- SDK disponible: 8.0.413 y 10.0.401. Se compiló la solución `.slnx` con SDK 10 manteniendo el target .NET 8. El SDK 8 puede compilar directamente el `.csproj`.
- El entorno restringe DNS y escrituras fuera del proyecto: el restore inicial falló; se recuperó con `NUGET_PACKAGES` apuntando a la caché existente, `DOTNET_CLI_HOME` dentro de `.build`, `--ignore-failed-sources` y `NuGetAudit=false` solo durante la comprobación local. No se desactiva la auditoría en el proyecto.

La especificación se usa como referencia de requisitos para esta implementación. La carga inicial de YouTube fue declarada por el usuario; el build de la base se comprobó aquí, sin atribuirle una prueba interactiva de reproducción.
