# 맵과 전투 씬 연결

## 변경 스크립트

| 파일 | 역할 |
|---|---|
| GameSession.cs | 세션 데이터 소유와 씬별 DI 및 비동기 씬 이동 |
| RunProgressModel.cs | 체력과 맵 진행 및 선택 적과 복귀 경로 보관 |
| GameFlowController.cs | 전투 객체 없이 맵과 상점 및 캠프 진행 |
| BattleSceneController.cs | 전투 진입 정보 적용과 결과 저장 및 Next 처리 |
| BattleInstaller.cs | 전투 씬 내부 의존성 연결과 자동 시작 순서 통일 |
| BattleParticipants.cs | 새 전투에 세션의 현재 HP 적용 |
| PlayerHealthView.cs | 전투 참가자 대신 세션 체력 구독 |
| CoinInstaller.cs | 맵에서는 코인 표시만 연결하고 전투에서는 보상도 연결 |
| CardListPanelView.cs | 전투 덱 없이도 전체 보유 덱 목록 지원 |
| EditorCheatPanel.cs | 비전투 씬의 회복 치트를 세션 체력에 연결 |
| BattleSceneSmokeTests.cs | 씬 이동과 보상 및 패배와 새 게임 통합 검사 |
| SceneSplitMigration.cs | 기존 씬 구성의 일회 이전과 빌드 목록 저장 |

## 씬 역할

- TitleScene: 새 게임 시작과 종료
- GameScene: 맵과 상점 및 캠프와 이벤트 및 공통 HP와 코인 및 보유 덱
- BattleScene: 전투 참가자와 손패 및 턴과 승패 및 카드와 코인 보상
- GameSession: 자동 생성되는 DontDestroyOnLoad 객체 하나이며 코인과 보유 덱 및 RunProgressModel 소유
- RunProgressModel: 현재 HP와 캐릭터 ID 및 맵 진행과 선택 적 ID와 복귀 씬 및 스크롤 위치 보관
- UI와 전투 컴포넌트 및 오브젝트 풀은 씬 로컬이며 세션에 저장하지 않음

씬 이동은 LoadSceneAsync와 LoadSceneMode.Single 사용
전투 진입 시 GameScene을 해제하고 보상 종료 시 BattleScene을 해제
씬 전환마다 GC.Collect를 강제로 호출하지 않음
진행 정보는 실행 중에만 유지하며 앱 종료 후 이어하기는 미구현

## GameScene 연결

GameFlowController에 다음 항목 연결

- Data Provider: GameDataProvider
- Object Pool Manager: 해당 씬 ObjectPoolManager
- Battle Scene Path: Assets/00.Scenes/BattleScene.unity
- Map Panel과 Event Panel 및 Camp Panel과 Shop Panel: 해당 화면
- Common Health View: CommonHUD/Health
- Map Scroll과 Map Title: 맵 스크롤과 제목
- Nodes: 실제 진행 순서의 MapNodeView 배열
- Player Id: 기본 50001
- Card List Panel: Canvas_Popup의 CardListPanelView
- Starting Deck Ids: 기존 BattleDeckManager의 테스트 덱에서 이전한 시작 덱
  - 비우면 캐릭터 SO의 Starting Deck Ids 사용
  - 이미 진행 중이면 보유 덱을 유지하고 초기화하지 않음

MapServices의 CoinInstaller는 Coin View만 연결
맵에는 BattleInstaller와 BattleParticipants 및 BattleDeckManager가 필요하지 않음

## BattleScene 연결

- BattleInstaller: 전투 데이터 공급자와 덱 및 손패와 참가자 및 턴과 보상과 목록과 풀 연결
- BattleSceneController: Data Provider와 Participants 및 Deck Manager와 Next Button 연결
- Next Button: Canvas_Popup/WinPanel/NextButton
- 다음 버튼은 BattleSceneController가 런타임에 자동 구독하므로 On Click 수동 추가 불필요
- 이전 GameFlowController.ContinueAfterReward 연결은 제거됨
- CoinInstaller는 Coin View와 전투 참가자 및 코인 보상 버튼과 텍스트 연결 유지
- 카드 목록은 보유 덱과 뽑기 및 버리기 목록 모두 사용 가능
- 공통 HP HUD는 전투 씬에 없으며 전투 참가자 HP 화면만 사용

배틀 씬만 직접 재생하면 BattleSceneController의 Test Player Id와 Test Enemy Id로 시작
직접 실행 테스트에서 Next를 누르면 첫 맵 지점부터 시작하며 해당 테스트 전투는 지점 완료로 계산하지 않음

## 지점과 빌드 설정

Build Profiles의 Scene List에 TitleScene과 GameScene 및 BattleScene 등록
기존 타이틀 인덱스 0과 게임 인덱스 1을 유지하고 배틀은 마지막에 추가

지점 이름이 아닌 MapNodeView의 Node Type이 실제 행동을 결정
분리 작업 당시 저장된 설정은 다음과 같으며 사용자 설정을 그대로 보존

| 오브젝트 | 현재 Node Type | Enemy Id |
|---|---|---|
| EnemyPoint | Shop | 60001 |
| CampPoint | Shop | 60001 |
| ShopPoint | Camp | 60001 |
| BossPoint | Boss | 60002 |

전투→캠프→상점→보스로 진행하려면 각 Node Type을 Battle과 Camp 및 Shop과 Boss로 설정
Nodes 배열 순서는 변경하지 않아도 됨
지점 버튼은 코드에서 자동 연결
EventPanel 완료 버튼은 GameFlowController.CompleteEvent 연결

## 확인 순서

1. TitleScene에서 새 게임 시작
2. Battle 또는 Boss 타입 지점 선택 후 Hierarchy의 씬이 BattleScene인지 확인
3. 피해를 받은 후 승리하고 코인과 카드 보상 수령
4. Next로 GameScene 복귀 후 HP와 코인 및 덱 증가와 다음 지점 개방 확인
5. 캠프 회복과 상점 방문 후 다음 전투에서 HP와 보유 덱 유지 확인
6. 새 전투에서는 방어도와 힘 등 일시 효과 및 카드 더미가 초기화되는지 확인
7. 보스 완료 후 지도 완료 표시 확인
8. 패배 시 Next 진행 차단과 타이틀에서 새 게임 초기화 확인

미수령 보상이 있어도 Next로 진행 가능하며 떠난 전투의 보상은 복구하지 않음
맵과 배틀 씬을 동시에 열면 Global Light 2D 중복 경고가 발생할 수 있으므로 단일 씬으로 열어서 편집
공용 Canvas 프리팹 원본은 변경하지 않았으며 구조를 분리한 씬의 Canvas 인스턴스는 연결 해제됨

## 자동 검사

Tests/BattleSceneSmokeTests.cs는 Unity CLI run_script로 실행하는 플레이 모드 검사
Main: 신선한 GameScene 실행에서 전투와 보상 및 캠프와 상점과 보스 왕복
DefeatAndReset: GameScene 플레이 중 새 게임과 패배 및 타이틀 복귀 검사
DirectBattle: BattleScene 직접 플레이에서 승리 후 맵 진입 검사
지점 타입은 검사 중에만 임시로 변경하고 씬 파일에 저장하지 않음

Tests/SceneSplitMigration.cs는 최초 분리용 일회 스크립트이며 이미 분리된 씬에서는 재실행 차단
