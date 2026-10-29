using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

//전투 편성 CSV 검증과 서브에셋 갱신
internal static partial class GameDatabaseCsvSync
{
    //자료형 행과 비활성 행 제외, 세 자리의 배치 순서 유지
    private static List<EncounterRow> ReadEncounters(CsvTable table)
    {
        var idColumn = ProjectIdHeader(table, "encounterId");
        table.Require(idColumn, "encounterName", "enemyId1", "enemyId2", "enemyId3", "enabled");
        var rows = new List<EncounterRow>();
        for (int row = 0; row < table.RowCount; row++)
        {
            if (!ShouldImportRow(table, row, idColumn)) continue;
            var ids = new int[3];
            for (int slot = 0; slot < ids.Length; slot++)
                ids[slot] = NonNegativeInt(table, row, $"enemyId{slot + 1}");
            if (ids.All(id => id == 0))
                throw Error(table, row, "전투 편성에는 적이 한 마리 이상 필요합니다.");
            rows.Add(new EncounterRow
            {
                Id = PositiveInt(table, row, idColumn),
                Name = Required(table, row, "encounterName"),
                EnemyIds = ids
            });
        }
        EnsureUnique(rows.Select(x => x.Id), "전투 편성 시트");
        return rows;
    }

    //활성 적 참조 확인, 0은 빈 자리로 허용
    private static void ValidateEncounterEnemies(IEnumerable<EncounterRow> rows, HashSet<int> enemyIds)
    {
        foreach (var row in rows)
            foreach (int enemyId in row.EnemyIds)
                if (enemyId != 0 && !enemyIds.Contains(enemyId))
                    throw new InvalidDataException($"전투 편성 '{row.Id}'가 존재하지 않는 적 '{enemyId}'를 참조합니다.");
    }

    //동일 ID의 SO 재사용과 편성 데이터 반영
    private static List<EncounterData> ApplyEncounterRows(GameDatabase database,
        List<EncounterRow> rows, IReadOnlyDictionary<int, GameDataEntry> existing)
    {
        return rows.Select(row =>
        {
            var asset = GetOrCreate<EncounterData>(database, existing, row.Id);
            asset.EditorSetIdentity(row.Id, row.Name);
            asset.EditorApply(row.EnemyIds);
            EditorUtility.SetDirty(asset);
            return asset;
        }).ToList();
    }

    //다른 데이터 변경 없이 편성 CSV만 검증 후 반영
    internal static void ImportEncounterCsv(GameDatabase database, string csv)
    {
        var rows = ReadEncounters(CsvTable.Parse("전투 편성 시트", csv));
        ValidateEncounterEnemies(rows, database.Enemies.Where(x => x != null).Select(x => x.Id).ToHashSet());
        string path = AssetDatabase.GetAssetPath(database);
        var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameDataEntry>().ToList();
        var existing = all.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First());
        foreach (var row in rows)
            if (all.Any(x => x.Id == row.Id && !(x is EncounterData)))
                throw new InvalidDataException($"전투 편성 ID '{row.Id}'가 다른 데이터 ID와 중복됩니다.");

        var encounters = ApplyEncounterRows(database, rows, existing);
        foreach (var old in all.OfType<EncounterData>().Where(x => !encounters.Contains(x)))
            UnityEngine.Object.DestroyImmediate(old, true);
        database.EditorSetEncounters(encounters);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    //검증 단계에서 사용하는 편성 행
    private class EncounterRow
    {
        public int Id;
        public string Name;
        public int[] EnemyIds;
    }
}
