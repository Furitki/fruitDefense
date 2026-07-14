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
wasm_path=`$(grep -o 'WebGL\.wasm\.unityweb?v=[0-9a-f]\{12\}' '$RemoteDir/dist/index.html' | head -n 1)
test -n "`$wasm_path"
headers_file=/tmp/fruit-defense-webgl-headers
curl -fsSI "http://127.0.0.1:3000/Build/`$wasm_path" | tr -d '\r' > "`$headers_file"
grep -qi '^content-type: application/octet-stream' "`$headers_file"
if grep -qi '^content-encoding:' "`$headers_file"; then cat "`$headers_file" >&2; rm -f "`$headers_file"; exit 1; fi
grep -Eqi '^cache-control: .*max-age=31536000.*immutable' "`$headers_file"
rm -f "`$headers_file"
echo 'remote WebGL delivery headers passed'
bash '$RemoteDir/deploy/service.sh' status
"@
  $remoteCommand = $remoteCommand.Replace("`r`n", "`n").Trim()

  ssh @sshOptions $target $remoteCommand
  if ($LASTEXITCODE -ne 0) { throw 'Remote deployment failed.' }

  $publicUrl = "http://${Server}:3000"
  Write-Host 'Running deployed portrait visual acceptance...'
  & $visualAcceptance -Url "$publicUrl/" -TimeoutSeconds 120
  Write-Host "Deployment complete: $publicUrl/"
}
finally {
  Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
  Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue
}
