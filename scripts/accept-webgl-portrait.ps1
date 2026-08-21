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
  [ValidateSet('victory', 'defeat')]
  [string]$SettlementOutcome = 'victory',
  [ValidateSet('victory', 'defeat')]
  [string]$BattleTerminalOutcome = 'victory',
  [switch]$ShellVisual,
  [switch]$InteractionPolishEvidence,
  [switch]$ShellError,
  [string]$ErrorLevelId = '__missing-ui-acceptance__',
  [ValidateRange(1, 20)]
  [int]$BootstrapCpuThrottlingRate = 8,
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
if ((@($Flow, $ShellVisual, $ShellError) | Where-Object { $_ }).Count -gt 1) {
  throw '-Flow, -ShellVisual, and -ShellError are distinct acceptance modes and cannot be combined.'
}
if ($ShellVisual -and $LevelId -eq 'orchard-01') {
  throw '-ShellVisual requires -LevelId orchard-02 or orchard-03 for alternate-selection evidence.'
}
if ($InteractionPolishEvidence -and $ShellError) {
  throw '-InteractionPolishEvidence is not available with -ShellError.'
}
if ($ShellError -and [string]::IsNullOrWhiteSpace($ErrorLevelId)) {
  throw '-ShellError requires a non-empty -ErrorLevelId.'
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
  if ($ShellError -or (-not $Flow -and -not $ShellVisual)) {
    $result = Set-UrlQueryParameter -TargetUrl $result -Name 'route' -Value 'battle'
  }
  $queryLevelId = if ($ShellError) { $ErrorLevelId } else { $LevelId }
  $result = Set-UrlQueryParameter -TargetUrl $result -Name 'levelId' -Value $queryLevelId
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
  lobbyLevelOrchard01 = [ordered]@{ x = 201; y = 206 }
  lobbyLevelOrchard02 = [ordered]@{ x = 201; y = 392 }
  lobbyLevelOrchard03 = [ordered]@{ x = 201; y = 578 }
  lobbyStart = [ordered]@{ x = 201; y = 746 }
  settlementRetry = [ordered]@{ x = 201; y = 674 }
  settlementReturn = [ordered]@{ x = 201; y = 754 }
  headerPause = [ordered]@{ x = 300; y = 46 }
  waveAction = [ordered]@{ x = 302; y = 548 }
  pauseContinue = [ordered]@{ x = 125; y = 492 }
  pauseRestart = [ordered]@{ x = 277; y = 492 }
  terminalRestart = [ordered]@{ x = 201; y = 536 }
  weaponGatling = [ordered]@{ x = 60; y = 624 }
  nurserySlot0 = [ordered]@{ x = 51; y = 705 }
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
$lobbyStartRect = [ordered]@{
  xMin = [Math]::Floor($shellContentX)
  yMin = [Math]::Floor($shellContentY + 690.0 * $referenceScale)
  xMax = [Math]::Ceiling($shellContentX + $shellContentWidth)
  yMax = [Math]::Ceiling($shellContentY + (690.0 + 76.0) * $referenceScale)
}
$lobbyLevelCardOffset = switch ($LevelId) {
  'orchard-01' { 100.0 }
  'orchard-02' { 286.0 }
  'orchard-03' { 472.0 }
  default { throw "No Lobby card rect is defined for level '$LevelId'." }
}
$lobbyAlternateCardRect = [ordered]@{
  # The approved selectable-card Sprite preserves transparent optical padding:
  # visible left/right outline ink begins 6/4 logical points inside the layout rect.
  xMin = [Math]::Floor($shellContentX + 6.0 * $referenceScale)
  yMin = [Math]::Floor($shellContentY + $lobbyLevelCardOffset * $referenceScale)
  xMax = [Math]::Ceiling(
    $shellContentX + $shellContentWidth - 4.0 * $referenceScale)
  yMax = [Math]::Ceiling(
    $shellContentY + ($lobbyLevelCardOffset + 176.0) * $referenceScale)
}
$headerSampleRegion = Convert-ReferenceRect -X 13 -Y 11 -Width 250 -Height 53
$formerActionRegion = Convert-ReferenceRect -X 8 -Y 760 -Width 386 -Height 50
$waveActionRect = Convert-ReferenceRect -X 210 -Y 526 -Width 184 -Height 44
$pauseTitleRect = Convert-ReferenceRect -X 52 -Y 326 -Width 298 -Height 52
$pauseTitleInkRegion = Convert-ReferenceRect -X 90 -Y 332 -Width 220 -Height 40
$pauseHintRect = Convert-ReferenceRect -X 60 -Y 390 -Width 282 -Height 52
$pauseHintIconRegion = Convert-ReferenceRect -X 102 -Y 398 -Width 26 -Height 36
$pauseHintCopyRegion = Convert-ReferenceRect -X 130 -Y 398 -Width 176 -Height 36
$pauseContinueRect = Convert-ReferenceRect -X 54 -Y 466 -Width 142 -Height 52
$pauseRestartRect = Convert-ReferenceRect -X 206 -Y 466 -Width 142 -Height 52
$pauseActionBandRect = Convert-ReferenceRect -X 36 -Y 454 -Width 330 -Height 70
$hudDarkPixelThreshold = [Math]::Max(1, [Math]::Floor(80 * $referenceScale * $referenceScale))
$hudLightPixelThreshold = [Math]::Max(1, [Math]::Floor(5000 * $referenceScale * $referenceScale))
$formerActionPixelThreshold = [Math]::Max(12, [Math]::Ceiling(12 * $referenceScale * $referenceScale))
$formerActionSpanThreshold = [Math]::Max(24,
  [Math]::Ceiling(($formerActionRegion.xMax - $formerActionRegion.xMin) * 0.20))
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

function Get-YamlScalar {
  param([string]$Text, [string]$Name)
  $match = [regex]::Match($Text, "(?m)^\s*$([regex]::Escape($Name)):\s*(?<value>[^\r\n]+)\s*$")
  if (-not $match.Success) { throw "Unity YAML scalar '$Name' was not found." }
  return $match.Groups['value'].Value.Trim().Trim("'", '"')
}

function Get-ReleaseRuntimeUiIdentity {
  $themePath = Join-Path $projectRoot 'Assets/UI/Theme/ReleaseRuntimeUiTheme.asset'
  $artSetDirectory = Join-Path $projectRoot 'Assets/UI/Art/Sets'
  if (-not (Test-Path -LiteralPath $themePath -PathType Leaf)) {
    throw "Release runtime UI identity source is missing: $themePath"
  }
  if (-not (Test-Path -LiteralPath $artSetDirectory -PathType Container)) {
    throw "Release runtime UI ArtSet directory is missing: $artSetDirectory"
  }

  $themeText = Get-Content -LiteralPath $themePath -Raw -Encoding UTF8
  $activeArtSetMatch = [regex]::Match(
    $themeText,
    '(?m)^\s*activeArtSet:\s*\{[^}]*guid:\s*(?<guid>[0-9a-f]{32})[^}]*\}\s*$')
  if (-not $activeArtSetMatch.Success) {
    throw 'Release runtime UI Theme active ArtSet GUID binding could not be read.'
  }
  $activeArtSetGuid = $activeArtSetMatch.Groups['guid'].Value
  $artSetMetaMatches = @(
    Get-ChildItem -LiteralPath $artSetDirectory -Filter '*.asset.meta' -File |
      Where-Object {
        $metaText = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
        $guidMatch = [regex]::Match($metaText, '(?m)^guid:\s*(?<guid>[0-9a-f]{32})\s*$')
        $guidMatch.Success -and $guidMatch.Groups['guid'].Value -ceq $activeArtSetGuid
      }
  )
  if ($artSetMetaMatches.Count -ne 1) {
    throw (
      "Release runtime UI Theme active ArtSet GUID '$activeArtSetGuid' resolved to " +
      "$($artSetMetaMatches.Count) production ArtSet metadata files; expected exactly one.")
  }

  $artSetMetaPath = $artSetMetaMatches[0].FullName
  $artSetPath = $artSetMetaPath.Substring(0, $artSetMetaPath.Length - '.meta'.Length)
  if (-not (Test-Path -LiteralPath $artSetPath -PathType Leaf)) {
    throw "Release runtime UI ArtSet asset is missing for metadata: $artSetMetaPath"
  }

  $themeId = Get-YamlScalar -Text $themeText -Name 'themeId'
  $themeRevision = Get-YamlScalar -Text $themeText -Name 'revision'
  if ($themeId -cne 'ui.sunny-orchard' -or $themeRevision -cne '1') {
    throw "Unexpected release runtime UI Theme identity: $themeId@$themeRevision"
  }

  $artSetText = Get-Content -LiteralPath $artSetPath -Raw -Encoding UTF8
  $artSetTypeMatch = [regex]::Match(
    $artSetText,
    '(?m)^\s*m_EditorClassIdentifier:\s*Assembly-CSharp::FruitDefense\.UI\.RuntimeUiArtSet\s*$')
  if (-not $artSetTypeMatch.Success) {
    throw "Release runtime UI Theme GUID does not resolve to a RuntimeUiArtSet asset: $artSetPath"
  }
  $artSetId = Get-YamlScalar -Text $artSetText -Name 'setId'
  $artSetRevision = Get-YamlScalar -Text $artSetText -Name 'revision'
  if ([string]::IsNullOrWhiteSpace($artSetId) -or [string]::IsNullOrWhiteSpace($artSetRevision)) {
    throw "Release RuntimeUiArtSet identity is incomplete: $artSetPath"
  }

  return [ordered]@{
    themeId = $themeId
    themeRevision = $themeRevision
    artSetId = $artSetId
    artSetRevision = $artSetRevision
    display = "$themeId@$themeRevision / $artSetId@$artSetRevision"
    themeAsset = $themePath
    artSetAsset = $artSetPath
    activeArtSetGuid = $activeArtSetGuid
  }
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
    [double]$lumaSum = 0
    $formerActionColorPixels = 0
    $formerActionColorXMin = [int]::MaxValue
    $formerActionColorXMax = [int]::MinValue
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
        $lumaSum += $luma
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
        if ($looksLikeOldOrange -or $looksLikeOldRed) {
          $formerActionColorPixels++
          $formerActionColorXMin = [Math]::Min($formerActionColorXMin, $x)
          $formerActionColorXMax = [Math]::Max($formerActionColorXMax, $x)
        }
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
      averageLuma = if ($sampleCount -gt 0) { $lumaSum / [double]$sampleCount } else { 0.0 }
      formerActionColorPixels = $formerActionColorPixels
      formerActionColorSpanPixels = if ($formerActionColorPixels -gt 0) {
        $formerActionColorXMax - $formerActionColorXMin + 1
      } else { 0 }
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

function Get-ImageDifferenceMetrics {
  param(
    [string]$ReferencePath,
    [string]$CandidatePath,
    [object]$Region,
    [int]$ChannelThreshold = 8
  )
  Add-Type -AssemblyName System.Drawing
  $reference = [Drawing.Bitmap]::FromFile($ReferencePath)
  $candidate = [Drawing.Bitmap]::FromFile($CandidatePath)
  try {
    if ($reference.Width -ne $candidate.Width -or
        $reference.Height -ne $candidate.Height) {
      throw "Image difference dimensions do not match: $ReferencePath / $CandidatePath"
    }
    $xMin = [Math]::Max(0, [int]$Region.xMin)
    $yMin = [Math]::Max(0, [int]$Region.yMin)
    $xMax = [Math]::Min($reference.Width, [int]$Region.xMax)
    $yMax = [Math]::Min($reference.Height, [int]$Region.yMax)
    if ($xMax -le $xMin -or $yMax -le $yMin) {
      throw "Image difference region is empty: $($Region | ConvertTo-Json -Compress)"
    }
    $edgeBand = [Math]::Max(1, [Math]::Ceiling(3 * $referenceScale))
    $changedPixels = 0
    $changedEdgePixels = 0
    $edgePixels = 0
    $changedXMin = [int]::MaxValue
    $changedYMin = [int]::MaxValue
    $changedXMax = [int]::MinValue
    $changedYMax = [int]::MinValue
    for ($y = $yMin; $y -lt $yMax; $y++) {
      for ($x = $xMin; $x -lt $xMax; $x++) {
        $isEdge = $x -lt $xMin + $edgeBand -or $x -ge $xMax - $edgeBand -or
          $y -lt $yMin + $edgeBand -or $y -ge $yMax - $edgeBand
        if ($isEdge) { $edgePixels++ }
        $left = $reference.GetPixel($x, $y)
        $right = $candidate.GetPixel($x, $y)
        $delta = [Math]::Max([Math]::Abs([int]$left.R - [int]$right.R),
          [Math]::Max([Math]::Abs([int]$left.G - [int]$right.G),
            [Math]::Abs([int]$left.B - [int]$right.B)))
        if ($delta -le $ChannelThreshold) { continue }
        $changedPixels++
        if ($isEdge) { $changedEdgePixels++ }
        $changedXMin = [Math]::Min($changedXMin, $x)
        $changedYMin = [Math]::Min($changedYMin, $y)
        $changedXMax = [Math]::Max($changedXMax, $x)
        $changedYMax = [Math]::Max($changedYMax, $y)
      }
    }
    $regionPixels = ($xMax - $xMin) * ($yMax - $yMin)
    $bounds = if ($changedPixels -eq 0) { $null } else {
      [ordered]@{
        xMin = $changedXMin; yMin = $changedYMin
        xMax = $changedXMax + 1; yMax = $changedYMax + 1
      }
    }
    return [ordered]@{
      channelThreshold = $ChannelThreshold
      region = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
      regionPixels = $regionPixels
      changedPixels = $changedPixels
      changedFraction = $changedPixels / [double]$regionPixels
      edgeBandPixels = $edgeBand
      edgePixels = $edgePixels
      changedEdgePixels = $changedEdgePixels
      changedBounds = $bounds
    }
  }
  finally {
    $reference.Dispose()
    $candidate.Dispose()
  }
}

function Get-ImageInsetEvidence {
  param(
    [string]$ReferencePath,
    [string]$CandidatePath,
    [object]$Region,
    [int]$DistanceMargin = 8
  )
  Add-Type -AssemblyName System.Drawing
  $reference = [Drawing.Bitmap]::FromFile($ReferencePath)
  $candidate = [Drawing.Bitmap]::FromFile($CandidatePath)
  try {
    if ($reference.Width -ne $candidate.Width -or
        $reference.Height -ne $candidate.Height) {
      throw "Image inset dimensions do not match: $ReferencePath / $CandidatePath"
    }
    $xMin = [Math]::Max(1, [int]$Region.xMin)
    $yMin = [Math]::Max(0, [int]$Region.yMin)
    $xMax = [Math]::Min($reference.Width - 1, [int]$Region.xMax)
    $yMax = [Math]::Min($reference.Height, [int]$Region.yMax)
    $edgeBand = [Math]::Max(2, [Math]::Ceiling(3 * $referenceScale))
    $sampleYMin = $yMin + $edgeBand
    $sampleYMax = $yMax - $edgeBand
    if ($xMax - $xMin -lt $edgeBand * 2 -or $sampleYMax -le $sampleYMin) {
      throw "Image inset region is too small: $($Region | ConvertTo-Json -Compress)"
    }
    $samples = 0
    $retreatedPixels = 0
    for ($y = $sampleYMin; $y -lt $sampleYMax; $y++) {
      $leftBackground = $candidate.GetPixel($xMin - 1, $y)
      $rightBackground = $candidate.GetPixel($xMax, $y)
      for ($offset = 0; $offset -lt $edgeBand; $offset++) {
        foreach ($probe in @(
          [ordered]@{ x = $xMin + $offset; background = $leftBackground },
          [ordered]@{ x = $xMax - 1 - $offset; background = $rightBackground })) {
          $referencePixel = $reference.GetPixel($probe.x, $y)
          $candidatePixel = $candidate.GetPixel($probe.x, $y)
          $background = $probe.background
          $referenceDistance = [Math]::Max(
            [Math]::Abs([int]$referencePixel.R - [int]$background.R),
            [Math]::Max([Math]::Abs([int]$referencePixel.G - [int]$background.G),
              [Math]::Abs([int]$referencePixel.B - [int]$background.B)))
          $candidateDistance = [Math]::Max(
            [Math]::Abs([int]$candidatePixel.R - [int]$background.R),
            [Math]::Max([Math]::Abs([int]$candidatePixel.G - [int]$background.G),
              [Math]::Abs([int]$candidatePixel.B - [int]$background.B)))
          $samples++
          if ($candidateDistance + $DistanceMargin -lt $referenceDistance) {
            $retreatedPixels++
          }
        }
      }
    }
    return [ordered]@{
      region = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
      edgeBandPixels = $edgeBand
      distanceMargin = $DistanceMargin
      samples = $samples
      retreatedPixels = $retreatedPixels
      retreatedFraction = if ($samples -eq 0) { 0.0 } else {
        $retreatedPixels / [double]$samples
      }
    }
  }
  finally {
    $reference.Dispose()
    $candidate.Dispose()
  }
}

function Get-ColorMaskEvidence {
  param(
    [object]$Bitmap,
    [object]$Region,
    [ValidateSet('title-ink', 'hint-icon', 'hint-copy', 'primary-surface', 'danger-surface')]
    [string]$Mask
  )
  $xMin = [Math]::Max(0, [int]$Region.xMin)
  $yMin = [Math]::Max(0, [int]$Region.yMin)
  $xMax = [Math]::Min($Bitmap.Width, [int]$Region.xMax)
  $yMax = [Math]::Min($Bitmap.Height, [int]$Region.yMax)
  $count = 0
  [double]$sumX = 0
  [double]$sumY = 0
  $visibleXMin = [int]::MaxValue
  $visibleYMin = [int]::MaxValue
  $visibleXMax = [int]::MinValue
  $visibleYMax = [int]::MinValue
  for ($y = $yMin; $y -lt $yMax; $y++) {
    for ($x = $xMin; $x -lt $xMax; $x++) {
      $pixel = $Bitmap.GetPixel($x, $y)
      $matches = switch ($Mask) {
        'title-ink' {
          [Math]::Abs([int]$pixel.R - 139) -le 18 -and
            [Math]::Abs([int]$pixel.G - 94) -le 18 -and
            [Math]::Abs([int]$pixel.B - 60) -le 18
          break
        }
        'hint-icon' {
          (($pixel.R - $pixel.G) -gt 30 -or ($pixel.G - $pixel.R) -gt 15) -and
            ($pixel.R -lt 240 -or $pixel.G -lt 220) -and $pixel.B -lt 150
          break
        }
        'hint-copy' {
          $pixel.R -lt 235 -and $pixel.G -lt 215 -and $pixel.B -lt 185
          break
        }
        'primary-surface' {
          $pixel.G -gt ($pixel.R + 18) -and
            $pixel.G -gt ($pixel.B + 35) -and $pixel.G -gt 65
          break
        }
        'danger-surface' {
          $pixel.R -gt ($pixel.G + 55) -and
            $pixel.R -gt ($pixel.B + 55) -and $pixel.R -gt 145
          break
        }
      }
      if (-not $matches) { continue }
      $count++
      $sumX += $x + .5
      $sumY += $y + .5
      $visibleXMin = [Math]::Min($visibleXMin, $x)
      $visibleYMin = [Math]::Min($visibleYMin, $y)
      $visibleXMax = [Math]::Max($visibleXMax, $x + 1)
      $visibleYMax = [Math]::Max($visibleYMax, $y + 1)
    }
  }
  if ($count -eq 0) {
    throw "Paused-modal optical mask '$Mask' found no final-raster pixels."
  }
  return [ordered]@{
    mask = $Mask
    sampleRegion = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
    pixels = $count
    bounds = [ordered]@{
      xMin = $visibleXMin; yMin = $visibleYMin
      xMax = $visibleXMax; yMax = $visibleYMax
      width = $visibleXMax - $visibleXMin
      height = $visibleYMax - $visibleYMin
      centerX = ($visibleXMin + $visibleXMax) * .5
      centerY = ($visibleYMin + $visibleYMax) * .5
    }
    centroid = [ordered]@{ x = $sumX / $count; y = $sumY / $count }
  }
}

function Get-PausedModalOpticalEvidence {
  param([string]$Path)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $title = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseTitleInkRegion -Mask 'title-ink'
    $hintIcon = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseHintIconRegion -Mask 'hint-icon'
    $hintCopy = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseHintCopyRegion -Mask 'hint-copy'
    $primary = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseContinueRect -Mask 'primary-surface'
    $danger = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseRestartRect -Mask 'danger-surface'

    $titleOwnerCenterY = ($pauseTitleRect.yMin + $pauseTitleRect.yMax) * .5
    $titleCenterDeltaLogical = [Math]::Abs(
      $title.centroid.y - $titleOwnerCenterY) / $referenceScale
    $hintCenterDeltaLogical = [Math]::Abs(
      $hintIcon.centroid.y - $hintCopy.centroid.y) / $referenceScale
    $hintUnion = [ordered]@{
      xMin = [Math]::Min($hintIcon.bounds.xMin, $hintCopy.bounds.xMin)
      yMin = [Math]::Min($hintIcon.bounds.yMin, $hintCopy.bounds.yMin)
      xMax = [Math]::Max($hintIcon.bounds.xMax, $hintCopy.bounds.xMax)
      yMax = [Math]::Max($hintIcon.bounds.yMax, $hintCopy.bounds.yMax)
    }
    $hintOwnerCenterX = ($pauseHintRect.xMin + $pauseHintRect.xMax) * .5
    $hintOwnerCenterY = ($pauseHintRect.yMin + $pauseHintRect.yMax) * .5
    $hintGroupCenterDeltaLogical = [ordered]@{
      x = [Math]::Abs((($hintUnion.xMin + $hintUnion.xMax) * .5) -
        $hintOwnerCenterX) / $referenceScale
      y = [Math]::Abs((($hintUnion.yMin + $hintUnion.yMax) * .5) -
        $hintOwnerCenterY) / $referenceScale
    }
    $primaryLocal = [ordered]@{
      left = $primary.bounds.xMin - $pauseContinueRect.xMin
      top = $primary.bounds.yMin - $pauseContinueRect.yMin
      right = $pauseContinueRect.xMax - $primary.bounds.xMax
      bottom = $pauseContinueRect.yMax - $primary.bounds.yMax
    }
    $dangerLocal = [ordered]@{
      left = $danger.bounds.xMin - $pauseRestartRect.xMin
      top = $danger.bounds.yMin - $pauseRestartRect.yMin
      right = $pauseRestartRect.xMax - $danger.bounds.xMax
      bottom = $pauseRestartRect.yMax - $danger.bounds.yMax
    }
    $pairedMaximumEdgeDelta = @(
      [Math]::Abs($primaryLocal.left - $dangerLocal.left),
      [Math]::Abs($primaryLocal.top - $dangerLocal.top),
      [Math]::Abs($primaryLocal.right - $dangerLocal.right),
      [Math]::Abs($primaryLocal.bottom - $dangerLocal.bottom)
    ) | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum

    if ($titleCenterDeltaLogical -gt 2.0) {
      throw "Paused title final-raster center delta exceeds 2 logical points: $titleCenterDeltaLogical"
    }
    if ($hintCenterDeltaLogical -gt 2.0 -or
        $hintGroupCenterDeltaLogical.x -gt 2.0 -or
        $hintGroupCenterDeltaLogical.y -gt 2.0) {
      throw (
        'Paused hint final-raster optical alignment exceeds 2 logical points: ' +
        "iconCopyY=$hintCenterDeltaLogical group=" +
        ($hintGroupCenterDeltaLogical | ConvertTo-Json -Compress))
    }
    if ($pairedMaximumEdgeDelta -gt 1 -or
        [Math]::Abs($primary.bounds.width - $danger.bounds.width) -gt 1 -or
        [Math]::Abs($primary.bounds.height - $danger.bounds.height) -gt 1) {
      throw (
        'Paused paired action final-raster envelopes differ by more than one capture pixel: ' +
        "edge=$pairedMaximumEdgeDelta primary=" +
        ($primary.bounds | ConvertTo-Json -Compress) + ' danger=' +
        ($danger.bounds | ConvertTo-Json -Compress))
    }

    return [ordered]@{
      thresholds = [ordered]@{
        titleCenterLogical = 2.0
        hintCenterLogical = 2.0
        pairedActionCapturePixels = 1
      }
      title = $title
      titleOwner = $pauseTitleRect
      titleCenterDeltaLogical = $titleCenterDeltaLogical
      hintIcon = $hintIcon
      hintCopy = $hintCopy
      hintOwner = $pauseHintRect
      hintUnion = $hintUnion
      hintIconCopyCenterDeltaLogical = $hintCenterDeltaLogical
      hintGroupCenterDeltaLogical = $hintGroupCenterDeltaLogical
      primarySurface = $primary
      dangerSurface = $danger
      primaryLocalInsets = $primaryLocal
      dangerLocalInsets = $dangerLocal
      pairedMaximumEdgeDeltaCapturePixels = $pairedMaximumEdgeDelta
    }
  }
  finally { $bitmap.Dispose() }
}

function Get-ImagePixelSample {
  param([string]$Path, [int]$X, [int]$Y)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $pixel = $bitmap.GetPixel(
      [Math]::Min([Math]::Max(0, $X), $bitmap.Width - 1),
      [Math]::Min([Math]::Max(0, $Y), $bitmap.Height - 1))
    return [ordered]@{ r = [int]$pixel.R; g = [int]$pixel.G; b = [int]$pixel.B; a = [int]$pixel.A }
  }
  finally { $bitmap.Dispose() }
}

function Get-ShellSurfaceSamples {
  param([string]$Path)
  $safeBaseY = [Math]::Floor($Height - $SafeBottom - 72 * $referenceScale)
  $samples = [ordered]@{
    safeBase = Get-ImagePixelSample -Path $Path -X ([Math]::Floor($Width / 2)) -Y $safeBaseY
  }
  if ($SafeTop -gt 0) {
    $samples.edge = Get-ImagePixelSample -Path $Path `
      -X ([Math]::Floor($Width / 2)) -Y ([Math]::Floor($SafeTop / 2))
  }
  elseif ($SafeBottom -gt 0) {
    $samples.edge = Get-ImagePixelSample -Path $Path `
      -X ([Math]::Floor($Width / 2)) -Y ($Height - [Math]::Ceiling($SafeBottom / 2))
  }
  return $samples
}

function Test-ShellSurfaceSamples {
  param([object]$Samples)
  foreach ($name in @('safeBase', 'edge')) {
    if (-not $Samples.Contains($name)) { continue }
    $sample = $Samples[$name]
    if ([int]$sample.a -lt 250) { return $false }
    $luma = Get-SrgbRelativeLuminance -R ([int]$sample.r) -G ([int]$sample.g) -B ([int]$sample.b)
    if ($luma -lt 0.08) { return $false }
  }
  return $true
}

function Get-SrgbRelativeLuminance {
  param([int]$R, [int]$G, [int]$B)
  $linear = foreach ($channel in @($R, $G, $B)) {
    $value = $channel / 255.0
    if ($value -le 0.04045) { $value / 12.92 }
    else { [Math]::Pow(($value + 0.055) / 1.055, 2.4) }
  }
  return 0.2126 * $linear[0] + 0.7152 * $linear[1] + 0.0722 * $linear[2]
}

function Get-LobbyActionContrast {
  param([string]$Path)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    # Inspect the central label area only; this excludes borders, the leading
    # Start icon, the trailing state indicator, and the shallow bottom highlight.
    $width = $lobbyStartRect.xMax - $lobbyStartRect.xMin
    $height = $lobbyStartRect.yMax - $lobbyStartRect.yMin
    $xMin = [Math]::Max(0, [Math]::Floor($lobbyStartRect.xMin + $width * 0.24))
    $xMax = [Math]::Min($bitmap.Width, [Math]::Ceiling($lobbyStartRect.xMin + $width * 0.76))
    $yMin = [Math]::Max(0, [Math]::Floor($lobbyStartRect.yMin + $height * 0.18))
    $yMax = [Math]::Min($bitmap.Height, [Math]::Ceiling($lobbyStartRect.yMin + $height * 0.72))
    $clusters = @{}
    for ($y = $yMin; $y -lt $yMax; $y++) {
      for ($x = $xMin; $x -lt $xMax; $x++) {
        $pixel = $bitmap.GetPixel($x, $y)
        if ($pixel.A -lt 250) { continue }
        $key = "$([Math]::Floor($pixel.R / 16.0)),$([Math]::Floor($pixel.G / 16.0)),$([Math]::Floor($pixel.B / 16.0))"
        if (-not $clusters.ContainsKey($key)) {
          $clusters[$key] = [ordered]@{ count = 0; r = 0L; g = 0L; b = 0L }
        }
        $cluster = $clusters[$key]
        $cluster.count++
        $cluster.r += $pixel.R
        $cluster.g += $pixel.G
        $cluster.b += $pixel.B
      }
    }
    if ($clusters.Count -lt 2) { throw "Action contrast sample has insufficient colors: $Path" }

    $colors = foreach ($entry in $clusters.GetEnumerator()) {
      $r = [int][Math]::Round($entry.Value.r / [double]$entry.Value.count)
      $g = [int][Math]::Round($entry.Value.g / [double]$entry.Value.count)
      $b = [int][Math]::Round($entry.Value.b / [double]$entry.Value.count)
      [pscustomobject]@{
        r = $r; g = $g; b = $b; count = [int]$entry.Value.count
        luminance = Get-SrgbRelativeLuminance -R $r -G $g -B $b
      }
    }
    $background = $colors | Sort-Object count -Descending | Select-Object -First 1
    $minimumSolidGlyphPixels = [Math]::Max(6,
      [Math]::Floor(($xMax - $xMin) * ($yMax - $yMin) * 0.0008))
    $foreground = $colors |
      Where-Object { $_.count -ge $minimumSolidGlyphPixels -and
        $_.luminance -gt $background.luminance -and
        ([Math]::Max($_.r, [Math]::Max($_.g, $_.b)) -
          [Math]::Min($_.r, [Math]::Min($_.g, $_.b))) -le 48 } |
      Sort-Object `
        @{ Expression = 'luminance'; Descending = $true },
        @{ Expression = 'count'; Descending = $true } |
      Select-Object -First 1
    if ($null -eq $foreground) {
      throw "Action contrast sample has no solid light glyph color: $Path"
    }
    $ratio = ([Math]::Max($background.luminance, $foreground.luminance) + 0.05) /
      ([Math]::Min($background.luminance, $foreground.luminance) + 0.05)
    return [ordered]@{
      background = [ordered]@{
        r = $background.r; g = $background.g; b = $background.b; pixels = $background.count
      }
      foreground = [ordered]@{
        r = $foreground.r; g = $foreground.g; b = $foreground.b; pixels = $foreground.count
      }
      ratio = $ratio
      minimum = 3.0
      passed = $ratio -ge 3.0
      sampleRect = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
    }
  }
  finally { $bitmap.Dispose() }
}

function Save-StableShellScreenshot {
  param([string]$Name)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $attempts = 0
  $consecutiveStableFrames = 0
  $previousStableLuma = $null
  do {
    $attempts++
    $path = Save-Screenshot -Name $Name
    $metrics = Get-ImageMetrics -Path $path
    $surfaceSamples = Get-ShellSurfaceSamples -Path $path
    $actionContrast = Get-LobbyActionContrast -Path $path
    $metrics['surfaceSamples'] = $surfaceSamples
    $metrics['actionContrast'] = $actionContrast
    $passesFrameGuards = $metrics.width -eq $Width -and $metrics.height -eq $Height -and
        (Test-StableFrameMetrics -Metrics $metrics) -and
        (Test-ShellSurfaceSamples -Samples $surfaceSamples) -and
        $actionContrast.passed
    if ($passesFrameGuards) {
      if ($null -ne $previousStableLuma -and
          [Math]::Abs([double]$metrics.averageLuma - [double]$previousStableLuma) -le 0.006) {
        $consecutiveStableFrames++
      }
      else {
        $consecutiveStableFrames = 1
      }
      $previousStableLuma = [double]$metrics.averageLuma
      if ($consecutiveStableFrames -ge 3) {
        $metrics['consecutiveStableFrames'] = $consecutiveStableFrames
        return [pscustomobject]@{ Path = $path; Metrics = $metrics; Attempts = $attempts }
      }
    }
    else {
      $consecutiveStableFrames = 0
      $previousStableLuma = $null
    }
    Start-Sleep -Milliseconds 250
  } while ((Get-Date) -lt $deadline)
  throw (
    "Sunny Orchard shell surface/contrast did not stabilize for '$Name': " +
    "$($metrics | ConvertTo-Json -Depth 6 -Compress)")
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

$runtimeUiIdentity = Get-ReleaseRuntimeUiIdentity

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
    runtimeUi = $runtimeUiIdentity
    mappedDesignBounds = $mappedDesignBounds
    referenceControls = $referenceControls
    mappedControls = $controls
    sampledRegions = [ordered]@{ header = $headerSampleRegion; formerAction = $formerActionRegion }
    thresholds = [ordered]@{
      hudDarkPixels = $hudDarkPixelThreshold
      hudLightPixels = $hudLightPixelThreshold
      formerActionColorPixels = $formerActionPixelThreshold
      formerActionColorSpanPixels = $formerActionSpanThreshold
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
  $initialChromeUrl = if ($ShellVisual -or $ShellError) { 'about:blank' } else { $Url }
  $chromeArgs = @(
    '--headless=new', '--no-first-run', '--disable-background-networking', '--disable-extensions',
    '--hide-scrollbars', '--use-angle=swiftshader', '--enable-webgl', '--ignore-gpu-blocklist',
    '--user-agent="Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1"',
    "--window-size=$Width,$Height", '--force-device-scale-factor=1',
    "--remote-debugging-port=$debugPort", "--user-data-dir=$profileDir", $initialChromeUrl
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
            [string]$typeProperty.Value -eq 'page' -and [string]$urlProperty.Value -eq $initialChromeUrl) {
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
  $bootstrapCapture = [ordered]@{
    state = if ($ShellVisual) { 'not-captured' } else { 'not-requested' }
    reason = if ($ShellVisual) { 'Unity route became ready before a stable initializing frame was observed.' } else { '' }
    cpuThrottlingRate = if ($ShellVisual) { $BootstrapCpuThrottlingRate } else { 1 }
    attempts = 0
  }
  if ($ShellVisual) {
    $bootstrapProbePath = Join-Path $outputDir '00-bootstrap-probe.png'
    $bootstrapFinalPath = Join-Path $outputDir '00-bootstrap-initializing.png'
    foreach ($staleBootstrapPath in @($bootstrapProbePath, $bootstrapFinalPath)) {
      if (Test-Path -LiteralPath $staleBootstrapPath -PathType Leaf) {
        Remove-Item -LiteralPath $staleBootstrapPath -Force
      }
    }
    Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{
      rate = $BootstrapCpuThrottlingRate
    } | Out-Null
    $coldStartedAt = [DateTimeOffset]::UtcNow
    Invoke-Cdp -Method 'Page.navigate' -Params @{ url = $Url } | Out-Null

    $bootstrapDeadline = (Get-Date).AddSeconds([Math]::Min($TimeoutSeconds, 25))
    do {
      try {
        $bootstrapReadiness = (Invoke-JavaScript -Expression $readinessExpression) | ConvertFrom-Json
        $bootstrapViewportReady = $bootstrapReadiness.innerWidth -eq $Width -and
          $bootstrapReadiness.innerHeight -eq $Height
        $bootstrapCanvasReady = $bootstrapReadiness.width -eq $Width -and
          $bootstrapReadiness.height -eq $Height -and
          [Math]::Abs([double]$bootstrapReadiness.cssWidth - $Width) -lt 0.51 -and
          [Math]::Abs([double]$bootstrapReadiness.cssHeight - $Height) -lt 0.51
        if ($bootstrapReadiness.canvas -and $bootstrapReadiness.loading -eq 'none' -and
            -not $bootstrapReadiness.acceptanceReady -and $bootstrapViewportReady -and
            $bootstrapCanvasReady) {
          $bootstrapCapture.attempts++
          $bootstrapPath = Save-Screenshot -Name '00-bootstrap-probe'
          $bootstrapMetrics = Get-ImageMetrics -Path $bootstrapPath
          # Reject Unity's dark splash frame; the Bootstrap presentation uses the
          # release Theme's light cream surfaces and is the application-owned evidence.
          if ((Test-StableFrameMetrics -Metrics $bootstrapMetrics) -and
              $bootstrapMetrics.averageLuma -gt 0.45) {
            $bootstrapCapture.state = 'captured'
            $bootstrapCapture.reason = ''
            Move-Item -LiteralPath $bootstrapPath -Destination $bootstrapFinalPath -Force
            $bootstrapCapture.screenshot = $bootstrapFinalPath
            $bootstrapCapture.imageMetrics = $bootstrapMetrics
            $bootstrapCapture.canvas = $bootstrapReadiness
            break
          }
        }
        if ($bootstrapReadiness.acceptanceReady) { break }
      }
      catch {
        # Navigation can temporarily destroy the Runtime execution context.
      }
      Start-Sleep -Milliseconds 35
    } while ((Get-Date) -lt $bootstrapDeadline)
    Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{ rate = 1 } | Out-Null
    if ($bootstrapCapture.state -ne 'captured' -and
        (Test-Path -LiteralPath $bootstrapProbePath -PathType Leaf)) {
      Remove-Item -LiteralPath $bootstrapProbePath -Force
    }
  }
  elseif ($ShellError) {
    $coldStartedAt = [DateTimeOffset]::UtcNow
    Invoke-Cdp -Method 'Page.navigate' -Params @{ url = $Url } | Out-Null
  }

  if ($ShellError) {
    $errorReadiness = $null
    $errorDeadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
      try {
        $candidate = (Invoke-JavaScript -Expression $readinessExpression) | ConvertFrom-Json
        $errorViewportReady = $candidate.innerWidth -eq $Width -and
          $candidate.innerHeight -eq $Height
        $errorCanvasReady = $candidate.width -eq $Width -and
          $candidate.height -eq $Height -and
          [Math]::Abs([double]$candidate.cssWidth - $Width) -lt 0.51 -and
          [Math]::Abs([double]$candidate.cssHeight - $Height) -lt 0.51
        if ($candidate.canvas -and $candidate.loading -eq 'none' -and
            -not $candidate.acceptanceReady -and [int]$candidate.appRoute -eq -1 -and
            $errorViewportReady -and $errorCanvasReady) {
          $errorReadiness = $candidate
          break
        }
      }
      catch {
        # Navigation can temporarily destroy the Runtime execution context.
      }
      Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $errorDeadline)
    if ($null -eq $errorReadiness) {
      throw 'Formal invalid-level launch did not reach a no-route application canvas.'
    }

    # The invalid level is resolved after the normal Bootstrap services complete.
    # Wait beyond the short initializing modal so the persistent blocking error,
    # rather than a startup or Unity splash frame, is the captured state.
    Start-Sleep -Milliseconds 1800
    $errorCapture = $null
    $lastDarkErrorMetrics = $null
    $errorFrameDeadline = (Get-Date).AddSeconds([Math]::Min($TimeoutSeconds, 20))
    do {
      $errorReadiness = (Invoke-JavaScript -Expression $readinessExpression) | ConvertFrom-Json
      if ($errorReadiness.acceptanceReady -or [int]$errorReadiness.appRoute -ne -1) {
        throw (
          'Formal invalid-level launch unexpectedly published a route: ' +
          ($errorReadiness | ConvertTo-Json -Compress))
      }
      $candidateErrorCapture = Save-StableScreenshot `
        -Name '00-bootstrap-blocking-error' `
        -RequireHud $false
      if ($candidateErrorCapture.Metrics.averageLuma -gt 0.25) {
        $errorCapture = $candidateErrorCapture
        break
      }
      $lastDarkErrorMetrics = $candidateErrorCapture.Metrics
      Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $errorFrameDeadline)
    if ($null -eq $errorCapture) {
      throw (
        'Formal invalid-level capture remained a dark Unity/black frame: ' +
        ($lastDarkErrorMetrics | ConvertTo-Json -Compress))
    }
    $coldCacheRun = Get-UnityResourceTiming `
      -Label 'cold-error' `
      -DeliveryAssets $delivery.assets `
      -StartedAt $coldStartedAt
    $errorManifest = [ordered]@{
      schemaVersion = 1
      evidenceType = 'bootstrap-blocking-error-webgl-visual'
      accepted = $true
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      input = [ordered]@{
        acceptance = '1'
        route = 'battle'
        levelId = $ErrorLevelId
        classification = 'formal-invalid-level-acceptance-input'
      }
      expectedUserCopy = -join ([char[]]@(
        0x542f, 0x52a8, 0x5931, 0x8d25, 0xff1a, 0x6240,
        0x9009, 0x5173, 0x5361, 0x4e0d, 0x53ef, 0x7528))
      viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
      safeArea = $safeAreaEvidence
      runtimeUi = $runtimeUiIdentity
      browser = $browserEvidence
      canvas = $errorReadiness
      checks = [ordered]@{
        releaseRuntimeUiIdentity = 'pass'
        unityPlayerLoaded = 'pass'
        noRouteReadyPublished = 'pass'
        applicationFrameNotSplashOrBlack = 'pass'
        requestedViewportAndCanvas = 'pass'
        safeAreaQueryApplied = 'pass'
        finiteUserCopyAndNonColorCue = 'manual-screenshot-review-required'
      }
      screenshot = $errorCapture.Path
      imageMetrics = $errorCapture.Metrics
      delivery = [ordered]@{
        metadata = $delivery
        coldRun = $coldCacheRun
      }
    }
    $errorManifestPath = Join-Path $outputDir 'shell-error-evidence.json'
    $errorManifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $errorManifestPath -Encoding UTF8
    Write-Host "FRUIT_DEFENSE_SHELL_ERROR_OK manifest=$errorManifestPath"
    return
  }
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

  if ($ShellVisual) {
    Wait-AppRoute -Route 0
    Move-CanvasPointerOut

    $defaultIdentity = Get-AcceptanceIdentity
    if ($null -eq $defaultIdentity -or [int]$defaultIdentity.route -ne 0 -or
        -not [string]::IsNullOrEmpty([string]$defaultIdentity.sessionId) -or
        [int]$defaultIdentity.seed -ne 0) {
      throw "Default Lobby identity is invalid: $($defaultIdentity | ConvertTo-Json -Compress)"
    }
    if ([string]$defaultIdentity.levelId -ceq $LevelId) {
      throw (
        "Alternate-selection level '$LevelId' is already selected in the supplied browser profile. " +
        'Use a fresh profile or choose another level.')
    }

    $shellScreenshots = [ordered]@{}
    $shellMetrics = [ordered]@{}
    $shellIdentities = [ordered]@{ defaultLobby = $defaultIdentity }
    # Route-ready can precede the first fully composited themed frame. Require
    # the approved base/edge colors so a transient grey overlay is never accepted.
    $defaultCapture = Save-StableShellScreenshot -Name '01-lobby-default'
    $shellScreenshots.defaultLobby = $defaultCapture.Path
    $shellMetrics.defaultLobby = $defaultCapture.Metrics

    if ($InteractionPolishEvidence) {
      Invoke-CanvasClickImmediate `
        -X $controls[$levelCardControlName].x -Y $controls[$levelCardControlName].y
      Start-Sleep -Milliseconds 45
      $selectionMotionPath = Save-Screenshot -Name '02a-lobby-selection-motion'
      $selectionMotionMetrics = Get-ImageMetrics -Path $selectionMotionPath
      $selectionMotionHash = (Get-FileHash -LiteralPath $selectionMotionPath -Algorithm SHA256).Hash
      $shellScreenshots.selectionMotion = $selectionMotionPath
      $shellMetrics.selectionMotion = $selectionMotionMetrics
    }
    else {
      Invoke-CanvasClick -X $controls[$levelCardControlName].x -Y $controls[$levelCardControlName].y
    }
    $shellIdentities.alternateLobby = Wait-AcceptanceIdentity `
      -Route 0 -Stage 'alternate-selected-lobby' -SessionMode Cleared
    Move-CanvasPointerOut
    $alternateCapture = Save-StableShellScreenshot -Name '02-lobby-alternate-selection'
    $shellScreenshots.alternateSelection = $alternateCapture.Path
    $shellMetrics.alternateSelection = $alternateCapture.Metrics
    $alternateHash = (Get-FileHash -LiteralPath $alternateCapture.Path -Algorithm SHA256).Hash
    $selectionMotionDifference = $null
    $selectionMotionInset = $null
    if ($InteractionPolishEvidence) {
      $selectionMotionDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $alternateCapture.Path -CandidatePath $selectionMotionPath `
        -Region ([ordered]@{ xMin = 0; yMin = 0; xMax = $Width; yMax = $Height })
      $selectionMotionInset = Get-ImageInsetEvidence `
        -ReferencePath $alternateCapture.Path -CandidatePath $selectionMotionPath `
        -Region $lobbyAlternateCardRect
      if ($selectionMotionHash -ceq $alternateHash -or
          $selectionMotionDifference.changedPixels -lt 1000 -or
          $selectionMotionInset.retreatedPixels -lt 20) {
        throw (
          'Lobby selection motion is not a material inset-only impulse: ' +
          ($selectionMotionInset | ConvertTo-Json -Compress))
      }
    }

    $transitionCapture = $null
    $transitionAttempts = 0
    $transitionDeadline = (Get-Date).AddSeconds([Math]::Min($TimeoutSeconds, 10))
    if ($InteractionPolishEvidence) {
      Start-CanvasPress -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
      Start-Sleep -Milliseconds 45
      $startPressPath = Save-Screenshot -Name '03-lobby-start-pressed'
      $startPressMetrics = Get-ImageMetrics -Path $startPressPath
      $startPressHash = (Get-FileHash -LiteralPath $startPressPath -Algorithm SHA256).Hash
      $startPressDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $alternateCapture.Path -CandidatePath $startPressPath `
        -Region $lobbyStartRect
      $startPressInset = Get-ImageInsetEvidence `
        -ReferencePath $alternateCapture.Path -CandidatePath $startPressPath `
        -Region $lobbyStartRect
      if ($startPressHash -ceq $alternateHash -or
          $startPressDifference.changedPixels -lt 200 -or
          $startPressDifference.changedEdgePixels -lt 20 -or
          $startPressInset.retreatedPixels -lt 20) {
        Stop-CanvasPress -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
        throw (
          'Lobby Start press checkpoint lacks a material action-region/edge difference: ' +
          ($startPressDifference | ConvertTo-Json -Compress))
      }
      $transitionCapture = [pscustomobject]@{
        Path = $startPressPath
        Metrics = $startPressMetrics
        Sha256 = $startPressHash
      }
      $transitionAttempts = 1
      Stop-CanvasPress -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
    }
    else {
      try {
      Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{
        rate = $BootstrapCpuThrottlingRate
      } | Out-Null
      Invoke-CanvasClickImmediate -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
      do {
        $transitionAttempts++
        $currentRoute = Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1'
        if ([int]$currentRoute -ne 0) { break }
        $transitionPath = Save-Screenshot -Name '03-lobby-transition'
        $routeAfterScreenshot = Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1'
        if ([int]$routeAfterScreenshot -ne 0) {
          Remove-Item -LiteralPath $transitionPath -Force -ErrorAction SilentlyContinue
          break
        }
        $transitionMetrics = Get-ImageMetrics -Path $transitionPath
        $transitionHash = (Get-FileHash -LiteralPath $transitionPath -Algorithm SHA256).Hash
        if ((Test-StableFrameMetrics -Metrics $transitionMetrics) -and
            $transitionHash -cne $alternateHash) {
          $transitionCapture = [pscustomobject]@{
            Path = $transitionPath
            Metrics = $transitionMetrics
            Sha256 = $transitionHash
          }
          break
        }
        Start-Sleep -Milliseconds 35
      } while ((Get-Date) -lt $transitionDeadline)
      }
      finally {
        Move-CanvasPointerOut
        Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{ rate = 1 } | Out-Null
      }
    }
    if ($null -eq $transitionCapture) {
      throw "Lobby transition frame was not captured before route change after $transitionAttempts attempts."
    }
    $transitionSurfaceSamples = Get-ShellSurfaceSamples -Path $transitionCapture.Path
    if (-not (Test-ShellSurfaceSamples -Samples $transitionSurfaceSamples)) {
      throw (
        'Lobby transition frame did not retain an opaque stable shell surface: ' +
        ($transitionSurfaceSamples | ConvertTo-Json -Compress))
    }
    $transitionActionContrast = Get-LobbyActionContrast -Path $transitionCapture.Path
    if (-not $transitionActionContrast.passed) {
      throw (
        'Lobby transition action contrast is below 3.0: ' +
        ($transitionActionContrast | ConvertTo-Json -Compress))
    }
    $transitionCapture.Metrics['surfaceSamples'] = $transitionSurfaceSamples
    $transitionCapture.Metrics['actionContrast'] = $transitionActionContrast
    $transitionEvidenceKey = if ($InteractionPolishEvidence) { 'startPress' } else { 'transition' }
    $shellScreenshots[$transitionEvidenceKey] = $transitionCapture.Path
    $shellMetrics[$transitionEvidenceKey] = $transitionCapture.Metrics

    Wait-AppRoute -Route 1
    $shellIdentities.battleAfterTransition = Wait-AcceptanceIdentity `
      -Route 1 -Stage 'battle-after-lobby-transition' -SessionMode Required

    $errorEvidence = [ordered]@{
      state = 'separate-formal-capture-required'
      reason = (
        'Capture the application-owned Bootstrap blocking error separately with -ShellError. ' +
        'That mode uses the production acceptance battle route with an invalid level id and does ' +
        'not add a runtime hook, loader failure, or scene mutation.')
      screenshot = $null
    }
    $allCapturedMetrics = @($shellMetrics.Values)
    foreach ($metrics in $allCapturedMetrics) {
      if ($metrics.width -ne $Width -or $metrics.height -ne $Height -or
          -not (Test-StableFrameMetrics -Metrics $metrics)) {
        throw "Shell visual frame is invalid: $($metrics | ConvertTo-Json -Compress)"
      }
    }

    $shellManifest = [ordered]@{
      schemaVersion = 1
      evidenceType = 'bootstrap-lobby-webgl-visual'
      accepted = $true
      completion = 'complete-shell-visual-states'
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
      safeArea = $safeAreaEvidence
      runtimeUi = $runtimeUiIdentity
      browser = $browserEvidence
      canvas = $readiness
      bootstrapInitializing = $bootstrapCapture
      errorEvidence = $errorEvidence
      checks = [ordered]@{
        releaseRuntimeUiIdentity = 'pass'
        bootstrapInitializing = if ($bootstrapCapture.state -eq 'captured') { 'pass' } else { 'not-captured' }
        lobbyDefault = 'pass'
        lobbyAlternateSelection = 'pass'
        lobbyTransition = if ($InteractionPolishEvidence) { 'release-route-pass' } else { 'pass' }
        lobbySelectionMotion = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
        lobbyStartPress = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
        lobbyActionContrast = 'pass'
        bootstrapOrLobbyError = 'separate-formal-capture-required'
        selectedLevelInputMapping = 'pass'
        startInputMapping = 'pass'
        requestedViewportAndCanvas = 'pass'
        safeAreaQueryApplied = 'pass'
        screenshotDimensions = 'pass'
        noBlackTransparentOrLargeNearBlackRegions = 'pass'
        warmCacheReuse = 'pass'
      }
      delivery = $delivery
      routeIdentities = $shellIdentities
      screenshots = $shellScreenshots
      imageMetrics = $shellMetrics
      controls = [ordered]@{
        alternateLevel = $controls[$levelCardControlName]
        start = $controls.lobbyStart
      }
      referenceControls = [ordered]@{
        alternateLevel = $referenceControls[$levelCardControlName]
        start = $referenceControls.lobbyStart
      }
      transition = [ordered]@{
        evidenceKind = if ($InteractionPolishEvidence) { 'pressed-input-checkpoint' } else { 'route-transition-frame' }
        attempts = $transitionAttempts
        cpuThrottlingRate = $BootstrapCpuThrottlingRate
        alternateSha256 = $alternateHash
        transitionSha256 = $transitionCapture.Sha256
      }
      interactionPolishEvidence = if ($InteractionPolishEvidence) {
        [ordered]@{
          selectionMotionDifference = $selectionMotionDifference
          selectionMotionInset = $selectionMotionInset
          startPressDifference = $startPressDifference
          startPressInset = $startPressInset
          releaseNavigation = 'Lobby-to-Battle-pass'
        }
      } else {
        [ordered]@{ state = 'not-requested' }
      }
    }
    $shellManifestPath = Join-Path $outputDir 'shell-visual-evidence.json'
    $shellManifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $shellManifestPath -Encoding UTF8
    Write-Host "FRUIT_DEFENSE_SHELL_VISUAL_OK manifest=$shellManifestPath"
    return
  }

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

    Invoke-AcceptanceFlowCommand -Command $SettlementOutcome
    Wait-AppRoute -Route 2
    $flowIdentities.settlement = Wait-AcceptanceIdentity `
      -Route 2 -Stage 'settlement' -SessionMode Required
    Assert-SameSession -Expected $flowIdentities.battle `
      -Actual $flowIdentities.settlement -Stage 'settlement'
    Move-CanvasPointerOut
    if ($InteractionPolishEvidence) {
      Start-Sleep -Milliseconds 45
      $flowScreenshots.settlementMotion = Save-Screenshot `
        -Name "03a-settlement-motion-$SettlementOutcome"
    }
    $flowScreenshots.settlement = (Save-StableScreenshot `
      -Name "03-settlement-$SettlementOutcome" -RequireHud $false).Path

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
    Invoke-AcceptanceFlowCommand -Command $SettlementOutcome
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
    $settlementMotionDifference = $null
    if ($InteractionPolishEvidence) {
      $settlementMotionDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $flowScreenshots.settlement `
        -CandidatePath $flowScreenshots.settlementMotion `
        -Region ([ordered]@{ xMin = 0; yMin = 0; xMax = $Width; yMax = $Height })
      if ($settlementMotionDifference.changedPixels -lt 1000) {
        throw (
          'Settlement motion checkpoint lacks a material difference from rest: ' +
          ($settlementMotionDifference | ConvertTo-Json -Compress))
      }
    }

    $flowManifest = [ordered]@{
      accepted = $true
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      levelId = $LevelId
      settlementOutcome = $SettlementOutcome
      expectedCompositeIdentity = $expectedLevelIdentity
      viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
      safeArea = $safeAreaEvidence
      browser = $browserEvidence
      canvas = $readiness
      runtimeUi = $runtimeUiIdentity
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
        settlementMotionCheckpoint = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
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
      interactionPolishEvidence = if ($InteractionPolishEvidence) {
        [ordered]@{ settlementMotionDifference = $settlementMotionDifference }
      } else {
        [ordered]@{ state = 'not-requested' }
      }
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

  $waveActionPressDifference = $null
  $pauseContinuePressDifference = $null
  $pauseContinuePressInset = $null
  $pauseRestartPressDifference = $null
  $pauseRestartPressInset = $null
  if ($InteractionPolishEvidence) {
    Move-CanvasPointer -X $controls.waveAction.x -Y $controls.waveAction.y
    Start-Sleep -Milliseconds 45
    $screenshots.waveActionHover = Save-Screenshot -Name '01a-wave-action-hover'
    Start-CanvasPress -X $controls.waveAction.x -Y $controls.waveAction.y
    try {
      Start-Sleep -Milliseconds 45
      $screenshots.waveActionPressed = Save-Screenshot -Name '02a-wave-action-pressed'
      $waveActionPressDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $screenshots.waveActionHover `
        -CandidatePath $screenshots.waveActionPressed `
        -Region $waveActionRect
      $waveActionPressInset = Get-ImageInsetEvidence `
        -ReferencePath $screenshots.waveActionHover `
        -CandidatePath $screenshots.waveActionPressed `
        -Region $waveActionRect
      if ($waveActionPressDifference.changedPixels -lt 200 -or
          $waveActionPressDifference.changedEdgePixels -lt 20 -or
          $waveActionPressInset.retreatedPixels -lt 20) {
        throw (
          'Battle Wave press checkpoint lacks a material action-region/edge difference: ' +
          ($waveActionPressDifference | ConvertTo-Json -Compress))
      }
    }
    finally {
      Stop-CanvasPress -X $controls.waveAction.x -Y $controls.waveAction.y
    }
  }
  else {
    Invoke-CanvasClick -X $controls.waveAction.x -Y $controls.waveAction.y
  }
  $screenshots.activeWave = (Save-StableScreenshot -Name '02-active-wave').Path

  Set-AcceptanceState -State 'between-wave'
  $screenshots.betweenWave = (Save-StableScreenshot -Name '03-between-wave').Path
  Invoke-CanvasClick -X $controls.waveAction.x -Y $controls.waveAction.y
  $screenshots.immediateNextWave = (Save-StableScreenshot -Name '04-immediate-next-wave').Path

  Invoke-CanvasClick -X $controls.headerPause.x -Y $controls.headerPause.y
  # The modal intentionally dims the HUD, so this state uses frame/dimension checks without the unobscured-HUD ink threshold.
  $screenshots.paused = (Save-StableScreenshot -Name '05-paused' -RequireHud $false).Path
  $pausedModalOpticalEvidence = Get-PausedModalOpticalEvidence -Path $screenshots.paused
  if ($InteractionPolishEvidence) {
    Move-CanvasPointer -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
    Start-Sleep -Milliseconds 45
    $screenshots.pauseContinueHover = Save-Screenshot -Name '05a-pause-continue-hover'
    Start-CanvasPress -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
    try {
      Start-Sleep -Milliseconds 45
      $screenshots.pauseContinuePressed = Save-Screenshot -Name '05b-pause-continue-pressed'
      $pauseContinuePressDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $screenshots.pauseContinueHover `
        -CandidatePath $screenshots.pauseContinuePressed `
        -Region $pauseActionBandRect
      $pauseContinuePressInset = Get-ImageInsetEvidence `
        -ReferencePath $screenshots.pauseContinueHover `
        -CandidatePath $screenshots.pauseContinuePressed `
        -Region $pauseContinueRect
      $bounds = $pauseContinuePressDifference.changedBounds
      if ($pauseContinuePressDifference.changedPixels -lt 100 -or
          $pauseContinuePressInset.retreatedPixels -lt 20 -or
          $null -eq $bounds -or
          $bounds.xMin -lt $pauseContinueRect.xMin -or
          $bounds.yMin -lt $pauseContinueRect.yMin -or
          $bounds.xMax -gt $pauseContinueRect.xMax -or
          $bounds.yMax -gt $pauseContinueRect.yMax) {
        throw (
          'Pause Continue press must materially contract inside its owner rect: ' +
          ($pauseContinuePressDifference | ConvertTo-Json -Compress))
      }
    }
    finally {
      Stop-CanvasPress -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
    }
  }
  else {
    Invoke-CanvasClick -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
  }
  $screenshots.continued = (Save-StableScreenshot -Name '06-continued').Path
  Invoke-CanvasClick -X $controls.headerPause.x -Y $controls.headerPause.y
  if ($InteractionPolishEvidence) {
    Move-CanvasPointer -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
    Start-Sleep -Milliseconds 45
    $screenshots.pauseRestartHover = Save-Screenshot -Name '06a-pause-restart-hover'
    Start-CanvasPress -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
    try {
      Start-Sleep -Milliseconds 45
      $screenshots.pauseRestartPressed = Save-Screenshot -Name '06b-pause-restart-pressed'
      $pauseRestartPressDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $screenshots.pauseRestartHover `
        -CandidatePath $screenshots.pauseRestartPressed `
        -Region $pauseActionBandRect
      $pauseRestartPressInset = Get-ImageInsetEvidence `
        -ReferencePath $screenshots.pauseRestartHover `
        -CandidatePath $screenshots.pauseRestartPressed `
        -Region $pauseRestartRect
      $bounds = $pauseRestartPressDifference.changedBounds
      if ($pauseRestartPressDifference.changedPixels -lt 100 -or
          $pauseRestartPressInset.retreatedPixels -lt 20 -or
          $null -eq $bounds -or
          $bounds.xMin -lt $pauseRestartRect.xMin -or
          $bounds.yMin -lt $pauseRestartRect.yMin -or
          $bounds.xMax -gt $pauseRestartRect.xMax -or
          $bounds.yMax -gt $pauseRestartRect.yMax) {
        throw (
          'Pause Restart press must materially contract inside its owner rect: ' +
          ($pauseRestartPressDifference | ConvertTo-Json -Compress))
      }
    }
    finally {
      Stop-CanvasPress -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
    }
  }
  else {
    Invoke-CanvasClick -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
  }
  $screenshots.restarted = (Save-StableScreenshot -Name '07-restarted').Path

  Set-AcceptanceState -State 'selected-tool'
  $toolAvailableCapture = Save-StableScreenshot -Name '08-tool-available'
  $screenshots.toolAvailable = $toolAvailableCapture.Path
  Invoke-CanvasClick -X $controls.weaponGatling.x -Y $controls.weaponGatling.y
  $selectedToolCapture = Save-StableScreenshot -Name '09-selected-tool'
  $toolAvailableHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $toolAvailableCapture.Path).Hash
  $selectedToolHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $selectedToolCapture.Path).Hash
  if ($selectedToolHash -ceq $toolAvailableHash) {
    throw 'Gatling click did not change the real selectable-tool presentation.'
  }
  $screenshots.selectedTool = $selectedToolCapture.Path

  Set-AcceptanceState -State 'adjacent-pots'
  $screenshots.adjacentPots = (Save-StableScreenshot -Name '10-adjacent-pots').Path

  Set-AcceptanceState -State 'drag-target'
  Start-CanvasDrag -FromX $controls.nurserySlot0.x -FromY $controls.nurserySlot0.y -ToX $controls.acceptanceCell0.x -ToY $controls.acceptanceCell0.y
  $screenshots.legalDragCue = (Save-StableScreenshot -Name '11-legal-drag-cue').Path
  Stop-CanvasDrag -X $controls.acceptanceCell0.x -Y $controls.acceptanceCell0.y

  Set-AcceptanceState -State 'selection-inspection'
  $illegalTargetX = $controls.acceptanceCell0.x + 12.0 * $referenceScale
  Start-CanvasDrag -FromX $controls.acceptanceCell0.x -FromY $controls.acceptanceCell0.y `
    -ToX $illegalTargetX -ToY $controls.acceptanceCell0.y
  $screenshots.illegalDragCue = (Save-StableScreenshot -Name '12-illegal-drag-cue').Path
  Stop-CanvasDrag -X $illegalTargetX -Y $controls.acceptanceCell0.y

  Set-AcceptanceState -State 'dense-board'
  $screenshots.denseBoard = (Save-StableScreenshot -Name '13-dense-board').Path

  Set-AcceptanceState -State 'selection-inspection'
  # Deterministic interaction state projected through the enlarged board: attacking plant and empty pot use the first two canonical plantable cells.
  Invoke-CanvasClick -X $controls.acceptanceCell0.x -Y $controls.acceptanceCell0.y
  $screenshots.plantDetail = (Save-StableScreenshot -Name '14-plant-detail').Path
  Invoke-CanvasClick -X $controls.acceptanceCell1.x -Y $controls.acceptanceCell1.y
  $screenshots.destinationClickNoMove = (Save-StableScreenshot -Name '15-destination-click-no-move').Path
  Start-CanvasDrag -FromX $controls.acceptanceCell0.x -FromY $controls.acceptanceCell0.y -ToX $controls.acceptanceCell1.x -ToY $controls.acceptanceCell1.y
  Stop-CanvasDrag -X $controls.acceptanceCell1.x -Y $controls.acceptanceCell1.y
  $screenshots.dragRelocation = (Save-StableScreenshot -Name '16-after-drag-move').Path

  # The existing URL-guarded Battle acceptance bridge owns stable terminal-card
  # preview states. Production terminal submission and Battle-to-Settlement flow
  # remain covered separately by Flow mode.
  $terminalReferenceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $screenshots.dragRelocation).Hash
  Move-CanvasPointerOut
  Set-AcceptanceState -State "terminal-$BattleTerminalOutcome"
  $terminalRouteBefore = [int](Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1')
  $terminalCapture = Save-StableScreenshot `
    -Name "17-battle-terminal-$BattleTerminalOutcome" -RequireHud $false
  $terminalRouteAfter = [int](Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1')
  $terminalHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $terminalCapture.Path).Hash
  if ($terminalRouteBefore -ne 1 -or $terminalRouteAfter -ne 1 -or
      $terminalHash -ceq $terminalReferenceHash -or
      $terminalCapture.Metrics.averageLuma -ge $readyCapture.Metrics.averageLuma * 0.92) {
    throw (
      "Stable Battle terminal preview validation failed: outcome=$BattleTerminalOutcome " +
      "route=$terminalRouteBefore/$terminalRouteAfter " +
      "luma=$($terminalCapture.Metrics.averageLuma)/$($readyCapture.Metrics.averageLuma)")
  }
  $screenshots.terminal = $terminalCapture.Path
  Invoke-CanvasClick -X $controls.terminalRestart.x -Y $controls.terminalRestart.y
  $terminalRestartRoute = [int](Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1')
  $terminalRestartIdentity = Wait-AcceptanceIdentity `
    -Route 1 -Stage 'terminal-preview-restart' -SessionMode Required
  Assert-SameSession -Expected $directBattleIdentity `
    -Actual $terminalRestartIdentity -Stage 'terminal-preview-restart'
  $terminalRestartCapture = Save-StableScreenshot -Name '18-terminal-preview-restarted'
  if ($terminalRestartRoute -ne 1 -or
      $terminalRestartCapture.Metrics.averageLuma -le $terminalCapture.Metrics.averageLuma * 1.08) {
    throw (
      "Terminal preview restart did not restore the unobscured Battle Ready presentation: " +
      "route=$terminalRestartRoute " +
      "luma=$($terminalRestartCapture.Metrics.averageLuma)/$($terminalCapture.Metrics.averageLuma)")
  }
  $screenshots.terminalPreviewRestarted = $terminalRestartCapture.Path

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
  $readyHasFormerActionRow =
    $metrics.ready.formerActionColorPixels -gt $formerActionPixelThreshold -and
    $metrics.ready.formerActionColorSpanPixels -gt $formerActionSpanThreshold
  $activeHasFormerActionRow =
    $metrics.activeWave.formerActionColorPixels -gt $formerActionPixelThreshold -and
    $metrics.activeWave.formerActionColorSpanPixels -gt $formerActionSpanThreshold
  if ($readyHasFormerActionRow -or $activeHasFormerActionRow) {
    throw (
      "Former bottom action-row signature is still present: " +
      "ready=$($metrics.ready.formerActionColorPixels)/$($metrics.ready.formerActionColorSpanPixels) " +
      "active=$($metrics.activeWave.formerActionColorPixels)/$($metrics.activeWave.formerActionColorSpanPixels).")
  }

  $manifest = [ordered]@{
    accepted = $true
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    url = $Url
    levelId = $LevelId
    expectedCompositeIdentity = $expectedLevelIdentity
    viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
    safeArea = $safeAreaEvidence
    runtimeUi = $runtimeUiIdentity
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
      waveActionPressedBeforeRelease = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
      pausedModalFinalRasterOpticalAlignment = 'pass'
      pauseActionsPressedBeforeReleaseAndContained = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
      oldBottomActionRowAbsent = 'pass'
      noLargeNearBlackRegions = 'pass'
      pauseContinuePreservesRun = 'pass'
      pauseRestartProducesCleanReadyState = 'pass'
      selectedToolState = 'pass'
      selectedToolAvailableToClickedHashChanged = 'pass'
      legalInteractionCue = 'pass'
      illegalInteractionCue = 'pass'
      inspectionClickInformationAndRange = 'pass'
      destinationClickNoRelocation = 'pass'
      dragRelocation = 'pass'
      battleTerminalResultCard = 'pass'
      terminalPreviewRestartReturnsReadyWithoutRouteSubmission = 'pass'
    }
    delivery = $delivery
    routeIdentities = [ordered]@{ battle = $directBattleIdentity }
    screenshots = $screenshots
    imageMetrics = $metrics
    opticalMeasurements = [ordered]@{ pausedModal = $pausedModalOpticalEvidence }
    interactionPolishEvidence = if ($InteractionPolishEvidence) {
      [ordered]@{
        waveActionRect = $waveActionRect
        waveActionPressDifference = $waveActionPressDifference
        waveActionPressInset = $waveActionPressInset
        pauseContinueRect = $pauseContinueRect
        pauseActionBandRect = $pauseActionBandRect
        pauseContinuePressDifference = $pauseContinuePressDifference
        pauseContinuePressInset = $pauseContinuePressInset
        pauseRestartRect = $pauseRestartRect
        pauseRestartPressDifference = $pauseRestartPressDifference
        pauseRestartPressInset = $pauseRestartPressInset
        releaseAction = 'StartWave-pass'
      }
    } else {
      [ordered]@{ state = 'not-requested' }
    }
    controls = $controls
    referenceControls = $referenceControls
    pixelThresholds = [ordered]@{
      hudDarkPixels = $hudDarkPixelThreshold
      hudLightPixels = $hudLightPixelThreshold
      formerActionColorPixels = $formerActionPixelThreshold
      formerActionColorSpanPixels = $formerActionSpanThreshold
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
    terminalCapture = [ordered]@{
      outcome = $BattleTerminalOutcome
      state = "terminal-$BattleTerminalOutcome"
      routeBeforeScreenshot = $terminalRouteBefore
      routeAfterScreenshot = $terminalRouteAfter
      sha256 = $terminalHash
      previewOnly = $true
      productionSubmissionEvidence = 'Flow mode / task 5.3'
      restart = [ordered]@{
        route = $terminalRestartRoute
        sessionId = $terminalRestartIdentity.sessionId
        seed = $terminalRestartIdentity.seed
        sameSession = $true
        resultCardDismissed = $true
      }
    }
    selectedToolCapture = [ordered]@{
      state = 'selected-tool'
      availableSha256 = $toolAvailableHash
      selectedSha256 = $selectedToolHash
      realClick = [ordered]@{
        x = $controls.weaponGatling.x
        y = $controls.weaponGatling.y
      }
      changed = $true
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
