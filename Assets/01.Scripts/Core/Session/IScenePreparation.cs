using System.Threading;
using Cysharp.Threading.Tasks;

//씬 활성화 이후 플레이 준비 완료 계약
public interface IScenePreparation
{
    UniTask PrepareSceneAsync(CancellationToken cancellationToken);
}
