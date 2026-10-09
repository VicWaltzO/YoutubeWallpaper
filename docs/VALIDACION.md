# Validación y entrega — 9 de octubre de 2026

## Resultado

- **Debug y Release:** compilación correcta, **0 errores y 0 advertencias**.
- **Publicación:** generada en artifacts/publish, dependiente de .NET Desktop Runtime 8.
- **Suite de base:** 58 comprobaciones, 0 fallos; parser y persistencia.
- **Panel:** 63 comprobaciones, 0 fallos; incluye base, error de URL, X, reapertura y salida programáticas. Render inspeccionado a 96 DPI, sin recorte del contenido.
- **Ciclo real en Release:** **78 comprobaciones, 0 fallos**; incluye base, reproducción oficial, ocultación, API de volumen/intensidad, reanudación, sustitución de vídeo, detención y salida desde menú de bandeja.
- **Limpieza:** consulta de procesos tras la prueba encontró **0** procesos del ejecutable de tests o de WebView2 asociados a sus perfiles temporales. El DLL publicado coincide por SHA-256 con el Release probado.
- La prueba Win32 independiente comprobó padre WorkerW, estilos de superficie pasiva/sin barra ni Alt+Tab y destrucción del HWND propio. El contenedor de iconos encontrado fue Progman.

La prueba inicial de mensajes a Explorer falló dentro del entorno restringido y pasó al ejecutarse en la sesión normal. Una corrida inicial presentó un fallo transitorio en los ajustes; se mejoró el uso compartido del archivo de lectura y el reemplazo ante bloqueos breves, y las suites finales pasaron.

El éxito de los eventos de YouTube y de la jerarquía Win32 **no sustituye la comprobación visual/interactiva del escritorio**. La fase B está validada parcialmente; el MVP no se presenta como certificado para todos los builds de Windows 11.

## Matriz de aceptación

PASS se limita a la evidencia indicada. NO PROBADO significa que falta completar el escenario; no implica que el código correspondiente no exista.

| ID | Resultado | Evidencia o pendiente |
|---|---|---|
| T01 | PASS | Panel mostrado programáticamente; host inactivo hasta pulsar Aplicar. |
| T02 | NO PROBADO | Vídeo real emite playing y su HWND está acoplado a WorkerW inferior al contenedor de iconos. Falta confirmar visualmente la imagen detrás de iconos. |
| T03 | PASS | Enlace youtu.be normalizado y reproducido al sustituir el contenido por otro vídeo real. |
| T04 | NO PROBADO | Parser admite playlists y vídeo+list; la API configura secuencia/repetición. No se comprobó avance real al siguiente vídeo. |
| T05 | PASS | 17 entradas inseguras/inválidas rechazadas; botón Aplicar con javascript muestra error sin crear host. |
| T06 | NO PROBADO | Mensajes específicos para 101/150 y otros errores implementados. Falta probar contenido realmente restringido. |
| T07 | NO PROBADO | HWND deshabilitado y capa inferior comprobados. No se hicieron clics físicos sobre iconos. |
| T08 | NO PROBADO | La prueba reprodujo con WebView2 propio, sin usar Zen. Falta ejecutar el ciclo de abrir/cambiar/cerrar Zen. |
| T09 | NO PROBADO | Host hijo del escritorio y sin activación; falta abrir VS Code/PDF y verificar la superposición visual. |
| T10 | PASS | X ocultó el panel, NotifyIcon permaneció visible y getPlayerState confirmó playing después de la ocultación. |
| T11 | NO PROBADO | Reapertura comprobada mediante la misma función y el menú. Falta doble clic físico en el icono de Windows. |
| T12 | PASS | Menú invocado programáticamente: abrir, reanudar tras pausa oficial, aplicar otra URL, detener y salir. Apertura del menú con clic derecho físico pendiente. |
| T13 | PASS | Sliders 0/20/100 verificados mediante getVolume e isMuted de YouTube. Código sin API de volumen maestro. No es una comprobación auditiva. |
| T14 | NO PROBADO | Filtros brightness(0), brightness(0.5) y brightness(1) comprobados con getComputedStyle. Falta valorar visualmente el efecto sobre vídeo; no hay transparencia real. |
| T15 | PASS | Detener desde bandeja dispuso WebView2/form, destruyó el HWND propio y mantuvo NotifyIcon. Restauración visual del fondo pendiente. |
| T16 | PASS | Cambio entre dos vídeos reales; primera superficie dispuesta y una sola WallpaperForm activa tras reemplazo. |
| T17 | PASS | Salir desde menú dispuso el panel/host y finalizó la prueba. Consulta posterior: 0 procesos de tests/WebView2 vinculados al perfil temporal. |
| T18 | NO PROBADO | Persistencia y recuperación de URL/volumen/intensidad pasaron; arranque sin autoplay pasó. Falta reabrir el exe publicado tras una salida manual. |
| T19 | PASS | JSON dañado, versión desconocida y valores fuera de rango recuperaron defaults; archivo bloqueado produjo error recuperable. |
| T20 | NO PROBADO | Detección y reajuste de dimensiones/DPI; pérdida/reordenación de capa libera host y pide Aplicar. No se cambió resolución ni se reinició Explorer. |
| T21 | PASS | Release sin advertencias/errores y publicación generada; instrucciones de uso y distribución en README. |
| T22 | PASS | Base preservada y cambios versionados localmente; ignores y diff comprobados. Sin remoto ni publicación externa. |

## Comandos ejecutados

La solución .slnx se construyó con SDK **10.0.401** y target **net8.0-windows**.

~~~powershell
dotnet restore YoutubeWallpaper.slnx --ignore-failed-sources -p:NuGetAudit=false
dotnet build YoutubeWallpaper.slnx --no-restore -c Debug -m:1
dotnet build YoutubeWallpaper.slnx --no-restore -c Release -m:1
dotnet publish YoutubeWallpaper/YoutubeWallpaper.csproj -c Release --no-restore --self-contained false -o artifacts/publish
dotnet run --project YoutubeWallpaper.Tests --no-build
dotnet run --project YoutubeWallpaper.Tests --no-build -- --ui
dotnet run --project YoutubeWallpaper.Tests --no-build -- --desktop
dotnet run --project YoutubeWallpaper.Tests --no-build -c Release -- --playback https://www.youtube.com/watch?v=M7lc1UVf-VE
git diff --check
~~~

Para este entorno se usaron DOTNET_CLI_HOME dentro de .build, la caché NuGet ya instalada y MSBuildEnableWorkloadResolver=false. La desactivación de NuGetAudit fue exclusiva del restore local sin DNS; no quedó configurada en el proyecto.

La prueba de reproducción inició con el vídeo de ejemplo de la documentación de la IFrame API y sustituyó el contenido por [otro vídeo embebible](https://youtu.be/aqz-KE-bpKQ). Usó los métodos oficiales para observar volumen, silencio, estado y pausa. Terminó desde Salir del menú propio. Los perfiles y ajustes de prueba se aislaron bajo .build/tests.

## Archivos relevantes

| Ruta | Responsabilidad |
|---|---|
| YoutubeWallpaper/Form1.cs, Form1.Designer.cs | Panel compacto, valores, estado y X=ocultar. |
| YoutubeWallpaper/Program.cs | Arranque sin reproducción, mutex y señal para instancia existente. |
| YoutubeWallpaper/Wallpaper/DesktopInterop.cs | Descubrimiento y comprobaciones Win32, estilos, parenting, DPI y tamaño. |
| YoutubeWallpaper/Wallpaper/WallpaperHost.cs | Aplicar/reemplazar/detener, cancelación, timeout y monitor del escritorio. |
| YoutubeWallpaper/Wallpaper/WallpaperForm.cs | WebView2 separado y superficie pasiva. |
| YoutubeWallpaper/Playback/YouTubeUrlParser.cs | Lista de hosts/rutas, IDs, parámetros y URL canónica. |
| YoutubeWallpaper/Playback/YouTubePlayer.cs, PlayerAssets/ | Host virtual, API oficial, eventos, errores, volumen y brillo. |
| YoutubeWallpaper/App/TrayController.cs | NotifyIcon y menú persistentes. |
| YoutubeWallpaper/App/AppSettings.cs, SettingsStore.cs, LocalDiagnostics.cs | Ajustes locales y diagnósticos sin URLs sensibles. |
| YoutubeWallpaper/Resources/app.ico, tools/Create-AppIcon.ps1 | Icono propio empaquetado y generación reproducible. |
| YoutubeWallpaper.Tests/Program.cs | Validación ejecutable sin nueva dependencia NuGet. |

## Comprobación manual pendiente

1. Abre el exe publicado y aplica un enlace embebible. Confirma imagen detrás de iconos.
2. Selecciona y abre un icono del escritorio; abre una ventana normal sobre el fondo. Revisa Alt+Tab.
3. Cierra el panel; localiza el icono en la barra o ^, haz doble clic y abre el menú con clic derecho.
4. Reproduce una playlist hasta que avance. Prueba contenido con inserción restringida.
5. Observa intensidad 0/50/100; detén y comprueba que reaparece el fondo original sin capa negra.
6. Cambia resolución/DPI y, cuando sea oportuno, prueba recreación de Explorer. La respuesta prevista ante pérdida de capa es liberación y error recuperable.
7. Sal y reabre el exe. Debe recuperar ajustes y permanecer inactivo hasta Aplicar. Comprueba también una segunda ejecución mientras está activo.

No se modificaron el registro, el fondo estático, el inicio de Windows, la configuración de barra de tareas ni las preferencias de Explorer. No se publicaron archivos en GitHub. La migración a .NET 10, transparencia real y validación en otros builds de Windows quedan como trabajos posteriores.
