# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Invoke-AcceptanceCacheSeedMode {
    $seedManifest = [ordered]@{
      schemaVersion = 1
      evidenceType = 'webgl-cache-seed'
      accepted = $true
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      verifiedBuildProfile = $verifiedBuildProfile
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
}

function Complete-AcceptanceWarmCache {
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
  return [ordered]@{
    readiness = $readiness
    delivery = $delivery
    releaseTransition = $releaseTransition
  }
}
