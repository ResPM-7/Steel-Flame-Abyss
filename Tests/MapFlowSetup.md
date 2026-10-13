# 고정 맵 연결과 확인

## 구성 요소

- GameFlowController: 항상 활성화된 루트 GameFlowController 오브젝트에 추가
- MapProgressModel: 코드에서 생성하는 진행 데이터이므로 컴포넌트 추가 불필요
- MapNodeView: Content 아래 EnemyPoint와 EventPoint와 BossPoint 각각에 추가
- 기존 BattleInstaller의 Game Flow Controller에 루트 컨트롤러 연결
- BattleParticipants와 BattleTurnController의 자동 시작은 설치자가 맵 모드에서 해제

## 지점 설정

각 지점에는 Image와 Button이 필요하며 MapNodeView의 Button과 Icon을 연결
Button의 Target Graphic도 해당 Image로 연결

| 지점 | Node Type | Enemy Id |
|---|---|---|
| 맨 아래 EnemyPoint | Battle | 실제 일반 적 데이터 ID |
| 가운데 EventPoint | Event | 사용하지 않음 |
| 맨 위 BossPoint | Boss | 실제 보스 적 데이터 ID |

지점 Button의 On Click은 Bind에서 자동 연결하므로 별도 이벤트 등록 불필요
기본 Enemy Id 60001은 일반 적 예시이며 보스 전용 데이터가 없으면 같은 적으로 전투 가능

## GameFlowController 필드

| 필드 | 연결 대상 |
|---|---|
| Map Panel | Canvas_Dynamic/MapPanel |
| Battle Panel | Canvas_Dynamic/BattlePanel |
| Hand Root | 손패와 CardHoverLayer를 함께 포함하는 Canvas_Overlay |
| Event Panel | Canvas_Dynamic/EventPanel |
| Map Scroll | MapPanel/Scroll View의 ScrollRect |
| Map Title | 지도 제목 텍스트 |
| Nodes 0 | 맨 아래 EnemyPoint의 MapNodeView |
| Nodes 1 | 가운데 EventPoint의 MapNodeView |
| Nodes 2 | 맨 위 BossPoint의 MapNodeView |
| Player Id | 실제 캐릭터 ID 기본값 50001 |
| Card List Panel | 기존 Canvas_Popup의 CardListPanelView |

Nodes는 하이어라키 자식 순서가 아니라 위 배열 순서로 진행
현재는 고정 경로이며 랜덤 생성과 갈림길은 미구현
노드 좌표는 기존 수동 배치를 유지하며 코드가 이동하지 않음
맵 ScrollRect는 Vertical을 켜고 Horizontal을 끄며 Content 높이는 지도 길이에 맞게 설정
처음에는 맨 아래로 스크롤하고 복귀할 때는 기존 스크롤 위치 유지

## 버튼 연결

- WinPanel/NextButton의 On Click에 GameFlowController.ContinueAfterReward 연결
- 기존 Next 버튼의 씬 이동 등 다른 이벤트가 있으면 의도하지 않은 중복 실행 여부 확인
- EventPanel에 완료 버튼을 만들고 GameFlowController.CompleteEvent 연결
- 맵의 덱 확인 버튼은 기존 CardListPanelView.ShowOwnedDeck 유지
- 패배 화면은 기존 Title과 Exit 버튼 유지

이벤트는 화면 진입과 완료만 구현한 임시 단계이며 보상이나 선택지는 없음
Next는 미수령 보상이 있어도 진행하며 떠난 전투의 보상은 다시 수령할 수 없음
마지막 보스 보상에서 Next를 누르면 지도 완료 문구와 모두 완료된 지점 표시
별도의 엔딩 화면과 진행 저장 파일은 미구현

## 동작 확인

1. 플레이 시작 시 맵과 아래 지점 하나만 선택 가능
2. 일반 전투 진입 후 피해를 받고 승리
3. 코인과 카드 보상 수령 후 Next로 맵 복귀
4. 완료한 전투 지점은 재선택 불가이며 이벤트 지점만 선택 가능
5. 덱 확인에서 보상 카드 포함 여부 확인
6. 이벤트 진입 후 완료 버튼으로 보스 지점 개방
7. 보스 진입 시 이전 체력과 코인 및 보유 덱 유지 여부 확인
8. 보스의 손패와 방어도 및 버프는 새 전투 기준으로 초기화
9. 보스 승리 보상 후 Next에서 지도 완료 확인
10. 별도 실행에서 패배 시 Next와 턴 종료로 진행할 수 없는지 확인

## 검증 범위

MapProgressSmokeTests.ps1은 Unity 없이 진행 모델의 조건 22개 확인
실제 씬 연결과 플레이 모드 UI 동작은 Unity에서 별도 확인 필요
