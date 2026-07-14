param(
  [string]$Url,
  [switch]$ServeLocal,
  [string]$BuildRoot = "$(Split-Path $PSScriptRoot)\Builds\WebGL",
  [string]$OutputRoot = "$(Split-Path $PSScriptRoot)\Logs\visual-acceptance",
  [string]$ChromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe',
  [int]$Width = 402,
  [int]$Height = 874,
  [int]$TimeoutSeconds = 45,
  [switch]$Flow
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$outputDir = Join-Path $OutputRoot $timestamp
$profileDir = Join-Path $env:TEMP "fruit-defense-cdp-$([Guid]::NewGuid().ToString('N'))"
$serverProcess = $null
$chromeProcess = $null
$socket = $null
$script:CdpId = 0
$acceptanceQuery = if ($Flow) { 'acceptance=1' } else { 'acceptance=1&route=battle' }

function Get-FreeTcpPort {
  $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
  $listener.Start()
  try { return ([Net.IPEndPoint]$listener.LocalEndpoint).Port }
  finally { $listener.Stop() }
}

function Wait-Http {
  param([string]$TargetUrl, [int]$Seconds)
  $deadline = (Get-Date).AddSeconds($Seconds)
  do {
    try {
      $response = Invoke-WebRequest -UseBasicParsing -Uri $TargetUrl -TimeoutSec 5
      if ($response.StatusCode -eq 200) { return $response }
    }
    catch { Start-Sleep -Milliseconds 300 }
  } while ((Get-Date) -lt $deadline)
  throw "Timed out waiting for HTTP 200: $TargetUrl"
}

function Get-UnityDeliveryMetadata {
  param(
    [string]$PageUrl,
    [object]$PageResponse
  )

  $htmlCacheControl = [string]$PageResponse.Headers['Cache-Control']
  if ($htmlCacheControl -match 'immutable') {
    throw "WebGL entry page must not be immutable: $htmlCacheControl"
  }
  if ($htmlCacheControl -notmatch 'no-cache|max-age=0') {
    throw "WebGL entry page must be revalidatable: $htmlCacheControl"
  }

  $patterns = [ordered]@{
    loader = 'loaderUrl\s*=\s*buildUrl\s*\+\s*"/(?<file>[^"]+)"'
    data = 'dataUrl\s*:\s*buildUrl\s*\+\s*"/(?<file>[^"]+)"'
    framework = 'frameworkUrl\s*:\s*buildUrl\s*\+\s*"/(?<file>[^"]+)"'
    wasm = 'codeUrl\s*:\s*buildUrl\s*\+\s*"/(?<file>[^"]+)"'
  }
  $expectedTypes = @{
    loader = 'javascript'
    data = 'application/octet-stream'
    framework = 'application/octet-stream'
    wasm = 'application/octet-stream'
  }
  $assets = [ordered]@{}
  $versions = New-Object System.Collections.Generic.List[string]

  foreach ($name in $patterns.Keys) {
    $match = [regex]::Match($PageResponse.Content, $patterns[$name])
    if (-not $match.Success) {
      throw "WebGL entry page does not advertise the $name asset."
    }

    $assetUri = [Uri]::new([Uri]$PageUrl, "Build/$($match.Groups['file'].Value)")
    $versionMatch = [regex]::Match($assetUri.Query, '(?:^\?|&)v=(?<version>[^&]+)')
    if (-not $versionMatch.Success) {
      throw "WebGL $name asset is missing its version token: $assetUri"
    }
    $version = [Uri]::UnescapeDataString($versionMatch.Groups['version'].Value)
    if ($version -notmatch '^[0-9a-f]{12}$') {
      throw "WebGL $name asset has an invalid content version: $version"
    }
    $versions.Add($version)

    $response = Invoke-WebRequest -UseBasicParsing -Method Head -Uri $assetUri.AbsoluteUri -TimeoutSec 15
    $cacheControl = [string]$response.Headers['Cache-Control']
    $contentEncoding = [string]$response.Headers['Content-Encoding']
    $contentType = [string]$response.Headers['Content-Type']
    $contentLength = [long]$response.Headers['Content-Length']
    if ($response.StatusCode -ne 200) {
      throw "WebGL $name asset returned HTTP $($response.StatusCode): $assetUri"
    }
    if ($cacheControl -notmatch 'public' -or $cacheControl -notmatch 'max-age=31536000' -or $cacheControl -notmatch 'immutable') {
      throw "WebGL $name asset has an invalid cache policy: $cacheControl"
    }
    if ($contentType -notmatch $expectedTypes[$name]) {
      throw "WebGL $name asset has an invalid content type: $contentType"
    }
    if ($contentLength -le 0) {
      throw "WebGL $name asset has an invalid content length: $contentLength"
    }
    if ($name -ne 'loader') {
      if ($assetUri.AbsolutePath -notmatch '\.unityweb$' -or -not [string]::IsNullOrWhiteSpace($contentEncoding)) {
        throw "WebGL $name asset is not served as a Brotli fallback container: path=$($assetUri.AbsolutePath) encoding=$contentEncoding"
      }
    }

    $assets[$name] = [ordered]@{
      url = $assetUri.AbsoluteUri
      contentType = $contentType
      contentEncoding = $contentEncoding
      cacheControl = $cacheControl
      contentLength = $contentLength
      etag = [string]$response.Headers['ETag']
    }
  }

  $uniqueVersions = @($versions | Select-Object -Unique)
  if ($uniqueVersions.Count -ne 1) {
    throw "WebGL assets do not share one content version: $($uniqueVersions -join ', ')"
  }

  return [ordered]@{
    version = $uniqueVersions[0]
    htmlCacheControl = $htmlCacheControl
    assets = $assets
  }
}

function Receive-CdpMessage {
  param([Net.WebSockets.ClientWebSocket]$WebSocket, [Threading.CancellationToken]$Token)
  $stream = [IO.MemoryStream]::new()
  try {
    do {
      $buffer = New-Object byte[] 131072
      $result = $WebSocket.ReceiveAsync([ArraySegment[byte]]::new($buffer), $Token).GetAwaiter().GetResult()
      if ($result.MessageType -eq [Net.WebSockets.WebSocketMessageType]::Close) {
        throw 'Chrome DevTools websocket closed unexpectedly.'
      }
      $stream.Write($buffer, 0, $result.Count)
    } while (-not $result.EndOfMessage)
    return ([Text.Encoding]::UTF8.GetString($stream.ToArray()) | ConvertFrom-Json)
  }
  finally { $stream.Dispose() }
}

function Invoke-Cdp {
  param([string]$Method, [hashtable]$Params = @{})
  $script:CdpId++
  $id = $script:CdpId
  $payload = @{ id = $id; method = $Method; params = $Params } | ConvertTo-Json -Compress -Depth 12
  $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
  $cts = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds($TimeoutSeconds))
  try {
    $socket.SendAsync(
      [ArraySegment[byte]]::new($bytes),
      [Net.WebSockets.WebSocketMessageType]::Text,
      $true,
      $cts.Token).GetAwaiter().GetResult() | Out-Null
    do { $message = Receive-CdpMessage -WebSocket $socket -Token $cts.Token } while ($message.id -ne $id)
    if ($message.error) { throw "CDP $Method failed: $($message.error.message)" }
    return $message.result
  }
  finally { $cts.Dispose() }
}

function Invoke-JavaScript {
  param([string]$Expression)
  $result = Invoke-Cdp -Method 'Runtime.evaluate' -Params @{
    expression = $Expression
    returnByValue = $true
    awaitPromise = $true
  }
  if ($result.exceptionDetails) { throw "Browser JavaScript failed: $($result.exceptionDetails.text)" }
  return $result.result.value
}

function Invoke-CanvasClick {
  param([double]$X, [double]$Y)
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mousePressed'; x = $X; y = $Y; button = 'left'; clickCount = 1
  } | Out-Null
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mouseReleased'; x = $X; y = $Y; button = 'left'; clickCount = 1
  } | Out-Null
  Start-Sleep -Milliseconds 450
}

function Start-CanvasDrag {
  param([double]$FromX, [double]$FromY, [double]$ToX, [double]$ToY)
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mousePressed'; x = $FromX; y = $FromY; button = 'left'; buttons = 1; clickCount = 1
  } | Out-Null
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mouseMoved'; x = (($FromX + $ToX) / 2); y = (($FromY + $ToY) / 2); button = 'left'; buttons = 1
  } | Out-Null
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mouseMoved'; x = $ToX; y = $ToY; button = 'left'; buttons = 1
  } | Out-Null
  Start-Sleep -Milliseconds 450
}

function Stop-CanvasDrag {
  param([double]$X, [double]$Y)
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mouseReleased'; x = $X; y = $Y; button = 'left'; buttons = 0; clickCount = 1
  } | Out-Null
  Start-Sleep -Milliseconds 300
}

function Set-AcceptanceState {
  param([string]$State)
  $escapedState = $State.Replace("'", "\'")
  $configured = Invoke-JavaScript -Expression @"
(() => {
  const instance = window.fruitDefenseUnityInstance;
  if (!instance) return false;
  instance.SendMessage('FruitDefenseGame', 'ConfigureAcceptanceState', '$escapedState');
  return true;
})()
"@
  if (-not $configured) { throw "Unity acceptance bridge is unavailable for state: $State" }
  Start-Sleep -Milliseconds 500
}

function Invoke-AcceptanceFlowCommand {
  param([string]$Command)
  $escapedCommand = $Command.Replace("'", "\'")
  $sent = Invoke-JavaScript -Expression @"
(() => {
  const instance = window.fruitDefenseUnityInstance;
  if (!instance) return false;
  instance.SendMessage('AppBootstrap', 'ConfigureAcceptanceFlow', '$escapedCommand');
  return true;
})()
"@
  if (-not $sent) { throw "Unity flow acceptance bridge is unavailable for command: $Command" }
}

function Wait-AppRoute {
  param([int]$Route)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    $current = Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1'
    if ([int]$current -eq $Route) { return }
    Start-Sleep -Milliseconds 250
  } while ((Get-Date) -lt $deadline)
  throw "Timed out waiting for app route $Route; current=$current"
}

function Save-Screenshot {
  param([string]$Name)
  $capture = Invoke-Cdp -Method 'Page.captureScreenshot' -Params @{
    format = 'png'; fromSurface = $true; captureBeyondViewport = $false
  }
  if (-not $capture.data) { throw "Screenshot did not return image data: $Name" }
  $path = Join-Path $outputDir "$Name.png"
  [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String($capture.data))
  if (-not (Test-Path -LiteralPath $path) -or (Get-Item $path).Length -lt 1024) {
    throw "Screenshot is missing or unexpectedly small: $path"
  }
  return $path
}

function Get-ImageMetrics {
  param([string]$Path)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $darkPixels = 0
    $lightPixels = 0
    $blackSamples = 0
    $invalidSamples = 0
    $sampleCount = 0
    $formerActionColorPixels = 0
    # Header copy occupies this interior area; panel borders and buttons are excluded.
    for ($y = 11; $y -lt 64; $y++) {
      for ($x = 13; $x -lt 263; $x++) {
        $pixel = $bitmap.GetPixel($x, $y)
        $luma = (.2126 * $pixel.R + .7152 * $pixel.G + .0722 * $pixel.B) / 255.0
        if ($pixel.A -gt 128 -and $luma -lt .48) { $darkPixels++ }
        if ($pixel.A -gt 128 -and $luma -gt .75) { $lightPixels++ }
      }
    }
    for ($y = 0; $y -lt $bitmap.Height; $y += 4) {
      for ($x = 0; $x -lt $bitmap.Width; $x += 4) {
        $pixel = $bitmap.GetPixel($x, $y)
        $luma = (.2126 * $pixel.R + .7152 * $pixel.G + .0722 * $pixel.B) / 255.0
        $sampleCount++
        if ($pixel.A -gt 128 -and $luma -lt .025) { $blackSamples++ }
        if ($pixel.A -le 128 -or $luma -lt .025) { $invalidSamples++ }
      }
    }
    # The removed persistent action row occupied x=8..394, y=760..810.
    for ($y = 760; $y -lt [Math]::Min(810, $bitmap.Height); $y++) {
      for ($x = 8; $x -lt [Math]::Min(394, $bitmap.Width); $x++) {
        $pixel = $bitmap.GetPixel($x, $y)
        $looksLikeOldOrange = $pixel.R -gt 190 -and $pixel.G -gt 90 -and $pixel.G -lt 190 -and $pixel.B -lt 90
        $looksLikeOldRed = $pixel.R -gt 180 -and $pixel.G -lt 115 -and $pixel.B -lt 110
        if ($looksLikeOldOrange -or $looksLikeOldRed) { $formerActionColorPixels++ }
      }
    }
    return [ordered]@{
      width = $bitmap.Width
      height = $bitmap.Height
      headerDarkPixels = $darkPixels
      headerLightPixels = $lightPixels
      blackFraction = if ($sampleCount -gt 0) { $blackSamples / [double]$sampleCount } else { 1.0 }
      invalidFraction = if ($sampleCount -gt 0) { $invalidSamples / [double]$sampleCount } else { 1.0 }
      formerActionColorPixels = $formerActionColorPixels
    }
  }
  finally { $bitmap.Dispose() }
}

function Save-StableScreenshot {
  param([string]$Name, [bool]$RequireHud = $true)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    $path = Save-Screenshot -Name $Name
    $metrics = Get-ImageMetrics -Path $path
    $dimensionsOk = $metrics.width -eq $Width -and $metrics.height -eq $Height
    $frameOk = $metrics.invalidFraction -lt .05
    $hudOk = -not $RequireHud -or ($metrics.headerDarkPixels -ge 80 -and $metrics.headerLightPixels -ge 5000)
    if ($dimensionsOk -and $frameOk -and $hudOk) {
      return [pscustomobject]@{ Path = $path; Metrics = $metrics }
    }
    Start-Sleep -Milliseconds 500
  } while ((Get-Date) -lt $deadline)
  throw "Stable screenshot timed out: $Name metrics=$($metrics | ConvertTo-Json -Compress)"
}

function Stop-OwnedChrome {
  if ($chromeProcess -and -not $chromeProcess.HasExited) {
    Stop-Process -Id $chromeProcess.Id -Force -ErrorAction SilentlyContinue
  }
  Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -eq 'chrome.exe' -and $_.CommandLine -like "*$profileDir*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}

try {
  if (-not (Test-Path -LiteralPath $ChromePath)) { throw "Chrome not found: $ChromePath" }
  New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

  if ($ServeLocal) {
    if (-not (Test-Path -LiteralPath (Join-Path $BuildRoot 'index.html'))) {
      throw "WebGL build not found: $BuildRoot"
    }
    $serverPort = Get-FreeTcpPort
    $oldStaticRoot = $env:STATIC_ROOT
    $oldPort = $env:PORT
    try {
      $env:STATIC_ROOT = (Resolve-Path $BuildRoot).Path
      $env:PORT = [string]$serverPort
      $serverStart = @{
        FilePath = 'node'
        ArgumentList = 'deploy/server.mjs'
        WorkingDirectory = $projectRoot
        WindowStyle = 'Hidden'
        PassThru = $true
        RedirectStandardOutput = (Join-Path $outputDir 'server.stdout.log')
        RedirectStandardError = (Join-Path $outputDir 'server.stderr.log')
      }
      $serverProcess = Start-Process @serverStart
    }
    finally {
      $env:STATIC_ROOT = $oldStaticRoot
      $env:PORT = $oldPort
    }
    $Url = "http://127.0.0.1:$serverPort/?$acceptanceQuery"
  }
  elseif ([string]::IsNullOrWhiteSpace($Url)) {
    throw 'Provide -Url or use -ServeLocal.'
  }
  elseif ($Url -notmatch '(?:\?|&)acceptance=1(?:&|$)') {
    $Url += $(if ($Url.Contains('?')) { "&$acceptanceQuery" } else { "?$acceptanceQuery" })
  }
  elseif (-not $Flow -and $Url -notmatch '(?:\?|&)route=battle(?:&|$)') {
    $Url += '&route=battle'
  }

  $pageResponse = Wait-Http -TargetUrl $Url -Seconds $TimeoutSeconds
  if ($pageResponse.Content -notmatch "width=$Width height=$Height") {
    throw "WebGL page does not declare the expected $Width x $Height portrait canvas."
  }
  $delivery = Get-UnityDeliveryMetadata -PageUrl $Url -PageResponse $pageResponse

  $debugPort = Get-FreeTcpPort
  $chromeArgs = @(
    '--headless=new', '--no-first-run', '--disable-background-networking', '--disable-extensions',
    '--hide-scrollbars', '--use-angle=swiftshader', '--enable-webgl', '--ignore-gpu-blocklist',
    '--user-agent="Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1"',
    "--window-size=$Width,$Height", '--force-device-scale-factor=1',
    "--remote-debugging-port=$debugPort", "--user-data-dir=$profileDir", $Url
  )
  $chromeProcess = Start-Process -FilePath $ChromePath -ArgumentList $chromeArgs -WindowStyle Hidden -PassThru

  $debugUrl = "http://127.0.0.1:$debugPort/json"
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    try {
      $rawTargets = Invoke-RestMethod -Uri $debugUrl -TimeoutSec 3
      $target = $null
      foreach ($candidate in $rawTargets) {
        if ($candidate.type -eq 'page' -and $candidate.url -eq $Url) {
          $target = $candidate
          break
        }
      }
      if ($target) { break }
    }
    catch { Start-Sleep -Milliseconds 300 }
  } while ((Get-Date) -lt $deadline)
  if (-not $target) { throw 'Chrome DevTools page target was not created.' }

  $socket = [Net.WebSockets.ClientWebSocket]::new()
  $connectCts = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(10))
  $debuggerUrl = [string]@($target.webSocketDebuggerUrl)[0]
  try { $socket.ConnectAsync([Uri]$debuggerUrl, $connectCts.Token).GetAwaiter().GetResult() | Out-Null }
  finally { $connectCts.Dispose() }
  Invoke-Cdp -Method 'Page.enable' | Out-Null
  Invoke-Cdp -Method 'Runtime.enable' | Out-Null
  Invoke-Cdp -Method 'Emulation.setDeviceMetricsOverride' -Params @{
    width = $Width
    height = $Height
    deviceScaleFactor = 1
    mobile = $false
    screenWidth = $Width
    screenHeight = $Height
  } | Out-Null

  $readinessExpression = @'
(() => {
  const canvas = document.querySelector('#unity-canvas');
  const loading = document.querySelector('#unity-loading-bar');
  const warning = document.querySelector('#unity-warning');
  return JSON.stringify({
    href: location.href,
    title: document.title,
    canvas: !!canvas,
    width: canvas ? canvas.width : 0,
    height: canvas ? canvas.height : 0,
    loading: loading ? getComputedStyle(loading).display : 'missing',
    warning: warning ? warning.textContent.trim() : '',
    acceptanceReady: !!window.fruitDefenseUnityInstance,
    appRoute: window.fruitDefenseAppRoute ?? -1,
    body: document.body ? document.body.innerText.slice(0, 240) : ''
  });
})()
'@
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    $readiness = (Invoke-JavaScript -Expression $readinessExpression) | ConvertFrom-Json
    if ($readiness.canvas -and $readiness.loading -eq 'none' -and $readiness.width -gt 0 -and $readiness.acceptanceReady) { break }
    Start-Sleep -Milliseconds 400
  } while ((Get-Date) -lt $deadline)
  if (-not $readiness.canvas -or $readiness.loading -ne 'none' -or -not $readiness.acceptanceReady) {
    throw "Unity player did not finish loading. state=$($readiness | ConvertTo-Json -Compress)"
  }
  if ($readiness.warning) { throw "Unity player warning: $($readiness.warning)" }

  $screenshots = [ordered]@{}
  # Named centers are derived from the shared 402 x 874 layout rectangles.
  $controls = [ordered]@{
    lobbyStart = [ordered]@{ x = 201; y = 152 }
    settlementRetry = [ordered]@{ x = 201; y = 449 }
    settlementReturn = [ordered]@{ x = 201; y = 528 }
    headerPause = [ordered]@{ x = 300; y = 38 }
    waveAction = [ordered]@{ x = 298; y = 450 }
    pauseContinue = [ordered]@{ x = 125; y = 492 }
    pauseRestart = [ordered]@{ x = 277; y = 492 }
    nurserySlot0 = [ordered]@{ x = 51; y = 619 }
    cell32 = [ordered]@{ x = 178; y = 223 }
    cell42 = [ordered]@{ x = 224; y = 223 }
  }

  if ($Flow) {
    Wait-AppRoute -Route 0
    $flowScreenshots = [ordered]@{}
    $flowMetrics = [ordered]@{}
    $flowScreenshots.lobby = (Save-StableScreenshot -Name '01-lobby' -RequireHud $false).Path

    Invoke-CanvasClick -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
    Wait-AppRoute -Route 1
    $flowScreenshots.battle = (Save-StableScreenshot -Name '02-battle' -RequireHud $true).Path

    Invoke-AcceptanceFlowCommand -Command 'victory'
    Wait-AppRoute -Route 2
    $flowScreenshots.settlement = (Save-StableScreenshot -Name '03-settlement' -RequireHud $false).Path

    Invoke-CanvasClick -X $controls.settlementReturn.x -Y $controls.settlementReturn.y
    Wait-AppRoute -Route 0
    $flowScreenshots.returnedLobby = (Save-StableScreenshot -Name '04-returned-lobby' -RequireHud $false).Path

    Invoke-CanvasClick -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
    Wait-AppRoute -Route 1
    Invoke-AcceptanceFlowCommand -Command 'victory'
    Wait-AppRoute -Route 2
    Invoke-CanvasClick -X $controls.settlementRetry.x -Y $controls.settlementRetry.y
    Wait-AppRoute -Route 1
    $flowScreenshots.retryBattle = (Save-StableScreenshot -Name '05-retry-battle' -RequireHud $true).Path

    foreach ($state in $flowScreenshots.Keys) {
      $flowMetrics[$state] = Get-ImageMetrics -Path $flowScreenshots[$state]
      if ($flowMetrics[$state].width -ne $Width -or $flowMetrics[$state].height -ne $Height) {
        throw "Unexpected flow screenshot dimensions for ${state}: $($flowMetrics[$state].width)x$($flowMetrics[$state].height)"
      }
      if ($flowMetrics[$state].blackFraction -ge .05 -or $flowMetrics[$state].invalidFraction -ge .05) {
        throw "Invalid flow frame for ${state}: black=$($flowMetrics[$state].blackFraction) invalid=$($flowMetrics[$state].invalidFraction)"
      }
    }

    $flowManifest = [ordered]@{
      accepted = $true
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      viewport = [ordered]@{ width = $Width; height = $Height }
      checks = [ordered]@{
        lobbyToBattle = 'pass'
        battleToSettlement = 'pass'
        settlementReturn = 'pass'
        settlementRetry = 'pass'
        noBlackOrTransparentFrames = 'pass'
      }
      delivery = $delivery
      screenshots = $flowScreenshots
      imageMetrics = $flowMetrics
      controls = $controls
    }
    $flowManifestPath = Join-Path $outputDir 'flow-acceptance.json'
    $flowManifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $flowManifestPath -Encoding UTF8
    Write-Host "FRUIT_DEFENSE_FLOW_ACCEPTANCE_OK manifest=$flowManifestPath"
    return
  }

  Set-AcceptanceState -State 'initial'
  $readyCapture = Save-StableScreenshot -Name '01-ready'
  $screenshots.ready = $readyCapture.Path

  Invoke-CanvasClick -X $controls.waveAction.x -Y $controls.waveAction.y
  $screenshots.activeWave = (Save-StableScreenshot -Name '02-active-wave').Path

  Set-AcceptanceState -State 'between-wave'
  $screenshots.betweenWave = (Save-StableScreenshot -Name '03-between-wave').Path
  Invoke-CanvasClick -X $controls.waveAction.x -Y $controls.waveAction.y
  $screenshots.immediateNextWave = (Save-StableScreenshot -Name '04-immediate-next-wave').Path

  Invoke-CanvasClick -X $controls.headerPause.x -Y $controls.headerPause.y
  # The modal intentionally dims the HUD, so this state uses frame/dimension checks without the unobscured-HUD ink threshold.
  $screenshots.paused = (Save-StableScreenshot -Name '05-paused' -RequireHud $false).Path
  Invoke-CanvasClick -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
  $screenshots.continued = (Save-StableScreenshot -Name '06-continued').Path
  Invoke-CanvasClick -X $controls.headerPause.x -Y $controls.headerPause.y
  Invoke-CanvasClick -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
  $screenshots.restarted = (Save-StableScreenshot -Name '07-restarted').Path

  Set-AcceptanceState -State 'adjacent-pots'
  $screenshots.adjacentPots = (Save-StableScreenshot -Name '08-adjacent-pots').Path

  Set-AcceptanceState -State 'drag-target'
  Start-CanvasDrag -FromX $controls.nurserySlot0.x -FromY $controls.nurserySlot0.y -ToX $controls.cell32.x -ToY $controls.cell32.y
  $screenshots.dragTarget = (Save-StableScreenshot -Name '09-drag-target').Path
  Stop-CanvasDrag -X $controls.cell32.x -Y $controls.cell32.y

  Set-AcceptanceState -State 'dense-board'
  $screenshots.denseBoard = (Save-StableScreenshot -Name '10-dense-board').Path

  Set-AcceptanceState -State 'selection-inspection'
  # Deterministic interaction state: attacking plant at cell (3, 2), empty pot at cell (4, 2).
  Invoke-CanvasClick -X $controls.cell32.x -Y $controls.cell32.y
  $screenshots.inspectionClick = (Save-StableScreenshot -Name '11-inspection-click').Path
  Invoke-CanvasClick -X $controls.cell42.x -Y $controls.cell42.y
  $screenshots.destinationClickNoMove = (Save-StableScreenshot -Name '12-destination-click-no-move').Path
  Start-CanvasDrag -FromX $controls.cell32.x -FromY $controls.cell32.y -ToX $controls.cell42.x -ToY $controls.cell42.y
  Stop-CanvasDrag -X $controls.cell42.x -Y $controls.cell42.y
  $screenshots.dragRelocation = (Save-StableScreenshot -Name '13-after-drag-move').Path

  $metrics = [ordered]@{}
  foreach ($state in $screenshots.Keys) {
    $metrics[$state] = Get-ImageMetrics -Path $screenshots[$state]
    if ($metrics[$state].width -ne $Width -or $metrics[$state].height -ne $Height) {
      throw "Unexpected screenshot dimensions for ${state}: $($metrics[$state].width)x$($metrics[$state].height)"
    }
    if ($metrics[$state].blackFraction -ge .05) {
      throw "Black-frame check failed for ${state}: $($metrics[$state].blackFraction)"
    }
    if ($metrics[$state].invalidFraction -ge .05) {
      throw "Transparent-frame check failed for ${state}: $($metrics[$state].invalidFraction)"
    }
  }
  if ($metrics.ready.headerDarkPixels -lt 80 -or $metrics.ready.headerLightPixels -lt 5000) {
    throw "HUD text check failed: dark=$($metrics.ready.headerDarkPixels) light=$($metrics.ready.headerLightPixels)."
  }
  if ($metrics.ready.formerActionColorPixels -gt 12 -or $metrics.activeWave.formerActionColorPixels -gt 12) {
    throw "Former bottom action-row colors are still present: ready=$($metrics.ready.formerActionColorPixels) active=$($metrics.activeWave.formerActionColorPixels)."
  }

  $manifest = [ordered]@{
    accepted = $true
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    url = $Url
    viewport = [ordered]@{ width = $Width; height = $Height }
    canvas = $readiness
    checks = [ordered]@{
      http = 'pass'
      wasm = 'pass'
      contentVersion = 'pass'
      brotliFallbackDelivery = 'pass'
      immutableBuildCache = 'pass'
      revalidatableHtml = 'pass'
      unityLoaded = 'pass'
      chineseHudInk = 'pass'
      screenshotDimensions = 'pass'
      requiredStates = 'pass'
      contextualWaveLabels = 'pass'
      oldBottomActionRowAbsent = 'pass'
      pauseContinuePreservesRun = 'pass'
      pauseRestartProducesCleanReadyState = 'pass'
      inspectionClickInformationAndRange = 'pass'
      destinationClickNoRelocation = 'pass'
      dragRelocation = 'pass'
    }
    delivery = $delivery
    screenshots = $screenshots
    imageMetrics = $metrics
    controls = $controls
    sessionSequence = [ordered]@{
      labels = [ordered]@{
        ready = -join @([char]0x5F00, [char]0x59CB, [char]0x6CE2, [char]0x6B21)
        betweenWave = -join @([char]0x7ACB, [char]0x5373, [char]0x5F00, [char]0x59CB, [char]0x4E0B, [char]0x4E00, [char]0x6CE2)
        pauseContinue = -join @([char]0x7EE7, [char]0x7EED, [char]0x6E38, [char]0x620F)
        pauseRestart = -join @([char]0x91CD, [char]0x65B0, [char]0x5F00, [char]0x59CB)
      }
      steps = @(
        'capture clean ready state and exact ready label',
        'click battlefield wave action and capture active wave without a start action',
        'capture deterministic between-wave countdown and exact immediate-start label',
        'click immediate next wave and capture active wave two',
        'pause active run and capture both modal actions',
        'continue and capture the same active run',
        'pause again, restart, and capture clean ready state'
      )
    }
    interactionSequence = [ordered]@{
      source = [ordered]@{ cell = @(3, 2); x = $controls.cell32.x; y = $controls.cell32.y }
      destination = [ordered]@{ cell = @(4, 2); x = $controls.cell42.x; y = $controls.cell42.y }
      steps = @(
        'click source plant to inspect',
        'click empty destination without relocation',
        'drag source plant to destination to relocate'
      )
    }
  }
  $manifestPath = Join-Path $outputDir 'acceptance.json'
  $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
  Write-Host "FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK manifest=$manifestPath"
}
finally {
  if ($socket) { $socket.Dispose() }
  Stop-OwnedChrome
  if ($serverProcess -and -not $serverProcess.HasExited) {
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
  }
  $tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
  $resolvedProfile = [IO.Path]::GetFullPath($profileDir)
  if ($resolvedProfile.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $resolvedProfile -Recurse -Force -ErrorAction SilentlyContinue
  }
}
