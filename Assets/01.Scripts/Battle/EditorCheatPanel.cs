#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

//에디터 플레이 전용 통합 치트 표시와 입력 처리
public class EditorCheatPanel : MonoBehaviour
{
    private IGameDataProvider data;
    private BattleParticipants battle;
    private CardHandController hand;
    private CoinModel coins;
    private OwnedDeckModel ownedDeck;
    private Func<int, bool> grantRelic;
    private readonly List<UnityEngine.UI.GraphicRaycaster> blockedRaycasters = new List<UnityEngine.UI.GraphicRaycaster>();
    private bool open;
    private RectTransform positionTarget;
    private Canvas positionCanvas;
    private Vector2 scroll;
    private string damageInput = "10";
    private string coinInput = "100";
    private string cardInput = "10001";
    private string relicInput = "";
    private string discardInput = "1";
    private string message = "에디터 플레이 전용 치트";
    private bool ActiveBattle => battle != null && battle.IsBattleActive;

    //치트 버튼 위치 기준과 부모 캔버스 저장
    private void Awake()
    {
        positionTarget = transform as RectTransform;
        positionCanvas = GetComponentInParent<Canvas>();
    }

    //게임 데이터와 전투 및 세션 모델 주입
    public void Inject(IGameDataProvider provider, BattleParticipants participants,
        CardHandController handController, CoinModel coinModel, OwnedDeckModel deck)
    {
        data = provider;
        battle = participants;
        hand = handController;
        coins = coinModel;
        ownedDeck = deck;
    }

    //추후 유물 보유 시스템의 지급 처리 연결
    public void InjectRelicGrant(Func<int, bool> handler)
    {
        grantRelic = handler;
    }

    //치트 오브젝트 위치에 공통 버튼과 치트창 표시
    private void OnGUI()
    {
        if (!Application.isPlaying || coins == null)
            return;
        int oldDepth = GUI.depth;
        GUI.depth = -1000;
        Rect buttonRect = GetButtonRect();
        if (!open)
        {
            if (GUI.Button(buttonRect, "에디터 치트"))
                SetOpen(true);
        }
        else
        {
            GUILayout.BeginArea(GetPanelRect(buttonRect), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("에디터 전용 치트");
            if (GUILayout.Button("닫기", GUILayout.Width(70)))
                SetOpen(false);
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll);
            DrawHealth();
            DrawCards();
            DrawCoinsAndRelics();
            GUILayout.Label(message);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        GUI.depth = oldDepth;
    }

    //오브젝트 중심의 화면 좌표를 치트 버튼 영역으로 변환
    private Rect GetButtonRect()
    {
        Vector2 size = new Vector2(Mathf.Min(120f, Mathf.Max(1f, Screen.width - 8f)),
            Mathf.Min(30f, Mathf.Max(1f, Screen.height - 8f)));
        Vector2 topLeft = new Vector2(12f, 12f);
        if (positionTarget != null && positionCanvas != null)
        {
            Canvas root = positionCanvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            if (root.renderMode == RenderMode.ScreenSpaceOverlay || camera != null)
            {
                Vector3 center = positionTarget.TransformPoint(positionTarget.rect.center);
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, center);
                topLeft = new Vector2(screen.x - size.x * 0.5f,
                    Screen.height - screen.y - size.y * 0.5f);
            }
        }
        return ClampToScreen(new Rect(topLeft, size));
    }

    //버튼 아래의 치트창 위치와 화면 내 크기 계산
    private Rect GetPanelRect(Rect buttonRect)
    {
        float width = Mathf.Min(430f, Mathf.Max(1f, Screen.width - 8f));
        float height = Mathf.Min(680f, Mathf.Max(1f, Screen.height - 8f));
        return ClampToScreen(new Rect(buttonRect.x, buttonRect.yMax + 6f, width, height));
    }

    //버튼과 치트창의 화면 경계 이탈 방지
    private static Rect ClampToScreen(Rect rect)
    {
        rect.x = Mathf.Clamp(rect.x, 4f, Mathf.Max(4f, Screen.width - rect.width - 4f));
        rect.y = Mathf.Clamp(rect.y, 4f, Mathf.Max(4f, Screen.height - rect.height - 4f));
        return rect;
    }

    //전투 피해와 회복 및 기존 상태 테스트 버튼 표시
    private void DrawHealth()
    {
        GUILayout.Label("체력 / 전투");
        if (battle != null && battle.Player != null)
            GUILayout.Label($"플레이어 HP {battle.Player.Stats.CurrentHealth}/{battle.Player.Stats.MaxHealth}");
        if (ActiveBattle)
            GUILayout.Label($"적 HP {battle.Enemy.Stats.CurrentHealth}/{battle.Enemy.Stats.MaxHealth}");
        damageInput = InputRow("피해 / 회복량", damageInput);
        GUILayout.BeginHorizontal();
        ActionButton("적 피해", ActiveBattle, () => Damage(true, false));
        ActionButton("적 즉사", ActiveBattle, () => Damage(true, true));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        ActionButton("플레이어 피해", ActiveBattle, () => Damage(false, false));
        ActionButton("플레이어 즉사", ActiveBattle, () => Damage(false, true));
        GUILayout.EndHorizontal();
        bool alive = battle != null && battle.Player != null && battle.Player.Stats.IsAlive;
        GUILayout.BeginHorizontal();
        ActionButton("플레이어 회복", alive, () =>
        {
            if (PositiveNumber(damageInput, out int amount))
                message = $"체력 {battle.Player.Stats.Heal(amount)} 회복";
        });
        ActionButton("체력 전부 회복", alive, () => battle.Player.Stats.Heal(battle.Player.Stats.MaxHealth));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        ActionButton("방어 +5", ActiveBattle, () => battle.Player.Stats.GainBlock(5));
        ActionButton("에너지 -1", ActiveBattle, () => battle.Player.TrySpendEnergy(1));
        ActionButton("에너지 복원", ActiveBattle, () => battle.Player.ResetEnergy());
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        ActionButton("약화 +1", ActiveBattle, () => battle.Player.Stats.ApplyWeak(1));
        ActionButton("취약 +1", ActiveBattle, () => battle.Player.Stats.ApplyVulnerable(1));
        ActionButton("적 행동 재추첨", ActiveBattle, () => battle.Enemy.PlanNextAction());
        GUILayout.EndHorizontal();
        ActionButton("현재 전투 참가자 초기화", ActiveBattle, () =>
            battle.Initialize(battle.Player.Data.Id, battle.Enemy.Data.Id));
        GUILayout.Label("피해는 방어도 적용 / 즉사는 방어도 제거 후 처리");
    }

    //기존 손패 치트와 보유 덱 추가 입력 표시
    private void DrawCards()
    {
        GUILayout.Space(8);
        GUILayout.Label($"카드 / 보유 덱 {ownedDeck?.CardIds.Count ?? 0}장");
        cardInput = InputRow("카드 ID", cardInput);
        GUILayout.BeginHorizontal();
        ActionButton("보유 덱 추가", ownedDeck != null && ownedDeck.IsInitialized, AddOwnedCard);
        ActionButton("손패 추가", ActiveBattle && hand != null && hand.IsReady, () =>
        {
            if (PositiveNumber(cardInput, out int id))
                message = hand.TryAddCheatCard(id) ? "손패 추가 완료" : "카드 ID 또는 손패 빈자리 확인";
        });
        GUILayout.EndHorizontal();
        GUILayout.Label("보유 덱 추가는 다음 전투부터 적용 / 손패 추가는 현재 전투만 적용");
        bool ready = ActiveBattle && hand != null && hand.IsReady;
        GUILayout.BeginHorizontal();
        ActionButton("덱 초기화", ready, () => hand.ResetTestDeck());
        ActionButton("1장 드로우", ready, () => hand.DrawCards(1));
        ActionButton("전체 버리기", ready, () => hand.DiscardAll());
        GUILayout.EndHorizontal();
        discardInput = InputRow("버릴 손패 순번", discardInput);
        ActionButton("선택 순번 버리기", ready, () =>
        {
            if (PositiveNumber(discardInput, out int number))
            {
                if (number > hand.HandCount) message = "손패 순번 범위 초과";
                else hand.DiscardAt(number - 1);
            }
        });
    }

    //코인 지급과 추후 유물 지급 입력 표시
    private void DrawCoinsAndRelics()
    {
        GUILayout.Space(8);
        GUILayout.Label($"코인 / 현재 {coins.Amount}");
        coinInput = InputRow("지급량", coinInput);
        ActionButton("코인 지급", true, () =>
        {
            if (PositiveNumber(coinInput, out int amount))
                message = coins.TryAdd(amount) ? $"{amount} 코인 지급" : "최대 코인 보유량 초과";
        });
        GUILayout.Space(8);
        relicInput = InputRow("유물 ID", relicInput);
        ActionButton("유물 지급", grantRelic != null, () =>
        {
            if (PositiveNumber(relicInput, out int id))
                message = grantRelic(id) ? "유물 지급 완료" : "유물 지급 실패";
        });
        if (grantRelic == null)
            GUILayout.Label("유물 보유 시스템 연결 준비 중");
    }

    //유효한 카드 ID의 보유 덱 추가
    private void AddOwnedCard()
    {
        if (!PositiveNumber(cardInput, out int id))
            return;
        if (data == null || !data.IsReady || !data.TryGetCard(id, out CardData card))
        {
            message = "존재하지 않는 카드 ID";
            return;
        }
        ownedDeck.Add(id);
        message = $"{card.DisplayName} 보유 덱 추가 완료";
    }

    //현재 전투 대상의 피해 또는 확정 사망 처리
    private void Damage(bool enemy, bool kill)
    {
        if (!ActiveBattle)
            return;
        CombatantState target = enemy ? battle.Enemy.Stats : battle.Player.Stats;
        int amount;
        if (kill)
        {
            target.ClearBlock();
            amount = target.CurrentHealth;
        }
        else if (!PositiveNumber(damageInput, out amount))
            return;
        message = $"실제 체력 피해 {target.TakeDamage(amount)}";
    }

    //양의 정수 입력 검증과 오류 안내
    private bool PositiveNumber(string input, out int value)
    {
        if (int.TryParse(input, out value) && value > 0)
            return true;
        message = "1 이상의 정수를 입력해 주세요";
        return false;
    }

    //레이블과 숫자 입력 필드 표시
    private string InputRow(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(130));
        string result = GUILayout.TextField(value, 10);
        GUILayout.EndHorizontal();
        return result;
    }

    //사용 가능 상태에 따른 치트 버튼 표시
    private void ActionButton(string label, bool available, Action action)
    {
        bool previous = GUI.enabled;
        GUI.enabled = previous && available;
        if (GUILayout.Button(label, GUILayout.Height(26)))
            action();
        GUI.enabled = previous;
    }

    //치트창 표시 상태와 배경 UI 클릭 차단 전환
    private void SetOpen(bool value)
    {
        open = value;
        if (open)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                foreach (var raycaster in root.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true))
                    if (raycaster.enabled)
                    {
                        blockedRaycasters.Add(raycaster);
                        raycaster.enabled = false;
                    }
        }
        else
        {
            foreach (var raycaster in blockedRaycasters)
                if (raycaster != null)
                    raycaster.enabled = true;
            blockedRaycasters.Clear();
        }
    }

    //치트창 비활성화 시 배경 UI 입력 복원
    private void OnDisable()
    {
        SetOpen(false);
    }
}
#endif
