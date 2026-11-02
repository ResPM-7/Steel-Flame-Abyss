using System;
using UnityEditor;
using UnityEngine;

//프로젝트 기존 IMGUI 검사기 방식으로 가중치 환산 확률 표시
[CustomEditor(typeof(RarityDropRates))]
public class RarityDropRatesEditor : Editor
{
    //설정값과 모든 등급 후보가 존재할 때의 기준 확률 표시
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var rates = (RarityDropRates)target;
        double total = 0;
        foreach (DataRarity rarity in Enum.GetValues(typeof(DataRarity))) total += rates.GetWeight(rarity);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("등급별 기준 확률", EditorStyles.boldLabel);
        foreach (DataRarity rarity in Enum.GetValues(typeof(DataRarity)))
            EditorGUILayout.LabelField(rarity.ToString(), total > 0 ? $"{rates.GetWeight(rarity) / total * 100:0.##}%" : "0%");
        EditorGUILayout.HelpBox(total <= 0 ? "모든 가중치가 0이면 상품·보상 후보가 생성되지 않습니다."
            : "후보가 없는 등급은 제외 후 재분배. 가중치 0은 등장 제외. 중복 없는 추첨으로 후보 소진 시 확률 변경.",
            total <= 0 ? MessageType.Warning : MessageType.Info);
    }
}
