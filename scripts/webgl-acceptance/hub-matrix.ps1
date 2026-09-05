# Dot-sourced by accept-webgl-portrait.ps1 and the outgame hub matrix runner.

function Get-OutgameHubPortraitMatrix {
  return @(
    [pscustomobject]@{ id = 'full-360x800'; width = 360; height = 800; safeTop = 0; safeBottom = 0; kind = 'full' }
    [pscustomobject]@{ id = 'inset-360x800-32-24'; width = 360; height = 800; safeTop = 32; safeBottom = 24; kind = 'inset' }
    [pscustomobject]@{ id = 'full-375x812'; width = 375; height = 812; safeTop = 0; safeBottom = 0; kind = 'full' }
    [pscustomobject]@{ id = 'inset-375x812-40-21'; width = 375; height = 812; safeTop = 40; safeBottom = 21; kind = 'inset' }
    [pscustomobject]@{ id = 'full-402x874'; width = 402; height = 874; safeTop = 0; safeBottom = 0; kind = 'full' }
    [pscustomobject]@{ id = 'inset-402x874-44-34'; width = 402; height = 874; safeTop = 44; safeBottom = 34; kind = 'inset' }
    [pscustomobject]@{ id = 'full-430x932'; width = 430; height = 932; safeTop = 0; safeBottom = 0; kind = 'full' }
    [pscustomobject]@{ id = 'inset-430x932-50-36'; width = 430; height = 932; safeTop = 50; safeBottom = 36; kind = 'inset' }
  )
}

function Assert-OutgameHubPortraitMatrix {
  param([object[]]$Cases = (Get-OutgameHubPortraitMatrix))

  if ($Cases.Count -ne 8) {
    throw "Outgame hub matrix must contain exactly eight full/inset cases; actual=$($Cases.Count)."
  }
  $expectedSizes = @('360x800', '375x812', '402x874', '430x932')
  foreach ($size in $expectedSizes) {
    $parts = $size.Split('x')
    $width = [int]$parts[0]
    $height = [int]$parts[1]
    $matching = @($Cases | Where-Object {
        [int]$_.width -eq $width -and [int]$_.height -eq $height
      })
    if ($matching.Count -ne 2 -or
        @($matching | Where-Object { [string]$_.kind -ceq 'full' }).Count -ne 1 -or
        @($matching | Where-Object { [string]$_.kind -ceq 'inset' }).Count -ne 1) {
      throw "Outgame hub matrix requires one full and one inset case for $size."
    }
  }
  $ids = @($Cases | ForEach-Object { [string]$_.id })
  if (@($ids | Sort-Object -Unique).Count -ne $ids.Count) {
    throw 'Outgame hub matrix case identities must be unique.'
  }
  foreach ($case in $Cases) {
    if ([int]$case.width -le 0 -or [int]$case.height -le 0 -or
        [int]$case.safeTop -lt 0 -or [int]$case.safeBottom -lt 0 -or
        [int]$case.safeTop + [int]$case.safeBottom -ge [int]$case.height) {
      throw "Outgame hub matrix case has invalid geometry: $($case | ConvertTo-Json -Compress)"
    }
    if ([string]$case.kind -ceq 'full' -and
        ([int]$case.safeTop -ne 0 -or [int]$case.safeBottom -ne 0)) {
      throw "Full outgame hub matrix case contains an inset: $($case.id)"
    }
    if ([string]$case.kind -ceq 'inset' -and
        ([int]$case.safeTop -eq 0 -or [int]$case.safeBottom -eq 0)) {
      throw "Inset outgame hub matrix case is missing a top/bottom inset: $($case.id)"
    }
  }
  return $Cases
}
