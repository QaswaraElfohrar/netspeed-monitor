# Generates src/NetSpeedMonitor.App/Assets/app.ico (multi-size, PNG frames). Run once; the .ico is committed.
Add-Type -AssemblyName System.Drawing
$out = Join-Path $PSScriptRoot '..\src\NetSpeedMonitor.App\Assets\app.ico'
$sizes = 16, 24, 32, 48, 64, 128, 256

function New-Frame([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $r = [int]($s * 0.22)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($s - $d - 1, 0, $d, $d, 270, 90)
    $path.AddArc($s - $d - 1, $s - $d - 1, $d, $d, 0, 90)
    $path.AddArc(0, $s - $d - 1, $d, $d, 90, 90)
    $path.CloseFigure()
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.Point]::new(0, 0)), ([System.Drawing.Point]::new(0, $s)), ([System.Drawing.Color]::FromArgb(255, 38, 50, 66)), ([System.Drawing.Color]::FromArgb(255, 18, 24, 33))
    $g.FillPath($bg, $path)

    $green = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 46, 204, 113))
    $blue = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 52, 152, 219))
    $u = $s / 100.0
    # down arrow (left), up arrow (right)
    $down = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(20 * $u, 20 * $u), [System.Drawing.PointF]::new(40 * $u, 20 * $u), [System.Drawing.PointF]::new(40 * $u, 50 * $u),
        [System.Drawing.PointF]::new(52 * $u, 50 * $u), [System.Drawing.PointF]::new(30 * $u, 80 * $u), [System.Drawing.PointF]::new(8 * $u, 50 * $u), [System.Drawing.PointF]::new(20 * $u, 50 * $u))
    $up = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(70 * $u, 80 * $u), [System.Drawing.PointF]::new(50 * $u, 80 * $u), [System.Drawing.PointF]::new(50 * $u, 50 * $u),
        [System.Drawing.PointF]::new(38 * $u, 50 * $u), [System.Drawing.PointF]::new(60 * $u, 20 * $u), [System.Drawing.PointF]::new(82 * $u, 50 * $u), [System.Drawing.PointF]::new(70 * $u, 50 * $u))
    # shift: draw down arrow at left-top region, up arrow at right
    $m = New-Object System.Drawing.Drawing2D.Matrix
    $m.Translate(5 * $u, 0)
    $g.FillPolygon($green, $down)
    $g.FillPolygon($blue, $up)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return , $ms.ToArray()
}

$frames = foreach ($s in $sizes) { , (New-Frame $s) }
$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $data = $frames[$i]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$data.Length); $bw.Write([uint32]$offset)
    $offset += $data.Length
}
foreach ($d in $frames) { $bw.Write($d) }
$bw.Close(); $fs.Close()
Write-Host "Wrote $out"
