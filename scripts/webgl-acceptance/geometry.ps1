# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

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

function Convert-ShellReferenceRect {
  param([double]$X, [double]$Y, [double]$Width, [double]$Height)
  return [ordered]@{
    xMin = $shellContentX + ($X - 16.0) * $referenceScale
    yMin = $SafeTop + $Y * $referenceScale
    xMax = $shellContentX + ($X - 16.0 + $Width) * $referenceScale
    yMax = $SafeTop + ($Y + $Height) * $referenceScale
  }
}
