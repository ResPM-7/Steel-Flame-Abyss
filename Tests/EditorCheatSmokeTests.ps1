$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $PSScriptRoot '../Assets/01.Scripts/Battle/UnitFSM/CombatantState.cs'),
    (Join-Path $PSScriptRoot '../Assets/01.Scripts/Coin/CoinModel.cs'),
    (Join-Path $PSScriptRoot '../Assets/01.Scripts/Battle/Card/OwnedDeckModel.cs'))
$script:checks = 0
#치트에 사용되는 상태 변경 결과 검증
function Assert-Cheat([bool] $condition) {
    if (-not $condition) { throw "Cheat check $script:checks failed" }
    $script:checks++
}
$enemy = [CombatantState]::new(80)
$enemy.GainBlock(20)
Assert-Cheat ($enemy.TakeDamage(10) -eq 0 -and $enemy.CurrentHealth -eq 80)
$enemy.ClearBlock()
Assert-Cheat ($enemy.TakeDamage($enemy.CurrentHealth) -eq 80 -and -not $enemy.IsAlive)
Assert-Cheat ($enemy.Heal(80) -eq 0)
$player = [CombatantState]::new(80, 20)
Assert-Cheat ($player.Heal(80) -eq 60 -and $player.CurrentHealth -eq 80)
$coins = [CoinModel]::new()
Assert-Cheat ($coins.TryAdd(100) -and $coins.Amount -eq 100)
Assert-Cheat (-not $coins.TryAdd(-1) -and $coins.Amount -eq 100)
Assert-Cheat (-not $coins.TryAdd([int]::MaxValue) -and $coins.Amount -eq 100)
$deck = [OwnedDeckModel]::new()
$deck.InitializeOnce([int[]] @(10001))
$deck.Add(10002)
Assert-Cheat ($deck.CardIds.Count -eq 2 -and $deck.CardIds[1] -eq 10002)
Write-Output "PASS: $script:checks cheat state checks"
