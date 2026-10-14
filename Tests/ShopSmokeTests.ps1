$ErrorActionPreference = 'Stop'
#유니티 데이터 대신 최소 카드 정의로 순수 구매 모델 검증
$stubs = @'
public enum CharacterClass { Common, Warrior, FireMage }
public enum CardType { Attack, Status }
public class CardData {
    public int Id;
    public CharacterClass Owner;
    public CardType CardType;
}
'@
$sources = @('ShopModel.cs', 'OwnedDeckModel.cs') | ForEach-Object {
    Get-Content (Join-Path $PSScriptRoot "../Assets/01.Scripts/Battle/Card/$_") -Raw
}
$sources += Get-Content (Join-Path $PSScriptRoot '../Assets/01.Scripts/Coin/CoinModel.cs') -Raw
Add-Type -TypeDefinition ("using System; using System.Collections.Generic;`n" + $stubs + "`n" + (($sources | ForEach-Object { $_ -replace '(?m)^using .+;\r?\n', '' }) -join "`n"))
$script:checks = 0
#구매 결과와 보유량 일치 검증
function Assert-Shop([bool] $condition) {
    if (-not $condition) { throw "Shop check $script:checks failed" }
    $script:checks++
}
#테스트 카드의 식별자와 소유자 구성
function New-TestCard([int] $id, [CharacterClass] $owner, [CardType] $type = 'Attack') {
    $card = [CardData]::new()
    $card.Id = $id
    $card.Owner = $owner
    $card.CardType = $type
    return $card
}
$cards = [CardData[]] @((New-TestCard 1 Warrior), (New-TestCard 2 Common), (New-TestCard 3 Warrior),
    (New-TestCard 1 Warrior), (New-TestCard 4 FireMage), (New-TestCard 5 Warrior Status))
$shop = [ShopModel]::new($cards, 'Warrior', [Random]::new(7), 50)
$coins = [CoinModel]::new()
$deck = [OwnedDeckModel]::new()
Assert-Shop ($shop.Cards.Count -eq 3)
Assert-Shop (@($shop.Cards.Id | Select-Object -Unique).Count -eq 3)
Assert-Shop (@($shop.Cards | Where-Object { $_.Id -gt 3 }).Count -eq 0)
Assert-Shop (-not $shop.TryBuy(0, $coins, $deck))
$deck.InitializeOnce([int[]] @(10))
Assert-Shop (-not $shop.TryBuy(0, $coins, $deck))
Assert-Shop (-not $shop.IsSold(0) -and $deck.CardIds.Count -eq 1)
$null = $coins.TryAdd(100)
Assert-Shop (-not $shop.TryBuy(-1, $coins, $deck))
Assert-Shop (-not $shop.TryBuy(3, $coins, $deck))
Assert-Shop ($shop.TryBuy(0, $coins, $deck))
Assert-Shop ($coins.Amount -eq 50 -and $deck.CardIds.Count -eq 2 -and $shop.IsSold(0))
Assert-Shop ($deck.CardIds[1] -eq $shop.Cards[0].Id)
Assert-Shop (-not $shop.TryBuy(0, $coins, $deck))
Assert-Shop ($coins.Amount -eq 50 -and $deck.CardIds.Count -eq 2)
Assert-Shop ($shop.TryBuy(1, $coins, $deck))
Assert-Shop ($coins.Amount -eq 0)
Assert-Shop (-not $shop.TryBuy(2, $coins, $deck) -and -not $shop.IsSold(2))
$empty = [ShopModel]::new([CardData[]] @(), 'Warrior', [Random]::new(1), 50)
Assert-Shop ($empty.Cards.Count -eq 0 -and -not $empty.TryBuy(0, $coins, $deck))
Write-Output "PASS: $script:checks shop checks"
