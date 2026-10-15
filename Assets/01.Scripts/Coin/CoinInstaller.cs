using UnityEngine;

//공통 코인 데이터와 화면 연결
public class CoinInstaller : MonoBehaviour
{
    [SerializeField] private CoinView coinView;
    private CoinPresenter presenter;
    protected CoinModel Model { get; private set; }

    //세션 코인 화면 연결과 자식 기능 초기화
    public void Inject(CoinModel coinModel)
    {
        if (coinModel == null)
            throw new System.ArgumentNullException(nameof(coinModel));
        if (Model == coinModel) return;
        if (!ValidateBindings()) return;

        ReleaseBindings();
        Model = coinModel;
        presenter = new CoinPresenter(Model, coinView);
        BindAdditional();
    }

    //공통 코인 화면 연결 확인
    protected virtual bool ValidateBindings()
    {
        if (coinView != null && coinView.IsConfigured) return true;
        Debug.LogError("[코인] Coin View와 Coin Text를 연결해 주세요", this);
        return false;
    }

    //자식 설치자의 추가 기능 연결
    protected virtual void BindAdditional()
    {
    }

    //씬 제거 시 공통 화면과 자식 기능 연결 해제
    protected virtual void OnDestroy()
    {
        ReleaseBindings();
    }

    //코인 변경 구독과 모델 참조 해제
    protected virtual void ReleaseBindings()
    {
        presenter?.Dispose();
        presenter = null;
        Model = null;
    }
}
