function Set-WebGlUrlQueryParameter {
  param([string]$TargetUrl, [string]$Name, [string]$Value)

  $builder = [UriBuilder]::new($TargetUrl)
  $pairs = [ordered]@{}
  foreach ($part in $builder.Query.TrimStart('?').Split(
      '&', [StringSplitOptions]::RemoveEmptyEntries)) {
    $components = $part.Split('=', 2)
    $key = [Uri]::UnescapeDataString($components[0])
    $pairs[$key] = if ($components.Count -eq 2) {
      [Uri]::UnescapeDataString($components[1])
    } else { '' }
  }
  $pairs[$Name] = $Value
  $builder.Query = (($pairs.GetEnumerator() | ForEach-Object {
    [Uri]::EscapeDataString([string]$_.Key) + '=' +
      [Uri]::EscapeDataString([string]$_.Value)
  }) -join '&')
  return $builder.Uri.AbsoluteUri
}

function Get-WebGlProfileMetaContent {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Html
  )

  $markerName = 'fruit-defense-build-profile'
  $values = @()
  foreach ($tagMatch in [regex]::Matches($Html, '(?is)<meta\b[^>]*>')) {
    $tag = $tagMatch.Value
    $attributes = @{}
    foreach ($attributeMatch in [regex]::Matches(
        $tag,
        '(?is)\b(?<name>[a-z_:][-a-z0-9_:.]*)\s*=\s*(?:"(?<double>[^"]*)"|''(?<single>[^'']*)''|(?<bare>[^\s>]+))')) {
      $attributeName = $attributeMatch.Groups['name'].Value.ToLowerInvariant()
      $attributeValue = if ($attributeMatch.Groups['double'].Success) {
        $attributeMatch.Groups['double'].Value
      } elseif ($attributeMatch.Groups['single'].Success) {
        $attributeMatch.Groups['single'].Value
      } else {
        $attributeMatch.Groups['bare'].Value
      }
      $attributes[$attributeName] = [Net.WebUtility]::HtmlDecode($attributeValue)
    }

    if ($attributes.ContainsKey('name') -and $attributes['name'] -eq $markerName) {
      if (-not $attributes.ContainsKey('content')) {
        throw "WebGL host profile marker '$markerName' has no content attribute."
      }
      $values += [string]$attributes['content']
    }
  }

  if ($values.Count -eq 0) {
    throw "WebGL host profile marker '$markerName' is missing."
  }
  if ($values.Count -ne 1) {
    throw "WebGL host must contain exactly one '$markerName' marker; found $($values.Count)."
  }
  return $values[0]
}

function Assert-WebGlBuildProfile {
  [CmdletBinding(DefaultParameterSetName = 'BuildRoot')]
  param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('release', 'acceptance')]
    [string]$ExpectedProfile,

    [Parameter(Mandatory = $true, ParameterSetName = 'BuildRoot')]
    [string]$BuildRoot,

    [Parameter(Mandatory = $true, ParameterSetName = 'Url')]
    [string]$Url,

    [ValidateRange(1, 300)]
    [int]$TimeoutSeconds = 30
  )

  $sourceKind = $PSCmdlet.ParameterSetName
  if ($sourceKind -eq 'BuildRoot') {
    $resolvedRoot = [IO.Path]::GetFullPath($BuildRoot)
    $indexPath = Join-Path $resolvedRoot 'index.html'
    if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
      throw "WebGL build profile probe requires the exact output index: $indexPath"
    }
    $source = $indexPath
    $html = Get-Content -LiteralPath $indexPath -Raw
  } else {
    if ([string]::IsNullOrWhiteSpace($Url)) {
      throw 'WebGL build profile probe requires a non-empty URL.'
    }
    $source = $Url
    $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec $TimeoutSeconds
    if ([int]$response.StatusCode -lt 200 -or [int]$response.StatusCode -ge 300) {
      throw "WebGL build profile probe returned HTTP $($response.StatusCode): $Url"
    }
    $html = [string]$response.Content
  }

  $actualProfile = Get-WebGlProfileMetaContent -Html $html
  if ($actualProfile -notin @('release', 'acceptance')) {
    throw "Unknown WebGL build profile '$actualProfile' from $source."
  }
  if ($actualProfile -cne $ExpectedProfile) {
    throw "WebGL build profile mismatch: expected '$ExpectedProfile', found '$actualProfile' at $source."
  }

  return [pscustomobject][ordered]@{
    marker = 'fruit-defense-build-profile'
    expectedProfile = $ExpectedProfile
    verifiedProfile = $actualProfile
    sourceKind = if ($sourceKind -eq 'BuildRoot') { 'build-root' } else { 'url' }
    source = $source
    verifiedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
  }
}

function Invoke-VerifiedWebGlBuildProfileAction {
  [CmdletBinding(DefaultParameterSetName = 'BuildRoot')]
  param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('release', 'acceptance')]
    [string]$ExpectedProfile,

    [Parameter(Mandatory = $true, ParameterSetName = 'BuildRoot')]
    [string]$BuildRoot,

    [Parameter(Mandatory = $true, ParameterSetName = 'Url')]
    [string]$Url,

    [ValidateRange(1, 300)]
    [int]$TimeoutSeconds = 30,

    [Parameter(Mandatory = $true)]
    [scriptblock]$Action
  )

  $profile = if ($PSCmdlet.ParameterSetName -eq 'BuildRoot') {
    Assert-WebGlBuildProfile `
      -ExpectedProfile $ExpectedProfile `
      -BuildRoot $BuildRoot `
      -TimeoutSeconds $TimeoutSeconds
  } else {
    Assert-WebGlBuildProfile `
      -ExpectedProfile $ExpectedProfile `
      -Url $Url `
      -TimeoutSeconds $TimeoutSeconds
  }
  return & $Action $profile
}
