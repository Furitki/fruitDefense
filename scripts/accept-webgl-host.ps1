param(
  [string]$BuildRoot = "$(Split-Path $PSScriptRoot)\Builds\WebGL",
  [string]$OutputDirectory = "$(Split-Path $PSScriptRoot)\Logs\webgl-host-acceptance",
  [string]$ChromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe',
  [ValidateRange(5, 180)]
  [int]$TimeoutSeconds = 60,
  [switch]$SourceSelfCheck
)

$ErrorActionPreference = 'Stop'
$logicalWidth = 402.0
$logicalHeight = 874.0
$hostId = 'fruit-defense-portrait-contain-v1'
$projectRoot = Split-Path $PSScriptRoot
$templateRoot = Join-Path $projectRoot 'Assets\WebGLTemplates\FruitDefensePortraitContain'
$script:CdpId = 0
$script:Socket = $null
$script:ChromeProcess = $null
$serverProcess = $null
$serverStdout = $null
$serverStderr = $null
$fixtureRoot = $null

function Assert-Condition {
  param([bool]$Condition, [string]$Message)
  if (-not $Condition) { throw $Message }
}

function Get-FreeTcpPort {
  $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
  $listener.Start()
  try { return ([Net.IPEndPoint]$listener.LocalEndpoint).Port }
  finally { $listener.Stop() }
}

function Wait-Http {
  param([string]$Url)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    try {
      $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 3
      if ($response.StatusCode -eq 200) { return }
    }
    catch { Start-Sleep -Milliseconds 200 }
  } while ((Get-Date) -lt $deadline)
  throw "Timed out waiting for host HTTP 200: $Url"
}

function Receive-CdpMessage {
  param([Net.WebSockets.ClientWebSocket]$WebSocket, [Threading.CancellationToken]$Token)
  $stream = [IO.MemoryStream]::new()
  try {
    do {
      $buffer = New-Object byte[] 131072
      $result = $WebSocket.ReceiveAsync(
        [ArraySegment[byte]]::new($buffer),
        $Token).GetAwaiter().GetResult()
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
  $payload = @{ id = $id; method = $Method; params = $Params } |
    ConvertTo-Json -Compress -Depth 12
  $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
  $cts = [Threading.CancellationTokenSource]::new(
    [TimeSpan]::FromSeconds($TimeoutSeconds))
  try {
    $script:Socket.SendAsync(
      [ArraySegment[byte]]::new($bytes),
      [Net.WebSockets.WebSocketMessageType]::Text,
      $true,
      $cts.Token).GetAwaiter().GetResult() | Out-Null
    do {
      $message = Receive-CdpMessage -WebSocket $script:Socket -Token $cts.Token
      $messageId = $message.PSObject.Properties['id']
    } while ($null -eq $messageId -or [int]$messageId.Value -ne $id)
    if ($null -ne $message.PSObject.Properties['error']) {
      throw "CDP $Method failed: $($message.error | ConvertTo-Json -Compress)"
    }
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
  if ($null -ne $result.PSObject.Properties['exceptionDetails']) {
    throw "Browser JavaScript failed: $($result.exceptionDetails | ConvertTo-Json -Compress)"
  }
  if ($null -eq $result.result.PSObject.Properties['value']) { return $null }
  return $result.result.value
}

function Stop-ChromeSession {
  if ($script:Socket) {
    try { $script:Socket.Dispose() } catch {}
    $script:Socket = $null
  }
  if ($script:ChromeProcess -and -not $script:ChromeProcess.HasExited) {
    Stop-Process -Id $script:ChromeProcess.Id -Force -ErrorAction SilentlyContinue
    $script:ChromeProcess.WaitForExit(5000) | Out-Null
  }
  $script:ChromeProcess = $null
}

function Start-ChromeSession {
  param([string]$InitialUrl, [string]$ProfileDirectory)
  Stop-ChromeSession
  $script:CdpId = 0
  $debugPort = Get-FreeTcpPort
  $arguments = @(
    '--headless=new', '--no-first-run', '--disable-background-networking', '--disable-extensions',
    '--hide-scrollbars', '--use-angle=swiftshader', '--enable-webgl', '--ignore-gpu-blocklist',
    '--force-device-scale-factor=1', "--remote-debugging-port=$debugPort",
    "--user-data-dir=$ProfileDirectory", 'about:blank'
  )
  $script:ChromeProcess = Start-Process `
    -FilePath $ChromePath `
    -ArgumentList $arguments `
    -WindowStyle Hidden `
    -PassThru

  $debugUrl = "http://127.0.0.1:$debugPort/json"
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $target = $null
  do {
    try {
      $rawTargets = Invoke-RestMethod -Uri $debugUrl -TimeoutSec 3
      foreach ($candidate in $rawTargets) {
        if ($candidate.type -eq 'page' -and $candidate.url -eq 'about:blank') {
          $target = $candidate
          break
        }
      }
    }
    catch {}
    if (-not $target) { Start-Sleep -Milliseconds 200 }
  } while (-not $target -and (Get-Date) -lt $deadline)
  if (-not $target) { throw 'Chrome DevTools page target was not created.' }

  $script:Socket = [Net.WebSockets.ClientWebSocket]::new()
  $connectCts = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(10))
  $debuggerUrl = ([string]$target.webSocketDebuggerUrl).Replace(
    '://localhost:',
    '://127.0.0.1:')
  try {
    try {
      $script:Socket.ConnectAsync(
        [Uri]$debuggerUrl,
        $connectCts.Token).GetAwaiter().GetResult() | Out-Null
    }
    catch {
      throw "Chrome DevTools websocket connection failed at ${debuggerUrl}: $($_.Exception.Message)"
    }
  }
  finally { $connectCts.Dispose() }
  Invoke-Cdp -Method 'Page.enable' | Out-Null
  Invoke-Cdp -Method 'Runtime.enable' | Out-Null
  Invoke-Cdp -Method 'Page.navigate' -Params @{ url = $InitialUrl } | Out-Null
}

function Set-Viewport {
  param([int]$Width, [int]$Height, [double]$DeviceScaleFactor = 1)
  Invoke-Cdp -Method 'Emulation.setDeviceMetricsOverride' -Params @{
    width = $Width
    height = $Height
    deviceScaleFactor = $DeviceScaleFactor
    mobile = $false
    screenWidth = $Width
    screenHeight = $Height
  } | Out-Null
}

function Get-HostState {
  $json = Invoke-JavaScript -Expression @'
(() => {
  const host = window.fruitDefenseWebGLHost;
  const snapshot = host ? host.snapshot() : null;
  const loading = document.querySelector('#unity-loading-bar');
  return JSON.stringify({
    href: location.href,
    readyState: document.readyState,
    host: snapshot,
    loading: loading ? getComputedStyle(loading).display : 'missing',
    warning: document.querySelector('#unity-warning')?.textContent.trim() ?? '',
    acceptanceReady: !!window.fruitDefenseUnityInstance,
    route: window.fruitDefenseAppRoute ?? -1,
    identity: window.fruitDefenseAcceptanceIdentity ?? null,
    payloadResources: performance.getEntriesByType('resource')
      .map(entry => entry.name)
      .filter(name => name.includes('/Build/'))
  });
})()
'@
  if ([string]::IsNullOrWhiteSpace([string]$json)) { return $null }
  return $json | ConvertFrom-Json
}

function Wait-HostState {
  param(
    [bool]$RequireUnity,
    [int]$ExpectedWidth = 0,
    [int]$ExpectedHeight = 0,
    [double]$ExpectedDeviceScaleFactor = 0
  )
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $state = $null
  do {
    try {
      $state = Get-HostState
      $hostReady = $null -ne $state -and $null -ne $state.host -and
        $state.host.hostId -eq $hostId
      $unityReady = -not $RequireUnity -or
        ($state.acceptanceReady -and [int]$state.route -eq 0 -and $state.loading -eq 'none')
      $expectedScale = if ($ExpectedWidth -gt 0) {
        [Math]::Min($ExpectedWidth / $logicalWidth, $ExpectedHeight / $logicalHeight)
      } else { 0 }
      $viewportReady = $ExpectedWidth -le 0 -or
        ([int]$state.host.viewportWidth -eq $ExpectedWidth -and
          [int]$state.host.viewportHeight -eq $ExpectedHeight -and
          [Math]::Abs([double]$state.host.devicePixelRatio - $ExpectedDeviceScaleFactor) -lt 0.001 -and
          [Math]::Abs([double]$state.host.scale - $expectedScale) -lt 0.0001)
      if ($hostReady -and $unityReady -and $viewportReady) { return $state }
    }
    catch {}
    Start-Sleep -Milliseconds 200
  } while ((Get-Date) -lt $deadline)
  throw "Host did not reach the required state: $($state | ConvertTo-Json -Compress -Depth 8)"
}

function Assert-HostLayout {
  param([object]$State, [int]$Width, [int]$Height, [double]$DeviceScaleFactor)
  $hostSnapshot = $State.host
  $canvas = $hostSnapshot.canvas
  $expectedScale = [Math]::Min($Width / $logicalWidth, $Height / $logicalHeight)
  $expectedWidth = $logicalWidth * $expectedScale
  $expectedHeight = $logicalHeight * $expectedScale
  $tolerance = 0.51

  Assert-Condition ($hostSnapshot.layout -eq 'desktop-contain') 'Desktop host did not use contain layout.'
  Assert-Condition ([Math]::Abs([double]$hostSnapshot.scale - $expectedScale) -lt 0.0001) `
    "Uniform scale mismatch at ${Width}x${Height}: expected=$expectedScale actual=$($hostSnapshot.scale)"
  Assert-Condition ([Math]::Abs([double]$canvas.width - $expectedWidth) -lt $tolerance) `
    "Canvas width mismatch at ${Width}x${Height}: expected=$expectedWidth actual=$($canvas.width)"
  Assert-Condition ([Math]::Abs([double]$canvas.height - $expectedHeight) -lt $tolerance) `
    "Canvas height mismatch at ${Width}x${Height}: expected=$expectedHeight actual=$($canvas.height)"
  Assert-Condition ([Math]::Abs(([double]$canvas.width / [double]$canvas.height) -
      ($logicalWidth / $logicalHeight)) -lt 0.0001) `
    "Canvas aspect ratio drifted at ${Width}x${Height}."
  Assert-Condition ([double]$canvas.left -ge -0.51 -and [double]$canvas.top -ge -0.51 -and
      [double]$canvas.right -le $Width + 0.51 -and [double]$canvas.bottom -le $Height + 0.51) `
    "Canvas escaped the viewport at ${Width}x${Height}: $($canvas | ConvertTo-Json -Compress)"
  Assert-Condition ([Math]::Abs((([double]$canvas.left + [double]$canvas.right) / 2) - ($Width / 2)) -lt $tolerance) `
    "Canvas is not horizontally centered at ${Width}x${Height}."
  Assert-Condition ([Math]::Abs((([double]$canvas.top + [double]$canvas.bottom) / 2) - ($Height / 2)) -lt $tolerance) `
    "Canvas is not vertically centered at ${Width}x${Height}."
  Assert-Condition ([double]$canvas.backingWidth -eq $logicalWidth -and
      [double]$canvas.backingHeight -eq $logicalHeight) `
    "Desktop canvas backing size changed from 402x874: $($canvas.backingWidth)x$($canvas.backingHeight)"
  Assert-Condition ([double]$hostSnapshot.scroll.x -eq 0 -and [double]$hostSnapshot.scroll.y -eq 0 -and
      [double]$hostSnapshot.scroll.documentWidth -le $Width + 0.5 -and
      [double]$hostSnapshot.scroll.documentHeight -le $Height + 0.5) `
    "Host created page scroll at ${Width}x${Height}: $($hostSnapshot.scroll | ConvertTo-Json -Compress)"
  Assert-Condition ([Math]::Abs([double]$hostSnapshot.devicePixelRatio - $DeviceScaleFactor) -lt 0.001) `
    "Device-pixel ratio did not update: expected=$DeviceScaleFactor actual=$($hostSnapshot.devicePixelRatio)"
  return [ordered]@{
    viewport = [ordered]@{ width = $Width; height = $Height; devicePixelRatio = $DeviceScaleFactor }
    expectedScale = $expectedScale
    snapshot = $hostSnapshot
  }
}

function Get-PayloadIdentity {
  param([string]$Root)
  $buildDirectory = Join-Path $Root 'Build'
  Assert-Condition (Test-Path -LiteralPath $buildDirectory -PathType Container) `
    "WebGL payload directory is missing: $buildDirectory"
  $roles = [ordered]@{
    loader = '*.loader.js'
    data = '*.data.unityweb'
    framework = '*.framework.js.unityweb'
    wasm = '*.wasm.unityweb'
  }
  $result = [ordered]@{}
  foreach ($role in $roles.Keys) {
    $matches = @(Get-ChildItem -LiteralPath $buildDirectory -Filter $roles[$role] -File)
    Assert-Condition ($matches.Count -eq 1) `
      "Expected one $role payload, found $($matches.Count)."
    $hash = (Get-FileHash -LiteralPath $matches[0].FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $result[$role] = [ordered]@{
      file = $matches[0].Name
      sha256 = $hash
      version = $hash.Substring(0, 12)
      sizeBytes = $matches[0].Length
    }
  }
  return $result
}

function Assert-PayloadResources {
  param([object]$State, [System.Collections.Specialized.OrderedDictionary]$Payload)
  foreach ($role in $Payload.Keys) {
    $needle = "/Build/$($Payload[$role].file)?v=$($Payload[$role].version)"
    $matches = @($State.payloadResources | Where-Object {
      ([string]$_).IndexOf($needle, [StringComparison]::Ordinal) -ge 0
    })
    Assert-Condition ($matches.Count -ge 1) `
      "Browser did not load the expected versioned $role payload: $needle"
  }
}

function Invoke-CanvasRelativeClick {
  param([object]$Canvas, [double]$LogicalX, [double]$LogicalY)
  $clientX = [double]$Canvas.left + ($LogicalX / $logicalWidth) * [double]$Canvas.width
  $clientY = [double]$Canvas.top + ($LogicalY / $logicalHeight) * [double]$Canvas.height
  $roundTripX = (($clientX - [double]$Canvas.left) / [double]$Canvas.width) * $logicalWidth
  $roundTripY = (($clientY - [double]$Canvas.top) / [double]$Canvas.height) * $logicalHeight
  $roundTripError = [Math]::Max(
    [Math]::Abs($roundTripX - $LogicalX),
    [Math]::Abs($roundTripY - $LogicalY))
  Assert-Condition ($roundTripError -le 0.5) `
    "Canvas-relative pointer round trip exceeded 0.5 logical point: $roundTripError"
  foreach ($type in @('mousePressed', 'mouseReleased')) {
    Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
      type = $type
      x = $clientX
      y = $clientY
      button = 'left'
      clickCount = 1
    } | Out-Null
  }
  return [ordered]@{
    logical = [ordered]@{ x = $LogicalX; y = $LogicalY }
    client = [ordered]@{ x = $clientX; y = $clientY }
    roundTrip = [ordered]@{ x = $roundTripX; y = $roundTripY; maxErrorLogical = $roundTripError }
  }
}

function Wait-SelectedLevel {
  param([string]$LevelId)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $state = $null
  do {
    $state = Get-HostState
    if ($state.acceptanceReady -and [int]$state.route -eq 0 -and
        [string]$state.identity.levelId -eq $LevelId) { return $state }
    Start-Sleep -Milliseconds 150
  } while ((Get-Date) -lt $deadline)
  throw "Canvas-relative Lobby click did not select ${LevelId}: $($state.identity | ConvertTo-Json -Compress)"
}

function Save-Screenshot {
  param([string]$Path)
  $capture = Invoke-Cdp -Method 'Page.captureScreenshot' -Params @{
    format = 'png'
    fromSurface = $true
    captureBeyondViewport = $false
  }
  [IO.File]::WriteAllBytes($Path, [Convert]::FromBase64String([string]$capture.data))
  Assert-Condition ((Get-Item -LiteralPath $Path).Length -gt 1024) `
    "Host screenshot is unexpectedly small: $Path"
  return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function New-SourceFixture {
  $root = Join-Path $env:TEMP "fruit-defense-host-source-$([Guid]::NewGuid().ToString('N'))"
  $data = Join-Path $root 'TemplateData'
  New-Item -ItemType Directory -Path $data -Force | Out-Null
  Copy-Item -LiteralPath (Join-Path $templateRoot 'TemplateData\fruit-defense-host.css') -Destination $data
  Copy-Item -LiteralPath (Join-Path $templateRoot 'TemplateData\fruit-defense-host.js') -Destination $data
  $html = @'
<!doctype html>
<html data-fruit-defense-host="portrait-contain-v1">
<head><meta charset="utf-8"><link rel="stylesheet" href="TemplateData/fruit-defense-host.css"></head>
<body><main id="unity-host"><section id="unity-container"><canvas id="unity-canvas" width="402" height="874"></canvas></section></main>
<script src="TemplateData/fruit-defense-host.js"></script>
<script>
window.fruitDefenseHostFixture = window.fruitDefenseWebGLHost.mount({
  host: document.querySelector('#unity-host'),
  container: document.querySelector('#unity-container'),
  canvas: document.querySelector('#unity-canvas'),
  logicalWidth: 402,
  logicalHeight: 874
});
</script></body></html>
'@
  Set-Content -LiteralPath (Join-Path $root 'index.html') -Value $html -Encoding UTF8
  return $root
}

function Start-HostServer {
  param([string]$StaticRoot)
  $port = Get-FreeTcpPort
  $script:serverStdout = Join-Path $env:TEMP "fruit-defense-host-server-$port.stdout.log"
  $script:serverStderr = Join-Path $env:TEMP "fruit-defense-host-server-$port.stderr.log"
  $oldRoot = $env:STATIC_ROOT
  $oldPort = $env:PORT
  try {
    $env:STATIC_ROOT = (Resolve-Path -LiteralPath $StaticRoot).Path
    $env:PORT = [string]$port
    $script:serverProcess = Start-Process `
      -FilePath 'node' `
      -ArgumentList 'deploy/server.mjs' `
      -WorkingDirectory $projectRoot `
      -WindowStyle Hidden `
      -PassThru `
      -RedirectStandardOutput $script:serverStdout `
      -RedirectStandardError $script:serverStderr
  }
  finally {
    $env:STATIC_ROOT = $oldRoot
    $env:PORT = $oldPort
  }
  $url = "http://127.0.0.1:$port/"
  Wait-Http -Url $url
  return $url
}

try {
  Assert-Condition (Test-Path -LiteralPath $ChromePath -PathType Leaf) `
    "Chrome not found: $ChromePath"
  Assert-Condition ((Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectSettings.asset') -Raw) -match
      '(?m)^\s*webGLTemplate:\s*PROJECT:FruitDefensePortraitContain\s*$') `
    'ProjectSettings does not select PROJECT:FruitDefensePortraitContain.'
  foreach ($relative in @('index.html', 'TemplateData\fruit-defense-host.css', 'TemplateData\fruit-defense-host.js')) {
    Assert-Condition (Test-Path -LiteralPath (Join-Path $templateRoot $relative) -PathType Leaf) `
      "Project-owned host source is missing: $relative"
  }

  New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
  if ($SourceSelfCheck) {
    $fixtureRoot = New-SourceFixture
    $url = Start-HostServer -StaticRoot $fixtureRoot
    $profile = Join-Path $env:TEMP "fruit-defense-host-profile-$([Guid]::NewGuid().ToString('N'))"
    try {
      Start-ChromeSession -InitialUrl $url -ProfileDirectory $profile
      $checks = @()
      foreach ($case in @(
        [ordered]@{ width = 1280; height = 720; dpr = 1.0 },
        [ordered]@{ width = 1024; height = 640; dpr = 2.0 },
        [ordered]@{ width = 1440; height = 900; dpr = 1.0 }
      )) {
        Set-Viewport -Width $case.width -Height $case.height -DeviceScaleFactor $case.dpr
        $state = Wait-HostState `
          -RequireUnity $false `
          -ExpectedWidth $case.width `
          -ExpectedHeight $case.height `
          -ExpectedDeviceScaleFactor $case.dpr
        $checks += Assert-HostLayout `
          -State $state `
          -Width $case.width `
          -Height $case.height `
          -DeviceScaleFactor $case.dpr
      }
      $manifest = [ordered]@{
        schemaVersion = 1
        evidenceType = 'project-webgl-host-source-self-check'
        accepted = $true
        hostId = $hostId
        checks = $checks
      }
      $manifestPath = Join-Path $OutputDirectory 'host-source-self-check.json'
      $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
      Write-Host "FRUIT_DEFENSE_WEBGL_HOST_SOURCE_OK manifest=$manifestPath"
    }
    finally {
      Stop-ChromeSession
      Remove-Item -LiteralPath $profile -Recurse -Force -ErrorAction SilentlyContinue
    }
    return
  }

  Assert-Condition (Test-Path -LiteralPath (Join-Path $BuildRoot 'index.html') -PathType Leaf) `
    "WebGL build not found: $BuildRoot"
  foreach ($relative in @('TemplateData\fruit-defense-host.css', 'TemplateData\fruit-defense-host.js')) {
    $sourcePath = Join-Path $templateRoot $relative
    $builtPath = Join-Path $BuildRoot $relative
    Assert-Condition (Test-Path -LiteralPath $builtPath -PathType Leaf) `
      "Built host file is missing: $builtPath"
    Assert-Condition ((Get-FileHash $sourcePath -Algorithm SHA256).Hash -eq
        (Get-FileHash $builtPath -Algorithm SHA256).Hash) `
      "Built host bytes differ from project source: $relative"
  }

  $payload = Get-PayloadIdentity -Root $BuildRoot
  $url = Start-HostServer -StaticRoot $BuildRoot
  $acceptanceUrl = $url + '?acceptance=1&levelId=orchard-01&safeTop=0&safeBottom=0'
  $matrices = @()
  foreach ($case in @(
    [ordered]@{ name = '1280x720'; width = 1280; height = 720 },
    [ordered]@{ name = '1440x900'; width = 1440; height = 900 },
    [ordered]@{ name = '1024x640'; width = 1024; height = 640 }
  )) {
    $profile = Join-Path $env:TEMP "fruit-defense-host-profile-$([Guid]::NewGuid().ToString('N'))"
    try {
      Start-ChromeSession -InitialUrl 'about:blank' -ProfileDirectory $profile
      Set-Viewport -Width $case.width -Height $case.height -DeviceScaleFactor 1
      Invoke-Cdp -Method 'Page.navigate' -Params @{ url = $acceptanceUrl } | Out-Null
      $state = Wait-HostState `
        -RequireUnity $true `
        -ExpectedWidth $case.width `
        -ExpectedHeight $case.height `
        -ExpectedDeviceScaleFactor 1
      $layout = Assert-HostLayout `
        -State $state `
        -Width $case.width `
        -Height $case.height `
        -DeviceScaleFactor 1
      Assert-PayloadResources -State $state -Payload $payload
      Assert-Condition ([string]$state.identity.levelId -eq 'orchard-01') `
        "Host acceptance did not start on orchard-01: $($state.identity | ConvertTo-Json -Compress)"

      $input = Invoke-CanvasRelativeClick `
        -Canvas $state.host.canvas `
        -LogicalX 201 `
        -LogicalY 406
      $selectedState = Wait-SelectedLevel -LevelId 'orchard-02'
      Assert-Condition ([int]$selectedState.route -eq 0) `
        'Canvas-relative input unexpectedly left the Lobby route.'
      $screenshotPath = Join-Path $OutputDirectory "$($case.name)-lobby-orchard-02.png"
      $screenshotHash = Save-Screenshot -Path $screenshotPath
      $matrices += [ordered]@{
        name = $case.name
        layout = $layout
        input = $input
        selectedLevelId = [string]$selectedState.identity.levelId
        route = [int]$selectedState.route
        payload = $payload
        screenshot = $screenshotPath
        screenshotSha256 = $screenshotHash
        checks = [ordered]@{
          completeCanvasContained = 'pass'
          uniformScale = 'pass'
          centeredLetterbox = 'pass'
          noPageScroll = 'pass'
          fixedLogicalBacking = 'pass'
          payloadIdentity = 'pass'
          canvasRelativeLobbyInput = 'pass'
        }
      }
    }
    finally {
      Stop-ChromeSession
      Remove-Item -LiteralPath $profile -Recurse -Force -ErrorAction SilentlyContinue
    }
  }

  $manifest = [ordered]@{
    schemaVersion = 1
    evidenceType = 'desktop-webgl-host-acceptance'
    accepted = $true
    hostId = $hostId
    logicalCanvas = [ordered]@{ width = [int]$logicalWidth; height = [int]$logicalHeight }
    buildRoot = (Resolve-Path -LiteralPath $BuildRoot).Path
    payload = $payload
    matrices = $matrices
  }
  $manifestPath = Join-Path $OutputDirectory 'webgl-host-acceptance.json'
  $manifest | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
  Write-Host "FRUIT_DEFENSE_WEBGL_HOST_ACCEPTANCE_OK manifest=$manifestPath"
}
finally {
  Stop-ChromeSession
  if ($serverProcess -and -not $serverProcess.HasExited) {
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    $serverProcess.WaitForExit(5000) | Out-Null
  }
  foreach ($log in @($serverStdout, $serverStderr)) {
    if (-not [string]::IsNullOrWhiteSpace($log)) {
      Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
    }
  }
  if ($fixtureRoot) {
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
  }
}
