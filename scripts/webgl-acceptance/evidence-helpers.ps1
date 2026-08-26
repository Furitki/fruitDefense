# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

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
  if ($themeId -cne 'ui.sunny-orchard' -or $themeRevision -cne '2') {
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
    $contentLengthHeader = @($response.Headers['Content-Length']) |
      Select-Object -First 1
    $contentLength = [long]$contentLengthHeader
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
    $wrongVersionUrl = Set-WebGlUrlQueryParameter -TargetUrl $assetUri.AbsoluteUri -Name 'v' -Value $wrongVersion
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

function Invoke-AcceptanceFlowCommand {
  param([string]$Command)
  Assert-AcceptanceBuildProfileVerified
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
