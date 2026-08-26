# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Invoke-ShellErrorMode {
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
      verifiedBuildProfile = $verifiedBuildProfile
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

function Invoke-ShellVisualMode {
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
    $transitionActionContrast = Get-ActionContentContrast `
      -Path $transitionCapture.Path -Rect $lobbyStartRect `
      -Polarity LightOnDark
    if (-not $transitionActionContrast.passed) {
      throw (
        'Lobby transition action contrast is below 4.5: ' +
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
      verifiedBuildProfile = $verifiedBuildProfile
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
