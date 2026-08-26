param()

$ErrorActionPreference = 'Stop'
$probeScript = Join-Path $PSScriptRoot 'webgl-build-profile-probe.ps1'
. $probeScript

function Assert-Condition {
  param([bool]$Condition, [string]$Message)
  if (-not $Condition) { throw $Message }
}

function Write-ProfileFixture {
  param(
    [string]$Root,
    [AllowNull()]
    [string]$Profile
  )

  New-Item -ItemType Directory -Path $Root -Force | Out-Null
  $marker = if ($null -eq $Profile) {
    ''
  } else {
    "<meta name=`"fruit-defense-build-profile`" content=`"$Profile`">"
  }
  Set-Content -LiteralPath (Join-Path $Root 'index.html') -Encoding UTF8 -Value @"
<!doctype html>
<html><head>$marker</head><body>fixture</body></html>
"@
}

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
$fixtureRoot = Join-Path $tempBase "fruit-defense-profile-probe-$([Guid]::NewGuid().ToString('N'))"
$script:BrowserCommandCount = 0
$script:UnityMessageCount = 0
$failureCount = 0
$dynamicServerJob = $null

try {
  $acceptanceRoot = Join-Path $fixtureRoot 'correct\Builds\WebGL-Acceptance'
  $releaseRoot = Join-Path $fixtureRoot 'release-mismatch\Builds\WebGL'
  $fallbackReleaseRoot = Join-Path $fixtureRoot 'no-acceptance\Builds\WebGL'
  $missingAcceptanceRoot = Join-Path $fixtureRoot 'no-acceptance\Builds\WebGL-Acceptance'
  $missingRoot = Join-Path $fixtureRoot 'missing-marker'
  $unknownRoot = Join-Path $fixtureRoot 'unknown-marker'
  Write-ProfileFixture -Root $acceptanceRoot -Profile 'acceptance'
  Write-ProfileFixture -Root $releaseRoot -Profile 'release'
  Write-ProfileFixture -Root $fallbackReleaseRoot -Profile 'release'
  Write-ProfileFixture -Root $missingRoot -Profile $null
  Write-ProfileFixture -Root $unknownRoot -Profile 'staging'

  $verified = Invoke-VerifiedWebGlBuildProfileAction `
    -ExpectedProfile acceptance `
    -BuildRoot $acceptanceRoot `
    -Action {
      param($Profile)
      $script:BrowserCommandCount++
      return $Profile
    }
  Assert-Condition ($verified.verifiedProfile -ceq 'acceptance') `
    'Correct acceptance profile did not pass the probe.'

  foreach ($failure in @(
    [ordered]@{ name = 'release-mismatch'; root = $releaseRoot },
    [ordered]@{ name = 'missing-marker'; root = $missingRoot },
    [ordered]@{ name = 'unknown-marker'; root = $unknownRoot },
    [ordered]@{ name = 'missing-acceptance-output'; root = $missingAcceptanceRoot }
  )) {
    $beforeBrowser = $script:BrowserCommandCount
    $beforeMessage = $script:UnityMessageCount
    $failedClosed = $false
    try {
      Invoke-VerifiedWebGlBuildProfileAction `
        -ExpectedProfile acceptance `
        -BuildRoot $failure.root `
        -Action {
          param($Profile)
          $script:BrowserCommandCount++
          $script:UnityMessageCount++
        } | Out-Null
    }
    catch {
      $failedClosed = $true
    }
    Assert-Condition $failedClosed "Profile case '$($failure.name)' unexpectedly passed."
    Assert-Condition ($script:BrowserCommandCount -eq $beforeBrowser) `
      "Profile case '$($failure.name)' invoked a browser action after verification failure."
    Assert-Condition ($script:UnityMessageCount -eq $beforeMessage) `
      "Profile case '$($failure.name)' invoked a Unity message after verification failure."
    $failureCount++
  }

  Assert-Condition (Test-Path -LiteralPath (Join-Path $fallbackReleaseRoot 'index.html') -PathType Leaf) `
    'Release fallback fixture must exist for the missing acceptance-output proof.'
  Assert-Condition (-not (Test-Path -LiteralPath $missingAcceptanceRoot)) `
    'Acceptance output must remain absent for the no-fallback proof.'
  Assert-Condition ($script:BrowserCommandCount -eq 1) `
    'Only the verified acceptance case may reach the guarded action.'
  Assert-Condition ($script:UnityMessageCount -eq 0) `
    'No failure case may reach a simulated Unity SendMessage action.'

  $portListener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
  $portListener.Start()
  $dynamicPort = ([Net.IPEndPoint]$portListener.LocalEndpoint).Port
  $portListener.Stop()
  $dynamicServerJob = Start-Job -ScriptBlock {
    param([int]$Port)
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
    $listener.Start()
    try {
      for ($requestIndex = 0; $requestIndex -lt 3; $requestIndex++) {
        $client = $listener.AcceptTcpClient()
        try {
          $stream = $client.GetStream()
          $reader = [IO.StreamReader]::new(
            $stream, [Text.Encoding]::ASCII, $false, 1024, $true)
          $requestLine = $reader.ReadLine()
          while (-not [string]::IsNullOrEmpty($reader.ReadLine())) { }
          $profile = if ($requestLine -match '(?:\?|&)acceptance=1(?:&|\s)') {
            'release'
          } else { 'acceptance' }
          $body = "<!doctype html><meta name=`"fruit-defense-build-profile`" content=`"$profile`">"
          $bodyBytes = [Text.Encoding]::UTF8.GetBytes($body)
          $headers = [Text.Encoding]::ASCII.GetBytes(
            "HTTP/1.1 200 OK`r`nContent-Type: text/html; charset=utf-8`r`n" +
            "Content-Length: $($bodyBytes.Length)`r`nConnection: close`r`n`r`n")
          $stream.Write($headers, 0, $headers.Length)
          $stream.Write($bodyBytes, 0, $bodyBytes.Length)
          $stream.Flush()
        }
        finally { $client.Dispose() }
      }
    }
    finally { $listener.Stop() }
  } -ArgumentList $dynamicPort
  $baseUrl = "http://127.0.0.1:$dynamicPort/"
  $baseVerified = $null
  $baseDeadline = (Get-Date).AddSeconds(5)
  do {
    try {
      $baseVerified = Assert-WebGlBuildProfile `
        -ExpectedProfile acceptance -Url $baseUrl -TimeoutSeconds 2
    }
    catch {
      if ((Get-Date) -ge $baseDeadline) { throw }
      Start-Sleep -Milliseconds 50
    }
  } while ($null -eq $baseVerified)
  Assert-Condition ($baseVerified.verifiedProfile -ceq 'acceptance') `
    'Dynamic base URL must expose the acceptance profile before query finalization.'

  $finalUrl = Set-WebGlUrlQueryParameter `
    -TargetUrl $baseUrl -Name 'acceptance' -Value '1'
  $finalUrl = Set-WebGlUrlQueryParameter `
    -TargetUrl $finalUrl -Name 'levelId' -Value 'orchard-01'
  $finalUrl = Set-WebGlUrlQueryParameter `
    -TargetUrl $finalUrl -Name 'safeTop' -Value '0'
  $finalUrl = Set-WebGlUrlQueryParameter `
    -TargetUrl $finalUrl -Name 'safeBottom' -Value '0'
  $beforeDynamicBrowser = $script:BrowserCommandCount
  $beforeDynamicMessage = $script:UnityMessageCount
  $dynamicFailedClosed = $false
  try {
    Invoke-VerifiedWebGlBuildProfileAction `
      -ExpectedProfile acceptance `
      -Url $finalUrl `
      -Action {
        param($Profile)
        $script:BrowserCommandCount++
        $script:UnityMessageCount++
      } | Out-Null
  }
  catch {
    $dynamicFailedClosed = $true
  }
  Assert-Condition $dynamicFailedClosed `
    'Final acceptance query URL unexpectedly accepted a dynamic release profile.'
  Assert-Condition ($script:BrowserCommandCount -eq $beforeDynamicBrowser) `
    'Dynamic final-query mismatch invoked a browser action before profile verification.'
  Assert-Condition ($script:UnityMessageCount -eq $beforeDynamicMessage) `
    'Dynamic final-query mismatch invoked a Unity message before profile verification.'
  $failureCount++

  $portraitOutput = & pwsh -NoProfile -File (
      Join-Path $PSScriptRoot 'accept-webgl-portrait.ps1') `
    -Url $baseUrl `
    -Flow `
    -ChromePath (Join-Path $fixtureRoot 'must-not-launch-chrome.exe') `
    -OutputDirectory (Join-Path $fixtureRoot 'portrait-final-query-probe') `
    -TimeoutSeconds 2 2>&1 | Out-String
  $portraitExitCode = $LASTEXITCODE
  Assert-Condition ($portraitExitCode -ne 0) `
    'Portrait runner unexpectedly accepted the dynamic final-query release profile.'
  Assert-Condition ($portraitOutput -match "expected 'acceptance', found 'release'") `
    "Portrait runner did not reject the exact final URL profile: $portraitOutput"
  Assert-Condition ($portraitOutput -notmatch 'Chrome not found') `
    'Portrait runner reached Chrome validation before rejecting the final URL profile.'
  $failureCount++

  Write-Host (
    'FRUIT_DEFENSE_WEBGL_BUILD_PROFILE_PROBE_SELF_CHECK_OK ' +
    "verified=acceptance failureCases=$failureCount browserActions=$script:BrowserCommandCount " +
    "unityMessages=$script:UnityMessageCount releaseFallback=absent " +
    'dynamicFinalQueryRelease=rejected-before-browser portraitFinalUrl=verified')
}
finally {
  if ($null -ne $dynamicServerJob) {
    Stop-Job -Job $dynamicServerJob -ErrorAction SilentlyContinue
    Remove-Job -Job $dynamicServerJob -Force -ErrorAction SilentlyContinue
  }
  $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
  if ($resolvedFixture.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) -and
      (Split-Path $resolvedFixture -Leaf).StartsWith('fruit-defense-profile-probe-', [StringComparison]::Ordinal)) {
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force -ErrorAction SilentlyContinue
  }
}
