using System;

public interface ICoinView
{
    //보유 코인 표시
    void ShowAmount(int amount);
}

//코인 변경 알림과 화면 표시 연결
public class CoinPresenter : IDisposable
{
    private readonly CoinModel model;
    private readonly ICoinView view;

    //코인 모델과 화면 연결 및 초기 보유량 표시
    public CoinPresenter(CoinModel model, ICoinView view)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        this.model = model;
        this.view = view;
        model.Changed += Refresh;
        Refresh(model.Amount);
    }

    //변경된 보유 코인을 화면에 전달
    private void Refresh(int amount)
    {
        view.ShowAmount(amount);
    }

    //화면 제거 시 코인 변경 구독 해제
    public void Dispose()
    {
        model.Changed -= Refresh;
    }
}
