param(
  [string]$Url,
  [switch]$ServeLocal,
  [string]$BuildRoot = "$(Split-Path $PSScriptRoot)\Builds\WebGL",
  [string]$OutputRoot = "$(Split-Path $PSScriptRoot)\Logs\visual-acceptance",
  [string]$ChromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe',
  [ValidateRange(1, 16384)]
  [int]$Width = 402,
  [ValidateRange(1, 16384)]
  [int]$Height = 874,
  [ValidateRange(0, 16384)]
  [int]$SafeTop = 0,
  [ValidateRange(0, 16384)]
  [int]$SafeBottom = 0,
  [ValidateSet('orchard-01', 'orchard-02', 'orchard-03')]
  [string]$LevelId = 'orchard-01',
  [int]$TimeoutSeconds = 45,
  [switch]$Flow,
  [string]$ProfilePath,
  [switch]$CacheSeedOnly,
  [string]$CacheSeedManifestPath,
  [string]$OutputDirectory,
  [switch]$SelfCheck
)

$ErrorActionPreference = 'Stop'
$referenceWidth = 402.0
$referenceHeight = 874.0
if ($SafeTop + $SafeBottom -ge $Height) {
  throw "Safe-area insets must leave positive content height: height=$Height top=$SafeTop bottom=$SafeBottom"
}
$safeContentHeight = $Height - $SafeTop - $SafeBottom
$referenceScale = [Math]::Min($Width / $referenceWidth, $safeContentHeight / $referenceHeight)
$referenceOffsetX = ($Width - $referenceWidth * $referenceScale) / 2.0
$referenceOffsetY = $SafeTop + ($safeContentHeight - $referenceHeight * $referenceScale) / 2.0
$shellContentWidth = [Math]::Min(370.0 * $referenceScale, $Width - 32.0 * $referenceScale)
$shellContentX = ($Width - $shellContentWidth) / 2.0
$shellContentY = $SafeTop + 18.0 * $referenceScale
$safeAreaEvidence = [ordered]@{
  top = $SafeTop
  bottom = $SafeBottom
  left = 0
  right = 0
  coordinateSpace = 'css-pixel/top-left'
  contentRect = [ordered]@{ x = 0; y = $SafeTop; width = $Width; height = $safeContentHeight }
  designReference = [ordered]@{ width = [int]$referenceWidth; height = [int]$referenceHeight }
  designScale = $referenceScale
  designOffset = [ordered]@{ x = $referenceOffsetX; y = $referenceOffsetY }
  shellContentRect = [ordered]@{
    x = $shellContentX
    y = $shellContentY
    width = $shellContentWidth
    height = [Math]::Max(0, $Height - $SafeBottom - $shellContentY - 18.0 * $referenceScale)
  }
}
$projectRoot = Split-Path $PSScriptRoot
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$outputDir = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
  Join-Path $OutputRoot $timestamp
} else {
  [IO.Path]::GetFullPath($OutputDirectory)
}
$ownsProfile = [string]::IsNullOrWhiteSpace($ProfilePath)
$profileDir = if ($ownsProfile) {
  Join-Path $env:TEMP "fruit-defense-cdp-$([Guid]::NewGuid().ToString('N'))"
} else {
  [IO.Path]::GetFullPath($ProfilePath)
}
$serverProcess = $null
$chromeProcess = $null
$socket = $null
$script:CdpId = 0

if ($CacheSeedOnly -and $ownsProfile) {
  throw 'Cache seed mode requires -ProfilePath so the browser cache survives this run.'
}
if (-not [string]::IsNullOrWhiteSpace($CacheSeedManifestPath) -and $ownsProfile) {
  throw 'Cross-release acceptance requires -ProfilePath from the cache seed run.'
}
if ($CacheSeedOnly -and -not [string]::IsNullOrWhiteSpace($CacheSeedManifestPath)) {
  throw 'Cache seed mode cannot consume another cache seed manifest.'
}

function Set-UrlQueryParameter {
  param([string]$TargetUrl, [string]$Name, [string]$Value)
  $builder = [UriBuilder]::new($TargetUrl)
  $pairs = [ordered]@{}
  foreach ($part in $builder.Query.TrimStart('?').Split('&', [StringSplitOptions]::RemoveEmptyEntries)) {
    $components = $part.Split('=', 2)
    $key = [Uri]::UnescapeDataString($components[0])
    $pairs[$key] = if ($components.Count -eq 2) { [Uri]::UnescapeDataString($components[1]) } else { '' }
  }
  $pairs[$Name] = $Value
  $builder.Query = (($pairs.GetEnumerator() | ForEach-Object {
    [Uri]::EscapeDataString([string]$_.Key) + '=' + [Uri]::EscapeDataString([string]$_.Value)
  }) -join '&')
  return $builder.Uri.AbsoluteUri
}

function Set-AcceptanceQuery {
  param([string]$TargetUrl)
  $result = Set-UrlQueryParameter -TargetUrl $TargetUrl -Name 'acceptance' -Value '1'
  if (-not $Flow) {
    $result = Set-UrlQueryParameter -TargetUrl $result -Name 'route' -Value 'battle'
  }
  $result = Set-UrlQueryParameter -TargetUrl $result -Name 'levelId' -Value $LevelId
  $result = Set-UrlQueryParameter -TargetUrl $result -Name 'safeTop' -Value ([string]$SafeTop)
  $result = Set-UrlQueryParameter -TargetUrl $result -Name 'safeBottom' -Value ([string]$SafeBottom)
  return $result
}

function Convert-ReferencePoint {
  param([double]$X, [double]$Y)
  return [ordered]@{
    x = $referenceOffsetX + $X * $referenceScale
    y = $referenceOffsetY + $Y * $referenceScale
  }
}

function Convert-ReferenceRect {
  param([double]$X, [double]$Y, [double]$Width, [double]$Height)
  $topLeft = Convert-ReferencePoint -X $X -Y $Y
  $bottomRight = Convert-ReferencePoint -X ($X + $Width) -Y ($Y + $Height)
  return [ordered]@{
    xMin = [Math]::Max(0, [Math]::Floor($topLeft.x))
    yMin = [Math]::Max(0, [Math]::Floor($topLeft.y))
    xMax = [Math]::Min($script:Width, [Math]::Ceiling($bottomRight.x))
    yMax = [Math]::Min($script:Height, [Math]::Ceiling($bottomRight.y))
  }
}

function Convert-ShellReferencePoint {
  param([double]$X, [double]$Y)
  # PortraitShellLayout anchors content at the safe-area top rather than vertically centering it.
  return [ordered]@{
    x = $shellContentX + ($X - 16.0) * $referenceScale
    y = $SafeTop + $Y * $referenceScale
  }
}

$referenceControls = [ordered]@{
  lobbyLevelOrchard01 = [ordered]@{ x = 201; y = 151 }
  lobbyLevelOrchard02 = [ordered]@{ x = 201; y = 249 }
  lobbyLevelOrchard03 = [ordered]@{ x = 201; y = 347 }
  lobbyStart = [ordered]@{ x = 201; y = 446 }
  settlementRetry = [ordered]@{ x = 201; y = 449 }
  settlementReturn = [ordered]@{ x = 201; y = 528 }
  headerPause = [ordered]@{ x = 300; y = 38 }
  waveAction = [ordered]@{ x = 302; y = 548 }
  pauseContinue = [ordered]@{ x = 125; y = 492 }
  pauseRestart = [ordered]@{ x = 277; y = 492 }
  nurserySlot0 = [ordered]@{ x = 51; y = 703 }
  acceptanceCell0 = [ordered]@{ x = 32; y = 195 }
  acceptanceCell1 = [ordered]@{ x = 80; y = 195 }
}
$controls = [ordered]@{}
foreach ($name in $referenceControls.Keys) {
  $isShellControl = $name.StartsWith('lobby') -or $name.StartsWith('settlement')
  $controls[$name] = if ($isShellControl) {
    Convert-ShellReferencePoint -X $referenceControls[$name].x -Y $referenceControls[$name].y
  }
  else {
    Convert-ReferencePoint -X $referenceControls[$name].x -Y $referenceControls[$name].y
  }
}
$levelCardControlName = 'lobbyLevel' + ($LevelId -replace '-', '').Replace('orchard', 'Orchard')
if (-not $controls.Contains($levelCardControlName)) {
  throw "No Lobby control is defined for level '$LevelId'."
}
$headerSampleRegion = Convert-ReferenceRect -X 13 -Y 11 -Width 250 -Height 53
$formerActionRegion = Convert-ReferenceRect -X 8 -Y 760 -Width 386 -Height 50
$hudDarkPixelThreshold = [Math]::Max(1, [Math]::Floor(80 * $referenceScale * $referenceScale))
$hudLightPixelThreshold = [Math]::Max(1, [Math]::Floor(5000 * $referenceScale * $referenceScale))
$formerActionPixelThreshold = [Math]::Max(12, [Math]::Ceiling(12 * $referenceScale * $referenceScale))
$nearBlackLumaThreshold = 0.08
$maxBlackFraction = 0.01
$maxNearBlackFraction = 0.05
$framePixelThresholds = [ordered]@{
  sampleStepPixels = 4
  nearBlackLuma = $nearBlackLumaThreshold
  maxBlackFraction = $maxBlackFraction
  maxNearBlackFraction = $maxNearBlackFraction
  maxInvalidFraction = 0.05
}
$warmAssetTransferLimitBytes = 16KB
$warmTotalTransferLimitBytes = 64KB
$expectedLevelIdentities = [ordered]@{
  'orchard-01' = [ordered]@{
    levelId = 'orchard-01'; mapId = 'orchard-01'
    waveSetId = 'waves.orchard-01.teaching'; ruleSetId = 'rules.orchard-01.baseline'
    themeId = 'theme.orchard-01.day'
  }
  'orchard-02' = [ordered]@{
    levelId = 'orchard-02'; mapId = 'orchard-02'
    waveSetId = 'waves.orchard-02.coverage'; ruleSetId = 'rules.orchard-02.coverage'
    themeId = 'theme.orchard-02.creek'
  }
  'orchard-03' = [ordered]@{
    levelId = 'orchard-03'; mapId = 'orchard-03'
    waveSetId = 'waves.orchard-03.pressure'; ruleSetId = 'rules.orchard-03.pressure'
    themeId = 'theme.orchard-03.dusk'
  }
}
$expectedLevelIdentity = $expectedLevelIdentities[$LevelId]

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
  $assetVersions = [ordered]@{}

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
    $assetVersions[$name] = $version

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

    $etag = [string]$response.Headers['ETag']
    $etagMatch = [regex]::Match($etag, '^"(?<sha256>[0-9a-f]{64})"$')
    if (-not $etagMatch.Success -or
        $etagMatch.Groups['sha256'].Value.Substring(0, 12) -cne $version) {
      throw "WebGL $name asset ETag does not identify its advertised content version: version=$version etag=$etag"
    }

    $wrongVersion = if ($version -ceq '000000000000') { '111111111111' } else { '000000000000' }
    $wrongVersionUrl = Set-UrlQueryParameter -TargetUrl $assetUri.AbsoluteUri -Name 'v' -Value $wrongVersion
    $wrongVersionResponse = Invoke-WebRequest -UseBasicParsing -Method Head -Uri $wrongVersionUrl -TimeoutSec 15
    $wrongVersionCacheControl = [string]$wrongVersionResponse.Headers['Cache-Control']
    if ($wrongVersionCacheControl -match 'immutable') {
      throw "WebGL $name asset grants immutable caching to an incorrect version: $wrongVersionCacheControl"
    }

    $assets[$name] = [ordered]@{
      url = $assetUri.AbsoluteUri
      version = $version
      contentType = $contentType
      contentEncoding = $contentEncoding
      cacheControl = $cacheControl
      contentLength = $contentLength
      etag = $etag
      contentSha256 = $etagMatch.Groups['sha256'].Value
    }
  }

  return [ordered]@{
    assetVersions = $assetVersions
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

function Get-UnityResourceTiming {
  param(
    [Parameter(Mandatory = $true)][string]$Label,
    [Parameter(Mandatory = $true)][System.Collections.Specialized.OrderedDictionary]$DeliveryAssets,
    [Parameter(Mandatory = $true)][DateTimeOffset]$StartedAt
  )

  $expectedAssets = @(
    foreach ($role in $DeliveryAssets.Keys) {
      [ordered]@{ role = $role; url = [string]$DeliveryAssets[$role].url }
    }
  )
  $expectedJson = ConvertTo-Json -InputObject $expectedAssets -Compress -Depth 4
  $expression = @"
(() => {
  const expectedAssets = $expectedJson;
  const entries = performance.getEntriesByType('resource');
  return JSON.stringify(expectedAssets.map(asset => {
    const matches = entries.filter(entry => entry.name === asset.url);
    if (!matches.length) return { role: asset.role, url: asset.url, missing: true };
    const entry = matches[matches.length - 1];
    return {
      role: asset.role,
      url: asset.url,
      missing: false,
      startTimeMilliseconds: Math.round(entry.startTime * 1000) / 1000,
      durationMilliseconds: Math.round(entry.duration * 1000) / 1000,
      transferSize: entry.transferSize,
      encodedBodySize: entry.encodedBodySize,
      decodedBodySize: entry.decodedBodySize,
      responseStatus: entry.responseStatus || 0,
      deliveryType: entry.deliveryType || '',
      nextHopProtocol: entry.nextHopProtocol || ''
    };
  }));
})()
"@
  $timings = (Invoke-JavaScript -Expression $expression) | ConvertFrom-Json
  $assets = [ordered]@{}
  [long]$totalTransferSize = 0
  foreach ($timing in $timings) {
    if ($timing.missing) {
      throw "Browser resource timing is missing the $($timing.role) payload: $($timing.url)"
    }
    $assets[[string]$timing.role] = [ordered]@{
      url = [string]$timing.url
      startTimeMilliseconds = [double]$timing.startTimeMilliseconds
      durationMilliseconds = [double]$timing.durationMilliseconds
      transferSize = [long]$timing.transferSize
      encodedBodySize = [long]$timing.encodedBodySize
      decodedBodySize = [long]$timing.decodedBodySize
      responseStatus = [int]$timing.responseStatus
      deliveryType = [string]$timing.deliveryType
      nextHopProtocol = [string]$timing.nextHopProtocol
    }
    $totalTransferSize += [long]$timing.transferSize
  }

  $completedAt = [DateTimeOffset]::UtcNow
  return [ordered]@{
    label = $Label
    startedAtUtc = $StartedAt.ToString('o')
    completedAtUtc = $completedAt.ToString('o')
    startupDurationMilliseconds = [math]::Round(($completedAt - $StartedAt).TotalMilliseconds, 3)
    totalPayloadTransferSize = $totalTransferSize
    assets = $assets
  }
}

function Assert-WarmCacheTransfer {
  param(
    [Parameter(Mandatory = $true)][System.Collections.Specialized.OrderedDictionary]$WarmRun
  )

  foreach ($role in $WarmRun.assets.Keys) {
    $transferSize = [long]$WarmRun.assets[$role].transferSize
    if ($transferSize -gt $warmAssetTransferLimitBytes) {
      throw "Warm WebGL $role payload redownloaded too many bytes: $transferSize > $warmAssetTransferLimitBytes"
    }
  }
  if ([long]$WarmRun.totalPayloadTransferSize -gt $warmTotalTransferLimitBytes) {
    throw "Warm WebGL payload transfer exceeded the total allowance: $($WarmRun.totalPayloadTransferSize) > $warmTotalTransferLimitBytes"
  }
}

function Get-ReleaseTransitionEvidence {
  param(
    [Parameter(Mandatory = $true)][object]$SeedManifest,
    [Parameter(Mandatory = $true)][System.Collections.Specialized.OrderedDictionary]$CandidateDelivery,
    [Parameter(Mandatory = $true)][System.Collections.Specialized.OrderedDictionary]$CandidateRun
  )

  if ($SeedManifest.schemaVersion -ne 1 -or $SeedManifest.evidenceType -ne 'webgl-cache-seed') {
    throw "Unsupported WebGL cache seed manifest: $CacheSeedManifestPath"
  }

  $reusedRoles = @()
  $changedRoles = @()
  [long]$expectedDownloadBytes = 0
  [long]$observedCandidateTransferBytes = 0
  foreach ($role in @('loader', 'data', 'framework', 'wasm')) {
    $seedAssetProperty = $SeedManifest.delivery.assets.PSObject.Properties[$role]
    if ($null -eq $seedAssetProperty) {
      throw "WebGL cache seed manifest is missing the $role payload."
    }
    $seedAsset = $seedAssetProperty.Value
    $candidateAsset = $CandidateDelivery.assets[$role]
    $candidateTiming = $CandidateRun.assets[$role]
    $observedCandidateTransferBytes += [long]$candidateTiming.transferSize
    if ([string]$seedAsset.version -ceq [string]$candidateAsset.version) {
      $reusedRoles += $role
      $transferSize = [long]$candidateTiming.transferSize
      if ($transferSize -gt $warmAssetTransferLimitBytes) {
        throw "Cross-release WebGL $role payload redownloaded unchanged bytes: $transferSize > $warmAssetTransferLimitBytes"
      }
    }
    else {
      $changedRoles += $role
      $expectedDownloadBytes += [long]$candidateAsset.contentLength
    }
  }

  return [ordered]@{
    state = 'compared'
    seedManifestPath = (Resolve-Path -LiteralPath $CacheSeedManifestPath).Path
    baselineUrl = [string]$SeedManifest.url
    candidateUrl = $Url
    baselineAssetVersions = $SeedManifest.delivery.assetVersions
    candidateAssetVersions = $CandidateDelivery.assetVersions
    reusedRoles = @($reusedRoles)
    changedRoles = @($changedRoles)
    expectedDownloadBytes = $expectedDownloadBytes
    observedCandidateTransferBytes = $observedCandidateTransferBytes
    unchangedPayloadTransferLimitBytes = $warmAssetTransferLimitBytes
    candidateFirstLoad = $CandidateRun
  }
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

function Get-AcceptanceIdentity {
  $json = Invoke-JavaScript -Expression @'
JSON.stringify(window.fruitDefenseAcceptanceIdentity ?? null)
'@
  if ([string]::IsNullOrWhiteSpace([string]$json) -or $json -eq 'null') { return $null }
  return $json | ConvertFrom-Json
}

function Assert-AcceptanceIdentity {
  param(
    [object]$Actual,
    [int]$Route,
    [string]$Stage,
    [ValidateSet('Required', 'Cleared')]
    [string]$SessionMode
  )
  if ($null -eq $Actual) { throw "Acceptance identity is missing at stage '$Stage'." }
  if ([int]$Actual.route -ne $Route) {
    throw "Acceptance route mismatch at '$Stage': expected=$Route actual=$($Actual.route)."
  }
  $expectedRouteName = @('lobby', 'battle', 'settlement')[$Route]
  if ([string]$Actual.routeName -cne $expectedRouteName) {
    throw "Acceptance route name mismatch at '$Stage': expected=$expectedRouteName actual=$($Actual.routeName)."
  }
  foreach ($field in @('levelId', 'mapId', 'waveSetId', 'ruleSetId', 'themeId')) {
    if ([string]$Actual.$field -cne [string]$expectedLevelIdentity[$field]) {
      throw (
        "Acceptance identity mismatch at '$Stage' field=$field " +
        "expected=$($expectedLevelIdentity[$field]) actual=$($Actual.$field).")
    }
  }
  if ($SessionMode -eq 'Required') {
    if ([string]::IsNullOrWhiteSpace([string]$Actual.sessionId) -or [int]$Actual.seed -eq 0) {
      throw "Acceptance session is incomplete at '$Stage': session=$($Actual.sessionId) seed=$($Actual.seed)."
    }
  }
  elseif (-not [string]::IsNullOrEmpty([string]$Actual.sessionId) -or [int]$Actual.seed -ne 0) {
    throw "Acceptance session was not cleared at '$Stage': session=$($Actual.sessionId) seed=$($Actual.seed)."
  }
  return $Actual
}

function Wait-AcceptanceIdentity {
  param(
    [int]$Route,
    [string]$Stage,
    [ValidateSet('Required', 'Cleared')]
    [string]$SessionMode
  )
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $actual = $null
  do {
    $actual = Get-AcceptanceIdentity
    if ($null -ne $actual -and [int]$actual.route -eq $Route) {
      $matchesComposite = $true
      foreach ($field in @('levelId', 'mapId', 'waveSetId', 'ruleSetId', 'themeId')) {
        if ([string]$actual.$field -cne [string]$expectedLevelIdentity[$field]) {
          $matchesComposite = $false
          break
        }
      }
      $matchesSession = if ($SessionMode -eq 'Required') {
        -not [string]::IsNullOrWhiteSpace([string]$actual.sessionId) -and [int]$actual.seed -ne 0
      }
      else {
        [string]::IsNullOrEmpty([string]$actual.sessionId) -and [int]$actual.seed -eq 0
      }
      if ($matchesComposite -and $matchesSession) {
        return Assert-AcceptanceIdentity -Actual $actual -Route $Route -Stage $Stage -SessionMode $SessionMode
      }
    }
    Start-Sleep -Milliseconds 200
  } while ((Get-Date) -lt $deadline)
  return Assert-AcceptanceIdentity -Actual $actual -Route $Route -Stage $Stage -SessionMode $SessionMode
}

function Assert-SameSession {
  param([object]$Expected, [object]$Actual, [string]$Stage)
  if ([string]$Expected.sessionId -cne [string]$Actual.sessionId -or
      [int]$Expected.seed -ne [int]$Actual.seed) {
    throw (
      "Acceptance session changed unexpectedly at '$Stage': " +
      "expected=$($Expected.sessionId)/$($Expected.seed) " +
      "actual=$($Actual.sessionId)/$($Actual.seed).")
  }
}

function Assert-FreshSession {
  param([object]$Previous, [object]$Actual, [string]$Stage)
  if ([string]::IsNullOrWhiteSpace([string]$Actual.sessionId) -or [int]$Actual.seed -eq 0 -or
      [string]$Previous.sessionId -ceq [string]$Actual.sessionId -or
      [int]$Previous.seed -eq [int]$Actual.seed) {
    throw (
      "Acceptance retry did not create a fresh session at '$Stage': " +
      "previous=$($Previous.sessionId)/$($Previous.seed) " +
      "actual=$($Actual.sessionId)/$($Actual.seed).")
  }
}

function Save-Screenshot {
  param([string]$Name)
  $capture = Invoke-Cdp -Method 'Page.captureScreenshot' -Params @{
    format = 'png'; fromSurface = $true; captureBeyondViewport = $false
  }
  $dataProperty = $capture.PSObject.Properties['data']
  if ($null -eq $dataProperty -or [string]::IsNullOrWhiteSpace([string]$dataProperty.Value)) {
    throw "Screenshot did not return image data: $Name"
  }
  $path = Join-Path $outputDir "$Name.png"
  [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String([string]$dataProperty.Value))
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
    $nearBlackSamples = 0
    $invalidSamples = 0
    $sampleCount = 0
    $formerActionColorPixels = 0
    $sampleStep = 4
    $maxNearBlackRunSamples = 0
    # These design-space regions are projected through the same safe-content transform as input controls.
    for ($y = $headerSampleRegion.yMin; $y -lt $headerSampleRegion.yMax; $y++) {
      for ($x = $headerSampleRegion.xMin; $x -lt $headerSampleRegion.xMax; $x++) {
        $pixel = $bitmap.GetPixel($x, $y)
        $luma = (.2126 * $pixel.R + .7152 * $pixel.G + .0722 * $pixel.B) / 255.0
        if ($pixel.A -gt 128 -and $luma -lt .48) { $darkPixels++ }
        if ($pixel.A -gt 128 -and $luma -gt .75) { $lightPixels++ }
      }
    }
    $frameSampleYMin = [Math]::Max(0, $SafeTop)
    $frameSampleYMax = [Math]::Min($bitmap.Height, $bitmap.Height - $SafeBottom)
    for ($y = $frameSampleYMin; $y -lt $frameSampleYMax; $y += $sampleStep) {
      $nearBlackRunSamples = 0
      for ($x = 0; $x -lt $bitmap.Width; $x += $sampleStep) {
        $pixel = $bitmap.GetPixel($x, $y)
        $luma = (.2126 * $pixel.R + .7152 * $pixel.G + .0722 * $pixel.B) / 255.0
        $isNearBlack = $pixel.A -gt 128 -and $luma -lt $nearBlackLumaThreshold
        if ($isNearBlack) {
          $nearBlackSamples++
          $nearBlackRunSamples++
          $maxNearBlackRunSamples = [Math]::Max($maxNearBlackRunSamples, $nearBlackRunSamples)
        }
        else { $nearBlackRunSamples = 0 }
        $sampleCount++
        if ($pixel.A -gt 128 -and $luma -lt .025) { $blackSamples++ }
        if ($pixel.A -le 128 -or $luma -lt .025) { $invalidSamples++ }
      }
    }
    $maxNearBlackRunFraction = [Math]::Min(
      1.0, $maxNearBlackRunSamples * $sampleStep / [double]$bitmap.Width)
    # The removed persistent action row occupied reference rect x=8..394, y=760..810.
    for ($y = $formerActionRegion.yMin; $y -lt [Math]::Min($formerActionRegion.yMax, $bitmap.Height); $y++) {
      for ($x = $formerActionRegion.xMin; $x -lt [Math]::Min($formerActionRegion.xMax, $bitmap.Width); $x++) {
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
      nearBlackFraction = if ($sampleCount -gt 0) { $nearBlackSamples / [double]$sampleCount } else { 1.0 }
      maxNearBlackHorizontalRunFraction = $maxNearBlackRunFraction
      invalidFraction = if ($sampleCount -gt 0) { $invalidSamples / [double]$sampleCount } else { 1.0 }
      formerActionColorPixels = $formerActionColorPixels
      sampledRegions = [ordered]@{
        header = $headerSampleRegion
        formerAction = $formerActionRegion
        frameContent = [ordered]@{
          xMin = 0
          yMin = $frameSampleYMin
          xMax = $bitmap.Width
          yMax = $frameSampleYMax
        }
      }
    }
  }
  finally { $bitmap.Dispose() }
}

function Test-StableFrameMetrics {
  param([object]$Metrics)
  return $Metrics.invalidFraction -lt .05 -and
    $Metrics.blackFraction -lt $maxBlackFraction -and
    $Metrics.nearBlackFraction -lt $maxNearBlackFraction
}

function Save-StableScreenshot {
  param([string]$Name, [bool]$RequireHud = $true)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $attempts = 0
  do {
    $attempts++
    $path = Save-Screenshot -Name $Name
    $metrics = Get-ImageMetrics -Path $path
    $dimensionsOk = $metrics.width -eq $Width -and $metrics.height -eq $Height
    $frameOk = Test-StableFrameMetrics -Metrics $metrics
    $hudOk = -not $RequireHud -or (
      $metrics.headerDarkPixels -ge $hudDarkPixelThreshold -and
      $metrics.headerLightPixels -ge $hudLightPixelThreshold)
    if ($dimensionsOk -and $frameOk -and $hudOk) {
      return [pscustomobject]@{ Path = $path; Metrics = $metrics }
    }
    Write-Warning (
      "Retrying unstable screenshot '$Name' attempt=$attempts " +
      "dimensions=$($metrics.width)x$($metrics.height) " +
      "invalid=$($metrics.invalidFraction) black=$($metrics.blackFraction) " +
      "nearBlack=$($metrics.nearBlackFraction) " +
      "maxNearBlackRun=$($metrics.maxNearBlackHorizontalRunFraction)")
    Start-Sleep -Milliseconds 500
  } while ((Get-Date) -lt $deadline)
  throw "Stable screenshot timed out: $Name metrics=$($metrics | ConvertTo-Json -Compress)"
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

$mappedDesignBounds = [ordered]@{
  xMin = $referenceOffsetX
  yMin = $referenceOffsetY
  xMax = $referenceOffsetX + $referenceWidth * $referenceScale
  yMax = $referenceOffsetY + $referenceHeight * $referenceScale
}
if ($mappedDesignBounds.xMin -lt -0.001 -or $mappedDesignBounds.yMin -lt ($SafeTop - 0.001) -or
    $mappedDesignBounds.xMax -gt ($Width + 0.001) -or
    $mappedDesignBounds.yMax -gt ($Height - $SafeBottom + 0.001)) {
  throw "Mapped 402x874 design viewport escapes safe content: $($mappedDesignBounds | ConvertTo-Json -Compress)"
}

if ($SelfCheck) {
  $syntheticHealthyMetrics = [pscustomobject]@{
    invalidFraction = 0.0
    blackFraction = 0.0
    nearBlackFraction = 0.0005
    maxNearBlackHorizontalRunFraction = 0.01
  }
  $syntheticBlackBlockMetrics = [pscustomobject]@{
    invalidFraction = 0.02
    blackFraction = 0.02
    nearBlackFraction = 0.03
    maxNearBlackHorizontalRunFraction = 0.25
  }
  $syntheticNearBlackBlockMetrics = [pscustomobject]@{
    invalidFraction = 0.0
    blackFraction = 0.0
    nearBlackFraction = 0.06
    maxNearBlackHorizontalRunFraction = 0.25
  }
  if (-not (Test-StableFrameMetrics -Metrics $syntheticHealthyMetrics) -or
      (Test-StableFrameMetrics -Metrics $syntheticBlackBlockMetrics) -or
      (Test-StableFrameMetrics -Metrics $syntheticNearBlackBlockMetrics)) {
    throw 'Black-frame stability guard self-check failed.'
  }
  foreach ($controlName in @(
      'lobbyLevelOrchard01', 'lobbyLevelOrchard02', 'lobbyLevelOrchard03',
      'lobbyStart', 'settlementRetry', 'settlementReturn')) {
    $point = $controls[$controlName]
    if ($point.x -lt 0 -or $point.x -gt $Width -or
        $point.y -lt $SafeTop -or $point.y -gt ($Height - $SafeBottom)) {
      throw "Mapped shell control escapes safe content: $controlName=$($point | ConvertTo-Json -Compress)"
    }
  }
  if (-not ($controls.lobbyLevelOrchard01.y -lt $controls.lobbyLevelOrchard02.y -and
      $controls.lobbyLevelOrchard02.y -lt $controls.lobbyLevelOrchard03.y -and
      $controls.lobbyLevelOrchard03.y -lt $controls.lobbyStart.y)) {
    throw 'Mapped Lobby card and Start controls are not ordered.'
  }
  if ($expectedLevelIdentity.Count -ne 5) {
    throw "Expected composite identity is incomplete for level '$LevelId'."
  }
  $syntheticBattleIdentity = [pscustomobject]@{
    route = 1; routeName = 'battle'; sessionId = 'session-a'; seed = 101
    levelId = $expectedLevelIdentity.levelId; mapId = $expectedLevelIdentity.mapId
    waveSetId = $expectedLevelIdentity.waveSetId; ruleSetId = $expectedLevelIdentity.ruleSetId
    themeId = $expectedLevelIdentity.themeId
  }
  $syntheticSettlementIdentity = [pscustomobject]@{
    route = 2; routeName = 'settlement'; sessionId = 'session-a'; seed = 101
    levelId = $expectedLevelIdentity.levelId; mapId = $expectedLevelIdentity.mapId
    waveSetId = $expectedLevelIdentity.waveSetId; ruleSetId = $expectedLevelIdentity.ruleSetId
    themeId = $expectedLevelIdentity.themeId
  }
  $syntheticLobbyIdentity = [pscustomobject]@{
    route = 0; routeName = 'lobby'; sessionId = ''; seed = 0
    levelId = $expectedLevelIdentity.levelId; mapId = $expectedLevelIdentity.mapId
    waveSetId = $expectedLevelIdentity.waveSetId; ruleSetId = $expectedLevelIdentity.ruleSetId
    themeId = $expectedLevelIdentity.themeId
  }
  $syntheticRetryIdentity = [pscustomobject]@{
    route = 1; routeName = 'battle'; sessionId = 'session-b'; seed = 202
    levelId = $expectedLevelIdentity.levelId; mapId = $expectedLevelIdentity.mapId
    waveSetId = $expectedLevelIdentity.waveSetId; ruleSetId = $expectedLevelIdentity.ruleSetId
    themeId = $expectedLevelIdentity.themeId
  }
  Assert-AcceptanceIdentity -Actual $syntheticBattleIdentity `
    -Route 1 -Stage 'self-check-battle' -SessionMode Required | Out-Null
  Assert-AcceptanceIdentity -Actual $syntheticSettlementIdentity `
    -Route 2 -Stage 'self-check-settlement' -SessionMode Required | Out-Null
  Assert-AcceptanceIdentity -Actual $syntheticLobbyIdentity `
    -Route 0 -Stage 'self-check-lobby' -SessionMode Cleared | Out-Null
  Assert-SameSession -Expected $syntheticBattleIdentity `
    -Actual $syntheticSettlementIdentity -Stage 'self-check-settlement'
  Assert-FreshSession -Previous $syntheticSettlementIdentity `
    -Actual $syntheticRetryIdentity -Stage 'self-check-retry'
  $syntheticWarmRun = [ordered]@{
    totalPayloadTransferSize = 1200
    assets = [ordered]@{
      loader = [ordered]@{ transferSize = 300 }
      data = [ordered]@{ transferSize = 300 }
      framework = [ordered]@{ transferSize = 300 }
      wasm = [ordered]@{ transferSize = 300 }
    }
  }
  Assert-WarmCacheTransfer -WarmRun $syntheticWarmRun
  $syntheticWarmRun.assets.data.transferSize = $warmAssetTransferLimitBytes + 1
  $warmCacheGuardRejectedBody = $false
  try { Assert-WarmCacheTransfer -WarmRun $syntheticWarmRun }
  catch { $warmCacheGuardRejectedBody = $true }
  if (-not $warmCacheGuardRejectedBody) {
    throw 'Warm-cache transfer guard self-check failed.'
  }
  $syntheticSeedManifest = [pscustomobject]@{
    schemaVersion = 1
    evidenceType = 'webgl-cache-seed'
    url = 'http://example.test/'
    delivery = [pscustomobject]@{
      assetVersions = [pscustomobject]@{ loader = 'aaaaaaaaaaaa'; data = 'bbbbbbbbbbbb'; framework = 'cccccccccccc'; wasm = 'dddddddddddd' }
      assets = [pscustomobject]@{
        loader = [pscustomobject]@{ version = 'aaaaaaaaaaaa' }
        data = [pscustomobject]@{ version = 'bbbbbbbbbbbb' }
        framework = [pscustomobject]@{ version = 'cccccccccccc' }
        wasm = [pscustomobject]@{ version = 'dddddddddddd' }
      }
    }
  }
  $syntheticCandidateDelivery = [ordered]@{
    assetVersions = [ordered]@{ loader = 'aaaaaaaaaaaa'; data = 'eeeeeeeeeeee'; framework = 'cccccccccccc'; wasm = 'dddddddddddd' }
    assets = [ordered]@{
      loader = [ordered]@{ version = 'aaaaaaaaaaaa'; contentLength = 10 }
      data = [ordered]@{ version = 'eeeeeeeeeeee'; contentLength = 200 }
      framework = [ordered]@{ version = 'cccccccccccc'; contentLength = 30 }
      wasm = [ordered]@{ version = 'dddddddddddd'; contentLength = 40 }
    }
  }
  $syntheticCandidateRun = [ordered]@{
    totalPayloadTransferSize = 200
    assets = [ordered]@{
      loader = [ordered]@{ transferSize = 0 }
      data = [ordered]@{ transferSize = 200 }
      framework = [ordered]@{ transferSize = 0 }
      wasm = [ordered]@{ transferSize = 0 }
    }
  }
  $script:CacheSeedManifestPath = $PSCommandPath
  $transition = Get-ReleaseTransitionEvidence `
    -SeedManifest $syntheticSeedManifest `
    -CandidateDelivery $syntheticCandidateDelivery `
    -CandidateRun $syntheticCandidateRun
  if ($transition.expectedDownloadBytes -ne 200 -or $transition.changedRoles.Count -ne 1 -or
      $transition.changedRoles[0] -ne 'data' -or $transition.reusedRoles.Count -ne 3) {
    throw 'Cross-release payload classification self-check failed.'
  }
  [ordered]@{
    levelId = $LevelId
    expectedCompositeIdentity = $expectedLevelIdentity
    viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
    safeArea = $safeAreaEvidence
    mappedDesignBounds = $mappedDesignBounds
    referenceControls = $referenceControls
    mappedControls = $controls
    sampledRegions = [ordered]@{ header = $headerSampleRegion; formerAction = $formerActionRegion }
    thresholds = [ordered]@{
      hudDarkPixels = $hudDarkPixelThreshold
      hudLightPixels = $hudLightPixelThreshold
      formerActionColorPixels = $formerActionPixelThreshold
      framePixels = $framePixelThresholds
    }
    blackFrameGuard = 'pass'
    shellControlMapping = 'pass'
    compositeIdentityContract = 'pass'
     sessionLifecycleContract = 'pass'
     warmCacheTransferGuard = 'pass'
     crossReleaseCacheGuard = 'pass'
  } | ConvertTo-Json -Depth 8
  Write-Host 'FRUIT_DEFENSE_ACCEPTANCE_SELF_CHECK_OK'
  return
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
    $Url = "http://127.0.0.1:$serverPort/"
  }
  elseif ([string]::IsNullOrWhiteSpace($Url)) {
    throw 'Provide -Url or use -ServeLocal.'
  }
  $Url = Set-AcceptanceQuery -TargetUrl $Url

  $pageResponse = Wait-Http -TargetUrl $Url -Seconds $TimeoutSeconds
  $delivery = Get-UnityDeliveryMetadata -PageUrl $Url -PageResponse $pageResponse

  $debugPort = Get-FreeTcpPort
  $chromeArgs = @(
    '--headless=new', '--no-first-run', '--disable-background-networking', '--disable-extensions',
    '--hide-scrollbars', '--use-angle=swiftshader', '--enable-webgl', '--ignore-gpu-blocklist',
    '--user-agent="Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1"',
    "--window-size=$Width,$Height", '--force-device-scale-factor=1',
    "--remote-debugging-port=$debugPort", "--user-data-dir=$profileDir", $Url
  )
  $coldStartedAt = [DateTimeOffset]::UtcNow
  $chromeProcess = Start-Process -FilePath $ChromePath -ArgumentList $chromeArgs -WindowStyle Hidden -PassThru

  $debugUrl = "http://127.0.0.1:$debugPort/json"
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    try {
      $rawTargets = Invoke-RestMethod -Uri $debugUrl -TimeoutSec 3
      $target = $null
      foreach ($candidate in $rawTargets) {
        $typeProperty = $candidate.PSObject.Properties['type']
        $urlProperty = $candidate.PSObject.Properties['url']
        if ($null -ne $typeProperty -and $null -ne $urlProperty -and
            [string]$typeProperty.Value -eq 'page' -and [string]$urlProperty.Value -eq $Url) {
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
  $debuggerProperty = $target.PSObject.Properties['webSocketDebuggerUrl']
  if ($null -eq $debuggerProperty -or
      [string]::IsNullOrWhiteSpace([string]$debuggerProperty.Value)) {
    throw 'Chrome DevTools page target has no websocket debugger URL.'
  }
  $debuggerUrl = [string]$debuggerProperty.Value
  try { $socket.ConnectAsync([Uri]$debuggerUrl, $connectCts.Token).GetAwaiter().GetResult() | Out-Null }
  finally { $connectCts.Dispose() }
  Invoke-Cdp -Method 'Page.enable' | Out-Null
  Invoke-Cdp -Method 'Runtime.enable' | Out-Null
  $browserVersion = Invoke-Cdp -Method 'Browser.getVersion'
  $browserEvidence = [ordered]@{
    product = $browserVersion.product
    userAgent = $browserVersion.userAgent
    protocolVersion = $browserVersion.protocolVersion
    jsVersion = $browserVersion.jsVersion
    executable = (Resolve-Path -LiteralPath $ChromePath).Path
    launchMode = 'external-headless-chrome-cdp'
  }
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
  const canvasRect = canvas ? canvas.getBoundingClientRect() : null;
  const loading = document.querySelector('#unity-loading-bar');
  const warning = document.querySelector('#unity-warning');
  return JSON.stringify({
    href: location.href,
    title: document.title,
    canvas: !!canvas,
    width: canvas ? canvas.width : 0,
    height: canvas ? canvas.height : 0,
    cssWidth: canvasRect ? canvasRect.width : 0,
    cssHeight: canvasRect ? canvasRect.height : 0,
    innerWidth: window.innerWidth,
    innerHeight: window.innerHeight,
    devicePixelRatio: window.devicePixelRatio,
    timeOrigin: performance.timeOrigin,
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
    $viewportReady = $readiness.innerWidth -eq $Width -and $readiness.innerHeight -eq $Height
    $canvasReady = $readiness.width -eq $Width -and $readiness.height -eq $Height -and
      [Math]::Abs([double]$readiness.cssWidth - $Width) -lt 0.51 -and
      [Math]::Abs([double]$readiness.cssHeight - $Height) -lt 0.51
    if ($readiness.canvas -and $readiness.loading -eq 'none' -and $readiness.acceptanceReady -and
        $viewportReady -and $canvasReady) { break }
    Start-Sleep -Milliseconds 400
  } while ((Get-Date) -lt $deadline)
  if (-not $readiness.canvas -or $readiness.loading -ne 'none' -or -not $readiness.acceptanceReady) {
    throw "Unity player did not finish loading. state=$($readiness | ConvertTo-Json -Compress)"
  }
  if ($readiness.warning) { throw "Unity player warning: $($readiness.warning)" }
  if (-not $viewportReady -or -not $canvasReady) {
    throw "Chrome viewport or Unity canvas did not resolve to ${Width}x${Height}. state=$($readiness | ConvertTo-Json -Compress)"
  }

  $coldCacheRun = Get-UnityResourceTiming `
    -Label 'cold' `
    -DeliveryAssets $delivery.assets `
    -StartedAt $coldStartedAt
  if ($CacheSeedOnly) {
    $seedManifest = [ordered]@{
      schemaVersion = 1
      evidenceType = 'webgl-cache-seed'
      accepted = $true
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      browser = $browserEvidence
      delivery = $delivery
      cacheRun = $coldCacheRun
      profilePath = $profileDir
      checks = [ordered]@{
        unityLoaded = 'pass'
        perAssetContentVersions = 'pass'
        strongContentEtags = 'pass'
        cacheSeedPersisted = 'pass'
      }
    }
    $seedManifestPath = Join-Path $outputDir 'cache-seed.json'
    $seedManifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $seedManifestPath -Encoding UTF8
    Write-Host "FRUIT_DEFENSE_CACHE_SEED_OK manifest=$seedManifestPath"
    return
  }

  $releaseTransition = $null
  if (-not [string]::IsNullOrWhiteSpace($CacheSeedManifestPath)) {
    if (-not (Test-Path -LiteralPath $CacheSeedManifestPath -PathType Leaf)) {
      throw "WebGL cache seed manifest not found: $CacheSeedManifestPath"
    }
    $seedManifest = Get-Content -LiteralPath $CacheSeedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $releaseTransition = Get-ReleaseTransitionEvidence `
      -SeedManifest $seedManifest `
      -CandidateDelivery $delivery `
      -CandidateRun $coldCacheRun
  }
  $coldTimeOrigin = [double]$readiness.timeOrigin
  $warmStartedAt = [DateTimeOffset]::UtcNow
  Invoke-Cdp -Method 'Page.reload' -Params @{ ignoreCache = $false } | Out-Null

  $warmReadiness = $null
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    try {
      $candidateReadiness = (Invoke-JavaScript -Expression $readinessExpression) | ConvertFrom-Json
      $newDocument = [Math]::Abs([double]$candidateReadiness.timeOrigin - $coldTimeOrigin) -gt 0.001
      $warmViewportReady = $candidateReadiness.innerWidth -eq $Width -and $candidateReadiness.innerHeight -eq $Height
      $warmCanvasReady = $candidateReadiness.width -eq $Width -and $candidateReadiness.height -eq $Height -and
        [Math]::Abs([double]$candidateReadiness.cssWidth - $Width) -lt 0.51 -and
        [Math]::Abs([double]$candidateReadiness.cssHeight - $Height) -lt 0.51
      if ($newDocument -and $candidateReadiness.canvas -and
          $candidateReadiness.loading -eq 'none' -and $candidateReadiness.acceptanceReady -and
          $warmViewportReady -and $warmCanvasReady) {
        $warmReadiness = $candidateReadiness
        break
      }
    }
    catch {
      # Navigation can temporarily destroy the Runtime execution context.
    }
    Start-Sleep -Milliseconds 400
  } while ((Get-Date) -lt $deadline)
  if ($null -eq $warmReadiness) {
    throw 'Warm WebGL reload did not finish in a new browser document.'
  }
  if ($warmReadiness.warning) { throw "Unity player warning after warm reload: $($warmReadiness.warning)" }

  $readiness = $warmReadiness
  $warmCacheRun = Get-UnityResourceTiming `
    -Label 'warm' `
    -DeliveryAssets $delivery.assets `
    -StartedAt $warmStartedAt
  Assert-WarmCacheTransfer -WarmRun $warmCacheRun
  $delivery['cacheLimits'] = [ordered]@{
    perAssetTransferBytes = $warmAssetTransferLimitBytes
    totalTransferBytes = $warmTotalTransferLimitBytes
  }
  $delivery['cacheRuns'] = [ordered]@{
    cold = $coldCacheRun
    warm = $warmCacheRun
  }
  $delivery['releaseTransition'] = if ($null -eq $releaseTransition) {
    [ordered]@{ state = 'not-requested' }
  } else {
    $releaseTransition
  }

  $screenshots = [ordered]@{}

  if ($Flow) {
    Wait-AppRoute -Route 0
    $flowScreenshots = [ordered]@{}
    $flowMetrics = [ordered]@{}
    $flowIdentities = [ordered]@{}

    Invoke-CanvasClick -X $controls[$levelCardControlName].x -Y $controls[$levelCardControlName].y
    $flowIdentities.lobby = Wait-AcceptanceIdentity `
      -Route 0 -Stage 'selected-lobby' -SessionMode Cleared
    $flowScreenshots.lobby = (Save-StableScreenshot -Name '01-lobby' -RequireHud $false).Path

    Invoke-CanvasClick -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
    Wait-AppRoute -Route 1
    $flowIdentities.battle = Wait-AcceptanceIdentity `
      -Route 1 -Stage 'battle' -SessionMode Required
    $flowScreenshots.battle = (Save-StableScreenshot -Name '02-battle' -RequireHud $true).Path

    Invoke-AcceptanceFlowCommand -Command 'victory'
    Wait-AppRoute -Route 2
    $flowIdentities.settlement = Wait-AcceptanceIdentity `
      -Route 2 -Stage 'settlement' -SessionMode Required
    Assert-SameSession -Expected $flowIdentities.battle `
      -Actual $flowIdentities.settlement -Stage 'settlement'
    $flowScreenshots.settlement = (Save-StableScreenshot -Name '03-settlement' -RequireHud $false).Path

    Invoke-CanvasClick -X $controls.settlementReturn.x -Y $controls.settlementReturn.y
    Wait-AppRoute -Route 0
    $flowIdentities.returnedLobby = Wait-AcceptanceIdentity `
      -Route 0 -Stage 'returned-lobby' -SessionMode Cleared
    $flowScreenshots.returnedLobby = (Save-StableScreenshot -Name '04-returned-lobby' -RequireHud $false).Path

    Invoke-CanvasClick -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
    Wait-AppRoute -Route 1
    $flowIdentities.secondBattle = Wait-AcceptanceIdentity `
      -Route 1 -Stage 'second-battle' -SessionMode Required
    if ([string]$flowIdentities.secondBattle.sessionId -ceq [string]$flowIdentities.battle.sessionId) {
      throw 'Returning to Lobby and starting again reused the completed session ID.'
    }
    Invoke-AcceptanceFlowCommand -Command 'victory'
    Wait-AppRoute -Route 2
    $flowIdentities.secondSettlement = Wait-AcceptanceIdentity `
      -Route 2 -Stage 'second-settlement' -SessionMode Required
    Assert-SameSession -Expected $flowIdentities.secondBattle `
      -Actual $flowIdentities.secondSettlement -Stage 'second-settlement'
    Invoke-CanvasClick -X $controls.settlementRetry.x -Y $controls.settlementRetry.y
    Wait-AppRoute -Route 1
    $flowIdentities.retryBattle = Wait-AcceptanceIdentity `
      -Route 1 -Stage 'retry-battle' -SessionMode Required
    Assert-FreshSession -Previous $flowIdentities.secondSettlement `
      -Actual $flowIdentities.retryBattle -Stage 'retry-battle'
    $flowScreenshots.retryBattle = (Save-StableScreenshot -Name '05-retry-battle' -RequireHud $true).Path

    foreach ($state in $flowScreenshots.Keys) {
      $flowMetrics[$state] = Get-ImageMetrics -Path $flowScreenshots[$state]
      if ($flowMetrics[$state].width -ne $Width -or $flowMetrics[$state].height -ne $Height) {
        throw "Unexpected flow screenshot dimensions for ${state}: $($flowMetrics[$state].width)x$($flowMetrics[$state].height)"
      }
      if (-not (Test-StableFrameMetrics -Metrics $flowMetrics[$state])) {
        throw (
          "Invalid flow frame for ${state}: black=$($flowMetrics[$state].blackFraction) " +
          "nearBlack=$($flowMetrics[$state].nearBlackFraction) " +
          "maxNearBlackRun=$($flowMetrics[$state].maxNearBlackHorizontalRunFraction) " +
          "invalid=$($flowMetrics[$state].invalidFraction)")
      }
    }

    $flowManifest = [ordered]@{
      accepted = $true
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      levelId = $LevelId
      expectedCompositeIdentity = $expectedLevelIdentity
      viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
      safeArea = $safeAreaEvidence
      browser = $browserEvidence
      canvas = $readiness
      checks = [ordered]@{
        lobbyToBattle = 'pass'
        battleToSettlement = 'pass'
        settlementReturn = 'pass'
        settlementRetry = 'pass'
        selectedLevelLaunch = 'pass'
        compositeIdentityPerRoute = 'pass'
        settlementSessionPreserved = 'pass'
        returnSelectionPreserved = 'pass'
        retryFreshSessionAndSeed = 'pass'
        requestedViewportAndCanvas = 'pass'
        safeAreaQueryApplied = 'pass'
        noBlackOrTransparentFrames = 'pass'
        noLargeNearBlackRegions = 'pass'
        perAssetContentVersions = 'pass'
        strongContentEtags = 'pass'
        warmCacheReuse = 'pass'
        crossReleaseCacheReuse = if ($null -eq $releaseTransition) { 'not-requested' } else { 'pass' }
      }
      delivery = $delivery
      routeIdentities = $flowIdentities
      screenshots = $flowScreenshots
      imageMetrics = $flowMetrics
      pixelThresholds = [ordered]@{
        framePixels = $framePixelThresholds
        hudDarkPixels = $hudDarkPixelThreshold
        hudLightPixels = $hudLightPixelThreshold
      }
      controls = $controls
      referenceControls = $referenceControls
    }
    $flowManifestPath = Join-Path $outputDir 'flow-acceptance.json'
    $flowManifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $flowManifestPath -Encoding UTF8
    Write-Host "FRUIT_DEFENSE_FLOW_ACCEPTANCE_OK manifest=$flowManifestPath"
    return
  }

  Wait-AppRoute -Route 1
  $directBattleIdentity = Wait-AcceptanceIdentity `
    -Route 1 -Stage 'direct-battle' -SessionMode Required
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
  Start-CanvasDrag -FromX $controls.nurserySlot0.x -FromY $controls.nurserySlot0.y -ToX $controls.acceptanceCell0.x -ToY $controls.acceptanceCell0.y
  $screenshots.dragTarget = (Save-StableScreenshot -Name '09-drag-target').Path
  Stop-CanvasDrag -X $controls.acceptanceCell0.x -Y $controls.acceptanceCell0.y

  Set-AcceptanceState -State 'dense-board'
  $screenshots.denseBoard = (Save-StableScreenshot -Name '10-dense-board').Path

  Set-AcceptanceState -State 'selection-inspection'
  # Deterministic interaction state projected through the enlarged board: attacking plant and empty pot use the first two canonical plantable cells.
  Invoke-CanvasClick -X $controls.acceptanceCell0.x -Y $controls.acceptanceCell0.y
  $screenshots.inspectionClick = (Save-StableScreenshot -Name '11-inspection-click').Path
  Invoke-CanvasClick -X $controls.acceptanceCell1.x -Y $controls.acceptanceCell1.y
  $screenshots.destinationClickNoMove = (Save-StableScreenshot -Name '12-destination-click-no-move').Path
  Start-CanvasDrag -FromX $controls.acceptanceCell0.x -FromY $controls.acceptanceCell0.y -ToX $controls.acceptanceCell1.x -ToY $controls.acceptanceCell1.y
  Stop-CanvasDrag -X $controls.acceptanceCell1.x -Y $controls.acceptanceCell1.y
  $screenshots.dragRelocation = (Save-StableScreenshot -Name '13-after-drag-move').Path

  $metrics = [ordered]@{}
  foreach ($state in $screenshots.Keys) {
    $metrics[$state] = Get-ImageMetrics -Path $screenshots[$state]
    if ($metrics[$state].width -ne $Width -or $metrics[$state].height -ne $Height) {
      throw "Unexpected screenshot dimensions for ${state}: $($metrics[$state].width)x$($metrics[$state].height)"
    }
    if (-not (Test-StableFrameMetrics -Metrics $metrics[$state])) {
      throw (
        "Frame stability check failed for ${state}: black=$($metrics[$state].blackFraction) " +
        "nearBlack=$($metrics[$state].nearBlackFraction) " +
        "maxNearBlackRun=$($metrics[$state].maxNearBlackHorizontalRunFraction) " +
        "invalid=$($metrics[$state].invalidFraction)")
    }
  }
  if ($metrics.ready.headerDarkPixels -lt $hudDarkPixelThreshold -or
      $metrics.ready.headerLightPixels -lt $hudLightPixelThreshold) {
    throw "HUD text check failed: dark=$($metrics.ready.headerDarkPixels)/$hudDarkPixelThreshold light=$($metrics.ready.headerLightPixels)/$hudLightPixelThreshold."
  }
  if ($metrics.ready.formerActionColorPixels -gt $formerActionPixelThreshold -or
      $metrics.activeWave.formerActionColorPixels -gt $formerActionPixelThreshold) {
    throw "Former bottom action-row colors are still present: ready=$($metrics.ready.formerActionColorPixels) active=$($metrics.activeWave.formerActionColorPixels)."
  }

  $manifest = [ordered]@{
    accepted = $true
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    url = $Url
    levelId = $LevelId
    expectedCompositeIdentity = $expectedLevelIdentity
    viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
    safeArea = $safeAreaEvidence
    browser = $browserEvidence
    canvas = $readiness
      checks = [ordered]@{
        http = 'pass'
        wasm = 'pass'
        perAssetContentVersions = 'pass'
        strongContentEtags = 'pass'
        brotliFallbackDelivery = 'pass'
        immutableBuildCache = 'pass'
        revalidatableHtml = 'pass'
        warmCacheReuse = 'pass'
        crossReleaseCacheReuse = if ($null -eq $releaseTransition) { 'not-requested' } else { 'pass' }
      unityLoaded = 'pass'
      directRouteLevelIdentity = 'pass'
      compositeIdentity = 'pass'
      requestedViewportAndCanvas = 'pass'
      safeAreaQueryApplied = 'pass'
      chineseHudInk = 'pass'
      screenshotDimensions = 'pass'
      requiredStates = 'pass'
      contextualWaveLabels = 'pass'
      oldBottomActionRowAbsent = 'pass'
      noLargeNearBlackRegions = 'pass'
      pauseContinuePreservesRun = 'pass'
      pauseRestartProducesCleanReadyState = 'pass'
      inspectionClickInformationAndRange = 'pass'
      destinationClickNoRelocation = 'pass'
      dragRelocation = 'pass'
    }
    delivery = $delivery
    routeIdentities = [ordered]@{ battle = $directBattleIdentity }
    screenshots = $screenshots
    imageMetrics = $metrics
    controls = $controls
    referenceControls = $referenceControls
    pixelThresholds = [ordered]@{
      hudDarkPixels = $hudDarkPixelThreshold
      hudLightPixels = $hudLightPixelThreshold
      formerActionColorPixels = $formerActionPixelThreshold
      framePixels = $framePixelThresholds
    }
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
      source = [ordered]@{ cell = @(0, 1); x = $controls.acceptanceCell0.x; y = $controls.acceptanceCell0.y }
      destination = [ordered]@{ cell = @(1, 1); x = $controls.acceptanceCell1.x; y = $controls.acceptanceCell1.y }
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
  Stop-OwnedChrome
  if ($socket) { $socket.Dispose() }
  if ($serverProcess -and -not $serverProcess.HasExited) {
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
  }
  $tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
  $resolvedProfile = [IO.Path]::GetFullPath($profileDir)
  if ($ownsProfile -and $resolvedProfile.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $resolvedProfile -Recurse -Force -ErrorAction SilentlyContinue
  }
}
