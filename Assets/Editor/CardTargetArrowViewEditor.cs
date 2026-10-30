using UnityEditor;

//기본 Graphic 검사기 대신 곡선 설정을 포함한 전체 직렬화 필드 표시
[CustomEditor(typeof(CardTargetArrowView))]
[CanEditMultipleObjects]
public class CardTargetArrowViewEditor : Editor
{
}
