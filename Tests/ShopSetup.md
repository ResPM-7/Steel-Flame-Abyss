# 상점 연결과 확인

## 화면 구조

```text
ShopPanel (ShopPanelView)
├ ItemPanel
│ ├ ShopCardSlot
│ │ ├ Card (Image + Button + Outline + ShopCardItemView)
│ │ │ ├ CardNameText
│ │ │ ├ CardCostText
│ │ │ └ CardDescriptionText
│ │ └ PriceText
│ ├ ShopCardSlot (1)
│ └ ShopCardSlot (2)
└ MapButton (Button)
```

- `ShopCardItemView`는 슬롯 루트가 아니라 **Card에 추가**한다
- 상점 프리팹에 기존 CardView 또는 CardListItemView 또는 CardRewardItemView와 CardHoverTween이 있으면 상점 프리팹에서만 제거한다
- 손패나 보상 프리팹 원본은 수정하지 않는다
- Card에 Button을 추가하고 Target Graphic을 Card의 Image로 지정한다
- Card의 Image는 Raycast Target을 켜고 자식 글자와 PriceText는 끈다
- ShopCardItemView의 Name Text와 Cost Text와 Description Text에 카드 자식 텍스트를 연결한다
- Outline과 공통 Rarity Colors를 연결한다
- Button에는 같은 Card의 Button을 연결하고 Price Text에는 형제 PriceText를 연결한다
- Hover Scale 기본 1.06은 현재 크기의 106%이며 Hover Duration 기본 0.15초이다
- ItemPanel에서 슬롯들을 배치하고 슬롯 안 Card는 중앙 피벗으로 둔다
- 슬롯과 가격은 확대하지 않고 Card만 확대하므로 슬롯 사이에 여유 간격을 둔다

## 상점과 맵 연결

1. ShopPanel에 ShopPanelView를 추가한다
2. Items 크기 3에 각 Card의 ShopCardItemView를 넣는다
3. Map Button에 맵 복귀 버튼을 넣는다
4. Card Price에서 장당 가격을 설정한다 기본값은 50이다
5. GameFlowController의 Shop Panel에 ShopPanelView를 연결한다
6. ShopPoint의 MapNodeView에서 Node Type을 Shop으로 설정한다
7. GameFlowController의 Nodes 배열에 ShopPoint를 실제 진행 순서로 넣는다
8. Button의 On Click은 코드가 구독하므로 같은 기능을 수동 등록하지 않는다

## 데이터 흐름

- GameSession이 기존 CoinModel과 OwnedDeckModel을 GameFlowController에 주입한다
- ShopPresenter는 지점 순번별 ShopModel을 보관한다
- 첫 진입 때 현재 캐릭터와 공용 카드에서 상태이상 카드를 제외하고 서로 다른 카드 최대 3장을 추첨한다
- 후보가 적으면 있는 상품만 표시한다 기본 카드도 후보에 포함한다
- 코인 부족과 품절 상품은 클릭 구매가 차단된다
- 코인이 부족해도 상품을 살펴보도록 호버 확대는 유지한다 품절 후에는 확대하지 않는다
- 구매 성공 시 코인을 차감하고 보유 덱에 카드를 추가한다 다음 전투부터 해당 카드가 포함된다
- 같은 상점 모델을 다시 열면 상품과 품절 상태가 유지된다 현재 맵 규칙상 완료 지점 재진입은 불가능하다
- 맵 복귀는 구매하지 않아도 가능하며 해당 지점을 완료한다
- 상점 상태는 현재 씬의 진행 동안만 유지하며 디스크 저장은 구현하지 않는다

## 확인 방법

1. Inspector 연결 후 플레이하여 순서대로 상점 지점에 진입한다
2. 카드 위로 마우스를 빠르게 왕복해도 확대가 누적되지 않는지 확인한다
3. 마우스를 올린 상태에서 상점을 닫고 다음 상점에서 카드가 기본 크기로 시작하는지 확인한다
4. 코인이 충분하면 구매 후 코인이 가격만큼 줄고 덱 목록에 한 장 추가되는지 확인한다
5. 구매한 상품은 품절로 표시되고 재구매되지 않는지 확인한다
6. 코인이 부족하면 코인과 덱이 변하지 않는지 확인한다
7. 구매 없이 MapButton을 눌러 맵과 다음 지점이 열리는지 확인한다

`pwsh -NoProfile -File Tests/ShopSmokeTests.ps1`은 최소 카드 데이터 대역을 사용해 실제 ShopModel과 CoinModel 및 OwnedDeckModel의 구매 규칙 17개를 검증한다
호버 애니메이션과 씬 연결은 플레이 모드에서 별도로 확인해야 한다
