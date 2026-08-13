param(
  [string]$Server = '175.178.80.66',
  [string]$User = 'root',
  [string]$RemoteDir = '/root/app/furitDefense',
  [string]$KeyPath = "$HOME\.ssh\id_ed25519",
  [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe',
  [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$webBuild = Join-Path $projectDir 'Builds\WebGL'
$visualAcceptance = Join-Path $projectDir 'scripts\accept-webgl-portrait.ps1'
$archive = Join-Path $env:TEMP 'fruitDefense-unity-webgl-deploy.tar.gz'
$staging = Join-Path $env:TEMP 'fruitDefense-unity-webgl-deploy'
$transitionId = "$(Get-Date -Format 'yyyyMMdd-HHmmss')-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
$transitionRoot = Join-Path $projectDir "Logs\visual-acceptance-transition\$transitionId"
$transitionProfile = Join-Path $env:TEMP "fruit-defense-release-cache-$transitionId"
$transitionEvidencePath = Join-Path $projectDir 'Builds\Pipeline\deployment-transition.json'
$seedOutput = Join-Path $transitionRoot 'seed'
$candidateOutput = Join-Path $transitionRoot 'candidate'
$seedManifestPath = Join-Path $seedOutput 'cache-seed.json'
$candidateManifestPath = Join-Path $candidateOutput 'acceptance.json'
$publicUrl = "http://${Server}:3000/"
Remove-Item -LiteralPath $transitionEvidencePath -Force -ErrorAction SilentlyContinue
$target = "$User@$Server"
$sshOptions = @(
  '-i', $KeyPath,
  '-o', 'BatchMode=yes',
  '-o', 'IdentitiesOnly=yes',
  '-o', 'StrictHostKeyChecking=accept-new',
  '-o', 'ConnectTimeout=10'
)

if (-not (Test-Path -LiteralPath $KeyPath)) {
  throw "SSH private key not found: $KeyPath"
}

if (-not $SkipBuild) {
  if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity editor not found: $UnityPath"
  }

  $webGLSupport = Join-Path (Split-Path $UnityPath) 'Data\PlaybackEngines\WebGLSupport'
  if (-not (Test-Path -LiteralPath $webGLSupport)) {
    throw "Unity Web Build Support is not installed: $webGLSupport"
  }

  $smokeLog = Join-Path $projectDir 'Logs\smoke-webgl-predeploy.log'
  $smokeArgs = @(
    '-batchmode', '-quit', '-nographics', '-projectPath', $projectDir,
    '-executeMethod', 'FruitDefense.Editor.ProjectSetup.SmokeValidate', '-logFile', $smokeLog
  )
  $smoke = Start-Process -FilePath $UnityPath -ArgumentList $smokeArgs -PassThru -Wait
  if ($smoke.ExitCode -ne 0 -or -not (Select-String -LiteralPath $smokeLog -Pattern 'FRUIT_DEFENSE_SMOKE_OK' -Quiet)) {
    throw "Unity smoke validation failed. See $smokeLog"
  }

  $buildLog = Join-Path $projectDir 'Logs\build-webgl.log'
  $buildArgs = @(
    '-batchmode', '-quit', '-nographics', '-projectPath', $projectDir,
    '-executeMethod', 'FruitDefense.Editor.WebBuild.Build', '-logFile', $buildLog
  )
  $build = Start-Process -FilePath $UnityPath -ArgumentList $buildArgs -PassThru -Wait
  if ($build.ExitCode -ne 0 -or -not (Select-String -LiteralPath $buildLog -Pattern 'FRUIT_DEFENSE_WEB_BUILD_OK' -Quiet)) {
    throw "Unity Web build failed. See $buildLog"
  }
}

if (-not (Test-Path -LiteralPath (Join-Path $webBuild 'index.html'))) {
  throw "Web build output not found: $webBuild"
}
if (-not (Test-Path -LiteralPath $visualAcceptance)) {
  throw "WebGL visual acceptance script not found: $visualAcceptance"
}

Write-Host 'Running local portrait visual acceptance...'
& $visualAcceptance -ServeLocal -BuildRoot $webBuild

$baselineReachable = $false
try {
  $baselineResponse = Invoke-WebRequest -UseBasicParsing -Method Head -Uri $publicUrl -TimeoutSec 10
  $baselineReachable = $baselineResponse.StatusCode -eq 200
}
catch {
  Write-Warning "No reachable previous WebGL release; transition will be recorded as first-release: $($_.Exception.Message)"
}
if ($baselineReachable) {
  Write-Host 'Seeding the currently deployed WebGL release into a persistent browser profile...'
  & $visualAcceptance `
    -Url $publicUrl `
    -TimeoutSeconds 120 `
    -ProfilePath $transitionProfile `
    -OutputDirectory $seedOutput `
    -CacheSeedOnly
  if (-not (Test-Path -LiteralPath $seedManifestPath -PathType Leaf)) {
    throw "WebGL cache seed evidence was not created: $seedManifestPath"
  }
}

try {
  Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
  Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Path (Join-Path $staging 'dist'), (Join-Path $staging 'deploy') | Out-Null
  Copy-Item -Path (Join-Path $webBuild '*') -Destination (Join-Path $staging 'dist') -Recurse -Force
  Copy-Item -Path (Join-Path $projectDir 'deploy\*') -Destination (Join-Path $staging 'deploy') -Recurse -Force

  tar -czf $archive -C $staging dist deploy
  if ($LASTEXITCODE -ne 0) { throw 'Creating deployment archive failed.' }

  scp @sshOptions $archive "${target}:/tmp/fruitDefense-deploy.tar.gz"
  if ($LASTEXITCODE -ne 0) { throw 'Upload failed.' }

  $remoteCommand = @"
set -e
if [ -f '$RemoteDir/deploy/service.sh' ]; then
  sed -i 's/\r$//' '$RemoteDir/deploy/service.sh'
  bash '$RemoteDir/deploy/service.sh' stop
fi
fuser -k 3000/tcp 2>/dev/null || true
mkdir -p '$RemoteDir'
rm -rf '$RemoteDir/dist' '$RemoteDir/deploy'
tar -xzf /tmp/fruitDefense-deploy.tar.gz -C '$RemoteDir'
sed -i 's/\r$//' '$RemoteDir/deploy/service.sh'
chmod +x '$RemoteDir/deploy/service.sh'
bash '$RemoteDir/deploy/service.sh' start
rm -f /tmp/fruitDefense-deploy.tar.gz
for attempt in `$(seq 1 20); do
  if curl -fsS http://127.0.0.1:3000/ >/dev/null; then break; fi
  sleep 0.25
done
curl -fsS http://127.0.0.1:3000/ >/dev/null
echo 'remote entry health check passed'
headers_file=/tmp/fruit-defense-webgl-headers
loader_path=`$(grep -o 'WebGL\.loader\.js?v=[0-9a-f]\{12\}' '$RemoteDir/dist/index.html' | head -n 1)
data_path=`$(grep -o 'WebGL\.data\.unityweb?v=[0-9a-f]\{12\}' '$RemoteDir/dist/index.html' | head -n 1)
framework_path=`$(grep -o 'WebGL\.framework\.js\.unityweb?v=[0-9a-f]\{12\}' '$RemoteDir/dist/index.html' | head -n 1)
wasm_path=`$(grep -o 'WebGL\.wasm\.unityweb?v=[0-9a-f]\{12\}' '$RemoteDir/dist/index.html' | head -n 1)
for asset_path in "`$loader_path" "`$data_path" "`$framework_path" "`$wasm_path"; do
  if [ -z "`$asset_path" ]; then echo 'missing advertised WebGL asset path' >&2; exit 1; fi
  version="`${asset_path##*?v=}"
  file_name="`${asset_path%%?v=*}"
  digest=`$(sha256sum '$RemoteDir/dist/Build/'"`$file_name" | awk '{print `$1}')
  if [ "`${digest#`$version}" = "`$digest" ]; then echo "version does not match bytes: `$asset_path" >&2; exit 1; fi
  curl -fsSI "http://127.0.0.1:3000/Build/`$asset_path" | tr -d '\r' > "`$headers_file"
  cache_control=`$(awk 'BEGIN{IGNORECASE=1} /^cache-control:/ {sub(/^[^:]*:[[:space:]]*/, ""); print; exit}' "`$headers_file")
  etag=`$(awk 'BEGIN{IGNORECASE=1} /^etag:/ {sub(/^[^:]*:[[:space:]]*/, ""); print; exit}' "`$headers_file")
  content_type=`$(awk 'BEGIN{IGNORECASE=1} /^content-type:/ {sub(/^[^:]*:[[:space:]]*/, ""); print; exit}' "`$headers_file")
  case "`$cache_control" in *public*max-age=31536000*immutable*) ;; *) echo "invalid cache-control for `$asset_path: `$cache_control" >&2; exit 1;; esac
  if [ "`$etag" != "\"`$digest\"" ]; then echo "invalid etag for `$asset_path: `$etag" >&2; exit 1; fi
  if [[ "`$asset_path" == WebGL.loader.js* ]]; then
    case "`$content_type" in text/javascript*) ;; *) echo "invalid loader content-type: `$content_type" >&2; exit 1;; esac
  else
    case "`$content_type" in application/octet-stream*) ;; *) echo "invalid binary content-type: `$content_type" >&2; exit 1;; esac
  fi
  if grep -qi '^content-encoding:' "`$headers_file"; then cat "`$headers_file" >&2; rm -f "`$headers_file"; exit 1; fi
done
curl -fsSI "http://127.0.0.1:3000/Build/WebGL.wasm.unityweb?v=000000000000" | tr -d '\r' > "`$headers_file"
if grep -Eqi '^cache-control: .*immutable' "`$headers_file"; then cat "`$headers_file" >&2; rm -f "`$headers_file"; exit 1; fi
rm -f "`$headers_file"
echo 'remote WebGL per-asset delivery headers passed'
bash '$RemoteDir/deploy/service.sh' status
"@
  $remoteCommand = $remoteCommand.Replace("`r`n", "`n").Trim()

  ssh @sshOptions $target $remoteCommand
  if ($LASTEXITCODE -ne 0) { throw 'Remote deployment failed.' }

  Write-Host 'Running deployed portrait visual acceptance...'
  if ($baselineReachable) {
    & $visualAcceptance `
      -Url $publicUrl `
      -TimeoutSeconds 120 `
      -ProfilePath $transitionProfile `
      -CacheSeedManifestPath $seedManifestPath `
      -OutputDirectory $candidateOutput
  }
  else {
    & $visualAcceptance `
      -Url $publicUrl `
      -TimeoutSeconds 120 `
      -OutputDirectory $candidateOutput
  }
  if (-not (Test-Path -LiteralPath $candidateManifestPath -PathType Leaf)) {
    throw "Deployed WebGL acceptance evidence was not created: $candidateManifestPath"
  }
  $candidateManifest = Get-Content -LiteralPath $candidateManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
  $transition = if ($baselineReachable) {
    $candidateManifest.delivery.releaseTransition
  }
  else {
    [ordered]@{
      state = 'first-release'
      baselineAssetVersions = $null
      candidateAssetVersions = $candidateManifest.delivery.assetVersions
      reusedRoles = @()
      changedRoles = @('loader', 'data', 'framework', 'wasm')
      expectedDownloadBytes = [long]$candidateManifest.delivery.cacheRuns.cold.totalPayloadTransferSize
      observedCandidateTransferBytes = [long]$candidateManifest.delivery.cacheRuns.cold.totalPayloadTransferSize
    }
  }
  $transitionEvidence = [ordered]@{
    schemaVersion = 1
    evidenceType = 'webgl-release-transition'
    accepted = $true
    baselineReachable = $baselineReachable
    publicUrl = $publicUrl
    seedManifestPath = if ($baselineReachable) { $seedManifestPath } else { $null }
    candidateManifestPath = $candidateManifestPath
    releaseTransition = $transition
  }
  $transitionDirectory = Split-Path -Parent $transitionEvidencePath
  New-Item -ItemType Directory -Path $transitionDirectory -Force | Out-Null
  $transitionEvidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $transitionEvidencePath -Encoding UTF8
  Write-Host "Deployment complete: $publicUrl"
}
finally {
  Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
  Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue
  $temporaryRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
  $resolvedTransitionProfile = [IO.Path]::GetFullPath($transitionProfile)
  if ($resolvedTransitionProfile.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $resolvedTransitionProfile -Recurse -Force -ErrorAction SilentlyContinue
  }
}
