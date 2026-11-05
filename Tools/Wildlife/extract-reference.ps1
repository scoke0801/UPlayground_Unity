param([string]$VideoPath,[string]$OutputPath)
Add-Type -AssemblyName PresentationCore,WindowsBase
$player=[System.Windows.Media.MediaPlayer]::new()
$player.Open([uri]([System.IO.Path]::GetFullPath($VideoPath)))
$player.Volume=0
$player.Play()
$timer=[System.Diagnostics.Stopwatch]::StartNew()
while (-not $player.NaturalDuration.HasTimeSpan -and $timer.Elapsed.TotalSeconds -lt 8) {
 [System.Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke([action]{},[System.Windows.Threading.DispatcherPriority]::Background)
 Start-Sleep -Milliseconds 50
}
if (-not $player.NaturalDuration.HasTimeSpan) {throw '동영상 디코더 준비 실패'}
$player.Pause()
foreach ($seconds in @(2,10,25,40)) {
 $player.Position=[timespan]::FromSeconds($seconds)
 $player.Play()
 $wait=[System.Diagnostics.Stopwatch]::StartNew()
 while ($wait.Elapsed.TotalSeconds -lt 1) {
  [System.Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke([action]{},[System.Windows.Threading.DispatcherPriority]::Background)
  Start-Sleep -Milliseconds 30
 }
 $player.Pause()
 Write-Output $player.Position.TotalSeconds
 $visual=[System.Windows.Media.DrawingVisual]::new()
 $drawing=$visual.RenderOpen()
 $drawing.DrawVideo($player,[System.Windows.Rect]::new(0,0,480,270))
 $drawing.Close()
 $bitmap=[System.Windows.Media.Imaging.RenderTargetBitmap]::new(480,270,96,96,[System.Windows.Media.PixelFormats]::Pbgra32)
 $bitmap.Render($visual)
 $encoder=[System.Windows.Media.Imaging.PngBitmapEncoder]::new()
 $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $stream=[System.IO.File]::Create((Join-Path $OutputPath ('frame-'+$seconds+'.png')))
 try {$encoder.Save($stream)} finally {$stream.Dispose()}
}
$player.Close()
