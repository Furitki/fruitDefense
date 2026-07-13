param(
  [string]$Server = '175.178.80.66',
  [string]$User = 'root',
  [string]$RemoteDir = '/root/app/furitDefense',
  [string]$KeyPath = "$HOME\.ssh\id_ed25519"
)

$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$archive = Join-Path $env:TEMP 'fruitDefense-deploy.tar.gz'
$target = "$User@$Server"
$sshOptions = @(
  '-i', $KeyPath,
  '-o', 'BatchMode=yes',
  '-o', 'IdentitiesOnly=yes',
  '-o', 'ConnectTimeout=10'
)

if (-not (Test-Path -LiteralPath $KeyPath)) {
  throw "SSH private key not found: $KeyPath"
}

Push-Location $projectDir
try {
  npm run typecheck
  if ($LASTEXITCODE -ne 0) { throw 'Typecheck failed.' }

  npm test
  if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }

  npm run build
  if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

  if (Test-Path $archive) {
    Remove-Item -LiteralPath $archive -Force
  }
  tar -czf $archive dist deploy
  if ($LASTEXITCODE -ne 0) { throw 'Creating deployment archive failed.' }

  Write-Host "Uploading to $target ..."
  scp @sshOptions $archive "${target}:/tmp/fruitDefense-deploy.tar.gz"
  if ($LASTEXITCODE -ne 0) { throw 'Upload failed.' }

  $remoteCommand = @"
set -e
if [ -x '$RemoteDir/deploy/service.sh' ]; then '$RemoteDir/deploy/service.sh' stop; fi
fuser -k 3000/tcp 2>/dev/null || true
fuser -k 3001/tcp 2>/dev/null || true
fuser -k 3002/tcp 2>/dev/null || true
mkdir -p '$RemoteDir'
rm -rf '$RemoteDir/dist' '$RemoteDir/deploy'
tar -xzf /tmp/fruitDefense-deploy.tar.gz -C '$RemoteDir'
chmod +x '$RemoteDir/deploy/service.sh'
'$RemoteDir/deploy/service.sh' start
rm -f /tmp/fruitDefense-deploy.tar.gz
curl -fsS http://127.0.0.1:3000/ >/dev/null
'$RemoteDir/deploy/service.sh' status
"@

  Write-Host 'Switching the server to the new build ...'
  ssh @sshOptions $target $remoteCommand
  if ($LASTEXITCODE -ne 0) { throw 'Remote deployment failed.' }

  Write-Host "Deployment complete: http://${Server}:3000/"
}
finally {
  Pop-Location
  if (Test-Path $archive) {
    Remove-Item -LiteralPath $archive -Force
  }
}
