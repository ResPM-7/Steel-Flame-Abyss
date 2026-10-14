$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $PSScriptRoot '../Assets/01.Scripts/Battle/UnitFSM/CombatantState.cs'), (Join-Path $PSScriptRoot '../Assets/01.Scripts/Battle/CampVisitModel.cs'))
#캠프 회복 결과 검증
function Assert-Camp([bool] $condition) {
    if (-not $condition) { throw 'Camp assertion failed' }
}
$health = [CombatantState]::new(80, 30)
$visit = [CampVisitModel]::new($health, 30)
$healed = 0
Assert-Camp ($visit.HealAmount -eq 24)
Assert-Camp ($visit.TryRest([ref]$healed))
Assert-Camp ($healed -eq 24 -and $health.CurrentHealth -eq 54)
Assert-Camp (-not $visit.TryRest([ref]$healed))
Assert-Camp ($health.CurrentHealth -eq 54)
$nextVisit = [CampVisitModel]::new($health, 100)
Assert-Camp ($nextVisit.TryRest([ref]$healed))
Assert-Camp ($healed -eq 26 -and $health.CurrentHealth -eq 80)
$fullVisit = [CampVisitModel]::new($health, 30)
Assert-Camp ($fullVisit.TryRest([ref]$healed) -and $healed -eq 0 -and $fullVisit.HasRested)
$deadVisit = [CampVisitModel]::new([CombatantState]::new(80, 0), 30)
Assert-Camp (-not $deadVisit.TryRest([ref]$healed))
Assert-Camp (-not $deadVisit.HasRested)
$smallVisit = [CampVisitModel]::new([CombatantState]::new(1), 1)
Assert-Camp ($smallVisit.HealAmount -eq 1)
Write-Output 'PASS: 11 camp checks'
