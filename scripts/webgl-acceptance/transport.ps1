# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Set-AcceptanceQuery {
  param([string]$TargetUrl)
  $result = Set-WebGlUrlQueryParameter -TargetUrl $TargetUrl -Name 'acceptance' -Value '1'
  if ($ShellError -or (-not $Flow -and -not $ShellVisual)) {
    $result = Set-WebGlUrlQueryParameter -TargetUrl $result -Name 'route' -Value 'battle'
  }
  $queryLevelId = if ($ShellError) { $ErrorLevelId } else { $LevelId }
  $result = Set-WebGlUrlQueryParameter -TargetUrl $result -Name 'levelId' -Value $queryLevelId
  $result = Set-WebGlUrlQueryParameter -TargetUrl $result -Name 'safeTop' -Value ([string]$SafeTop)
  $result = Set-WebGlUrlQueryParameter -TargetUrl $result -Name 'safeBottom' -Value ([string]$SafeBottom)
  return $result
}

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
    do {
      $message = Receive-CdpMessage -WebSocket $socket -Token $cts.Token
      $messageId = $message.PSObject.Properties['id']
    } while ($null -eq $messageId -or [int]$messageId.Value -ne $id)
    $errorProperty = $message.PSObject.Properties['error']
    if ($null -ne $errorProperty) {
      $errorValue = $errorProperty.Value
      $errorMessage = $errorValue.PSObject.Properties['message']
      $errorText = if ($null -ne $errorMessage) {
        [string]$errorMessage.Value
      } else {
        $errorValue | ConvertTo-Json -Compress -Depth 8
      }
      throw "CDP $Method failed: $errorText"
    }
    $resultProperty = $message.PSObject.Properties['result']
    if ($null -eq $resultProperty) { throw "CDP $Method returned no result." }
    return $resultProperty.Value
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
  $exceptionProperty = $result.PSObject.Properties['exceptionDetails']
  if ($null -ne $exceptionProperty) {
    $details = $exceptionProperty.Value
    $textProperty = $details.PSObject.Properties['text']
    $description = if ($null -ne $textProperty) {
      [string]$textProperty.Value
    } else {
      $details | ConvertTo-Json -Compress -Depth 8
    }
    throw "Browser JavaScript failed: $description"
  }
  $remoteObjectProperty = $result.PSObject.Properties['result']
  if ($null -eq $remoteObjectProperty) { throw 'Browser JavaScript returned no remote object.' }
  $valueProperty = $remoteObjectProperty.Value.PSObject.Properties['value']
  if ($null -eq $valueProperty) { return $null }
  return $valueProperty.Value
}

function Invoke-CanvasClick {
  param([double]$X, [double]$Y)
  Invoke-CanvasClickImmediate -X $X -Y $Y
  Start-Sleep -Milliseconds 450
}

function Invoke-CanvasClickImmediate {
  param([double]$X, [double]$Y)
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mousePressed'; x = $X; y = $Y; button = 'left'; clickCount = 1
  } | Out-Null
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mouseReleased'; x = $X; y = $Y; button = 'left'; clickCount = 1
  } | Out-Null
}

function Start-CanvasPress {
  param([double]$X, [double]$Y)
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mousePressed'; x = $X; y = $Y; button = 'left'; buttons = 1; clickCount = 1
  } | Out-Null
}

function Stop-CanvasPress {
  param([double]$X, [double]$Y)
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mouseReleased'; x = $X; y = $Y; button = 'left'; buttons = 0; clickCount = 1
  } | Out-Null
}

function Move-CanvasPointer {
  param([double]$X, [double]$Y)
  Invoke-Cdp -Method 'Input.dispatchMouseEvent' -Params @{
    type = 'mouseMoved'; x = $X; y = $Y; button = 'none'; buttons = 0
  } | Out-Null
}

function Move-CanvasPointerOut {
  Move-CanvasPointer -X 1 -Y 1
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

function Assert-AcceptanceBuildProfileVerified {
  if ($null -eq $verifiedBuildProfile -or
      [string]$verifiedBuildProfile.verifiedProfile -cne 'acceptance') {
    throw 'Acceptance build profile must be verified before browser or Unity bridge commands.'
  }
}

function Set-AcceptanceState {
  param([string]$State)
  Assert-AcceptanceBuildProfileVerified
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

function Stop-OwnedChrome {
  if ($socket -and $socket.State -eq [Net.WebSockets.WebSocketState]::Open) {
    try { Invoke-Cdp -Method 'Browser.close' | Out-Null } catch { }
  }
  if ($chromeProcess -and -not $chromeProcess.HasExited) {
    try { [void]$chromeProcess.WaitForExit(5000) } catch { }
  }
  if ($chromeProcess -and -not $chromeProcess.HasExited) {
    Stop-Process -Id $chromeProcess.Id -Force -ErrorAction SilentlyContinue
  }
  Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -eq 'chrome.exe' -and $_.CommandLine -like "*$profileDir*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}
