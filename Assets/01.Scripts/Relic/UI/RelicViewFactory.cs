using System;
using System.Threading.Tasks;
using UnityEngine;

//유물 데이터 조회와 풀에서 빌린 표시 오브젝트의 설정 및 반환 담당
public class RelicViewFactory : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager pool;
    [SerializeField] private GameDataProvider data;
    [SerializeField] private string poolName = "RelicIcon";
    public string PoolName => poolName;
    public bool IsConfigured => pool != null && data != null && !string.IsNullOrWhiteSpace(poolName);

    //유물 데이터 조회
    public bool TryGetRelic(int id, out RelicData relic)
    {
        relic = null;
        return data != null && data.TryGetRelic(id, out relic);
    }

    //연결된 풀의 비동기 사전 준비
    public Task<bool> PrepareAsync() => IsConfigured
        ? pool.EnsurePoolReadyAsync(poolName) : Task.FromResult(false);

    //로드 완료된 풀에서 표시 객체를 빌려 부모와 유물 정보를 연결
    public RelicIconView Create(int relicId, Transform parent, RelicPanelView owner)
    {
        if (!IsConfigured || !TryGetRelic(relicId, out RelicData relic)) return null;
        GameObject instance = pool.GetObject(poolName);
        if (instance == null) return null;
        if (!instance.TryGetComponent(out RelicIconView view))
        {
            pool.ReturnObject(poolName, instance);
            throw new InvalidOperationException($"{poolName} 프리팹에 RelicIconView가 필요합니다");
        }
        instance.transform.SetParent(parent, false);
        instance.transform.SetAsLastSibling();
        view.Bind(relic, owner);
        return view;
    }

    //비활성화 콜백 중에도 안전하게 연결 해제 후 지연 반환
    public void Release(RelicIconView view)
    {
        if (view == null) return;
        view.Unbind();
        if (pool != null) pool.RequestReturn(poolName, view.gameObject);
    }
}
