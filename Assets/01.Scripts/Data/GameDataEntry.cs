using UnityEngine;

namespace SteelFlameAbyss.Data
{
    /// <summary>모든 시트 기반 데이터가 공유하는 고유 ID와 표시 이름입니다.</summary>
    public abstract class GameDataEntry : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        public string Id => id;
        public string DisplayName => displayName;

#if UNITY_EDITOR
        /// <summary>시트에서 읽은 공통 식별 정보를 에디터 전용 동기화 과정에서 반영합니다.</summary>
        public void EditorSetIdentity(string newId, string newDisplayName)
        {
            id = newId;
            displayName = newDisplayName;
            name = $"{GetType().Name}_{newId}";
        }
#endif
    }
}
