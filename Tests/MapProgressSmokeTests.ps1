$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../Assets/01.Scripts/Battle/MapProgressModel.cs')

#맵 진행 조건 검증
function Assert-MapCondition([bool] $condition, [string] $message) {
    if (-not $condition) { throw $message }
}

$mapProgress = [MapProgressModel]::new(3)
Assert-MapCondition ($mapProgress.CanEnter(0)) 'First node must be available'
Assert-MapCondition (-not $mapProgress.CanEnter(1)) 'Later nodes must be locked'
Assert-MapCondition (-not $mapProgress.TryEnter(-1)) 'Negative index must be rejected'
Assert-MapCondition (-not $mapProgress.TryEnter(3)) 'Out of range index must be rejected'
Assert-MapCondition (-not $mapProgress.CompleteCurrent()) 'Cannot complete an unvisited node'
Assert-MapCondition ($mapProgress.TryEnter(0)) 'First entry must succeed'
Assert-MapCondition (-not $mapProgress.TryEnter(0)) 'Double click must be rejected'
Assert-MapCondition ($mapProgress.NextIndex -eq 0) 'Entry alone must not advance progress'
$mapProgress.CancelEntry()
Assert-MapCondition ($mapProgress.CanEnter(0)) 'Failed initialization must allow retry'
Assert-MapCondition ($mapProgress.TryEnter(0)) 'Retry must succeed'
Assert-MapCondition ($mapProgress.CompleteCurrent()) 'Completed battle must advance progress'
Assert-MapCondition (-not $mapProgress.CompleteCurrent()) 'Duplicate completion must be rejected'
Assert-MapCondition (-not $mapProgress.TryEnter(0)) 'Completed node must stay locked'
Assert-MapCondition ($mapProgress.TryEnter(1)) 'Event must open after first battle'
Assert-MapCondition ($mapProgress.CompleteCurrent()) 'Event completion must unlock boss'
Assert-MapCondition ($mapProgress.TryEnter(2)) 'Boss must open after event'
Assert-MapCondition ($mapProgress.CompleteCurrent()) 'Boss completion must finish route'
Assert-MapCondition ($mapProgress.IsComplete) 'Route must be complete'
Assert-MapCondition (-not $mapProgress.TryEnter(3)) 'Completed route must reject entry'

$failedProgress = [MapProgressModel]::new(3)
$null = $failedProgress.TryEnter(0)
$failedProgress.Fail()
Assert-MapCondition ($failedProgress.IsFailed) 'Defeat must mark failure'
Assert-MapCondition (-not $failedProgress.TryEnter(0)) 'Defeat must block retry'
Assert-MapCondition (-not $failedProgress.CompleteCurrent()) 'Defeat must block completion'
Write-Output 'PASS: 22 map progression checks'
