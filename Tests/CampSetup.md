# 캠프와 공통 HP 연결

## UI 배치

Canvas_Dynamic 아래에 CampPanel과 CommonHUD를 배치
CommonHUD 아래 Health 오브젝트에 PlayerHealthView 추가
CommonHUD 자체가 아니라 Health만 GameFlowController의 Common Health View로 연결
코인처럼 전투에서도 보여야 하는 UI는 Health의 자식으로 배치하지 않음

## CampPanel

- CampPanel에 CampPanelView 추가
- 자식으로 상태 텍스트와 휴식 버튼 및 계속 버튼 구성
- Status Text와 Rest Button 및 Continue Button 연결
- Heal Percent 기본값 30은 최대 체력의 30퍼센트 회복이며 소수점은 올림
- 버튼 On Click은 코드에서 연결하므로 별도 등록 불필요
- GameFlowController의 Camp Panel에 CampPanelView 연결

## 공통 HP

- Health 오브젝트에 PlayerHealthView 추가
- Health Text에 체력 텍스트 연결
- Health Fill은 선택 항목이며 사용 시 Image Type을 Filled로 설정
- GameFlowController의 Common Health View에 PlayerHealthView 연결
- 맵과 이벤트 및 캠프에서 표시하고 전투와 승리 보상 화면에서 숨김
- 전투 패널의 기존 체력 표시는 변경하지 않음

## 캠프 지점

- 새 캠프 지점에 Image와 Button 및 MapNodeView 추가
- MapNodeView의 Button과 Icon 연결 후 Node Type을 Camp로 설정
- GameFlowController의 Nodes 배열에 원하는 진행 순서로 캠프 추가
- Camp 지점의 Enemy Id는 사용하지 않음
- 기존 Battle과 Event 및 Boss의 숫자 값은 유지

## 확인

1. 첫 전투 전 맵의 공통 HP가 최대 체력으로 표시되는지 확인
2. 전투에서 피해를 받은 후 승리하고 맵으로 복귀
3. 전투 중 공통 HP는 숨겨지고 맵에서 남은 체력 표시
4. 캠프에서 휴식 시 공통 HP와 캠프 결과에 같은 회복량 반영
5. 휴식 버튼 연속 클릭으로 추가 회복 불가
6. 계속 버튼으로 맵에 복귀하고 다음 지점 개방
7. 다음 전투에서 회복한 체력 유지와 방어도 및 버프 초기화 확인
8. 최대 체력에서도 휴식은 완료 처리되고 계속 진행 가능
9. 첫 지점을 캠프로 설정해도 정상 방문 가능

현재는 GameSession의 RunProgressModel.Health를 캠프와 공통 HP에 주입하는 방식
전투 종료 시 HP만 세션으로 복사하며 방어도와 일시 효과는 유지하지 않음
씬 이동 중 HP는 유지하지만 앱 종료 후 이어하기와 카드 강화 기능은 미구현
씬과 프리팹의 UI 생성 및 필드 연결은 별도로 필요
