$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$iconPath = Join-Path $PSScriptRoot '..\YoutubeWallpaper\Resources\app.ico'
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($iconPath)) | Out-Null
$frames = @()
foreach ($size in @(16, 32, 48, 256)) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $background = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(27, 42, 58))
    $accent = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(126, 200, 250))
    $graphics.FillEllipse($background, 0, 0, $size - 1, $size - 1)
    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($size * 0.39, $size * 0.27),
        [System.Drawing.PointF]::new($size * 0.73, $size * 0.5),
        [System.Drawing.PointF]::new($size * 0.39, $size * 0.73)
    )
    $graphics.FillPolygon($accent, $points)
    $buffer = [System.IO.MemoryStream]::new()
    $bitmap.Save($buffer, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames += ,@{ Size = $size; Data = $buffer.ToArray() }
    $buffer.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    $background.Dispose()
    $accent.Dispose()
}
$file = [System.IO.File]::Create($iconPath)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frame.Data.Length)
        $writer.Write([uint32]$offset)
        $offset += $frame.Data.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Data) }
}
finally { $writer.Dispose(); $file.Dispose() }
