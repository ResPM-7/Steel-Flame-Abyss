using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteelFlameAbyss.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SteelFlameAbyss.Editor.Data
{
    /// <summary>
    /// 공개 CSV 주소에서 모든 시트를 내려받아 검증한 뒤 GameDatabase 서브에셋을 갱신합니다.
    /// 일부 시트만 적용되는 상태를 막기 위해 네 시트 다운로드와 검증이 모두 끝난 후에만 에셋을 변경합니다.
    /// </summary>
    internal static class GameDatabaseCsvSync
    {
        internal const string DatabasePath = "Assets/03.Data/GameDatabase.asset";
        private static bool isSyncing;

        /// <summary>에디터 시작 시 자동 실행하지 않고 메뉴 또는 단축키로만 동기화합니다.</summary>
        [MenuItem("Tools/데이터/원격 시트 동기화", false, 100)]
        public static async void SyncFromMenu() => await SyncRemoteAsync(showDialogOnFailure: true);

        /// <summary>Unity 배치 실행에서 원격 시트로 데이터베이스를 복구하기 위한 진입점입니다.</summary>
        public static void RepairScriptLinksFromBatch()
        {
            var database = GetOrCreateDatabase();
            var urls = SheetConnectionSettings.GetDatabaseUrls();

            var cardsCsv = DownloadCsvBlocking("카드", urls.Cards);
            var charactersCsv = DownloadCsvBlocking("캐릭터", urls.Characters);
            var enemiesCsv = DownloadCsvBlocking("적", urls.Enemies);
            var relicsCsv = string.IsNullOrWhiteSpace(urls.Relics)
                ? null
                : DownloadCsvBlocking("유물", urls.Relics);

            var input = ReadAndValidateAllSheets(cardsCsv, charactersCsv, enemiesCsv, relicsCsv);
            Apply(database, input);
            Debug.Log($"[게임 데이터] 배치 복구 완료: 카드 {input.Cards.Count}, " +
                      $"캐릭터 {input.Characters.Count}, 적 {input.Enemies.Count}, 유물 {input.Relics.Count}");
        }

        [MenuItem("Tools/데이터/게임 데이터베이스 선택", false, 200)]
        private static void SelectDatabase()
        {
            var database = GetOrCreateDatabase();
            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);
        }

        private static async Task SyncRemoteAsync(bool showDialogOnFailure)
        {
            if (isSyncing)
            {
                if (showDialogOnFailure)
                    EditorUtility.DisplayDialog("원격 시트 동기화", "이미 시트를 동기화하고 있습니다.", "확인");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (showDialogOnFailure)
                    EditorUtility.DisplayDialog("원격 시트 동기화", "Unity 컴파일 또는 에셋 갱신이 끝난 뒤 다시 실행해 주세요.", "확인");
                return;
            }

            isSyncing = true;
            try
            {
                var database = GetOrCreateDatabase();
                var urls = SheetConnectionSettings.GetDatabaseUrls();

                EditorUtility.DisplayProgressBar("원격 시트 동기화", "CSV 주소에서 데이터를 받는 중...", 0.25f);

                // 다운로드는 동시에 시작하되, 에셋 수정은 모든 응답이 성공한 뒤에만 수행합니다.
                var downloadTasks = new List<Task<string>>
                {
                    DownloadCsvAsync("카드", urls.Cards),
                    DownloadCsvAsync("캐릭터", urls.Characters),
                    DownloadCsvAsync("적", urls.Enemies)
                };
                var hasRelicSheet = !string.IsNullOrWhiteSpace(urls.Relics);
                if (hasRelicSheet)
                    downloadTasks.Add(DownloadCsvAsync("유물", urls.Relics));

                var downloads = await Task.WhenAll(downloadTasks);

                EditorUtility.DisplayProgressBar("원격 시트 동기화", "CSV 형식과 데이터 참조를 검증하는 중...", 0.65f);
                var input = ReadAndValidateAllSheets(downloads[0], downloads[1], downloads[2],
                    hasRelicSheet ? downloads[3] : null);

                EditorUtility.DisplayProgressBar("원격 시트 동기화", "서브에셋을 갱신하는 중...", 0.9f);
                Apply(database, input);
                Debug.Log($"[게임 데이터] 원격 시트 동기화 완료: 카드 {input.Cards.Count}, " +
                          $"캐릭터 {input.Characters.Count}, 적 {input.Enemies.Count}, 유물 {input.Relics.Count}");

                if (showDialogOnFailure)
                    EditorUtility.DisplayDialog("원격 시트 동기화 완료",
                        $"카드 {input.Cards.Count}개 / 캐릭터 {input.Characters.Count}개 / " +
                        $"적 {input.Enemies.Count}개 / 유물 {input.Relics.Count}개를 반영했습니다.", "확인");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[게임 데이터] 원격 시트 동기화 중단. 기존 데이터베이스는 유지됩니다.\n{exception.Message}\n{exception}");
                if (showDialogOnFailure)
                    EditorUtility.DisplayDialog("원격 시트 동기화 실패",
                        exception.Message + "\n\n기존 데이터베이스는 변경되지 않았습니다.", "확인");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isSyncing = false;
            }
        }

        private static ImportData ReadAndValidateAllSheets(string cardsCsv, string charactersCsv,
            string enemiesCsv, string relicsCsv)
        {
            var input = new ImportData
            {
                Cards = ReadCards(CsvTable.Parse("카드 시트", cardsCsv)),
                Characters = ReadCharacters(CsvTable.Parse("캐릭터 시트", charactersCsv)),
                Enemies = ReadEnemies(CsvTable.Parse("적 시트", enemiesCsv)),
                // 유물은 MVP 후순위이므로 URL이 없으면 빈 목록으로 정상 처리합니다.
                Relics = string.IsNullOrWhiteSpace(relicsCsv)
                    ? new List<RelicRow>()
                    : ReadRelics(CsvTable.Parse("유물 시트", relicsCsv))
            };

            var allIds = new HashSet<int>();
            foreach (var id in input.Cards.Select(x => x.Id)
                         .Concat(input.Characters.Select(x => x.Id))
                         .Concat(input.Enemies.Select(x => x.Id))
                         .Concat(input.Relics.Select(x => x.Id)))
            {
                if (!allIds.Add(id))
                    throw new InvalidDataException($"모든 시트를 통틀어 ID '{id}'가 중복됩니다.");
            }

            var cardIds = input.Cards.Select(x => x.Id).ToHashSet();
            foreach (var character in input.Characters)
            {
                if (character.DeriveCardLists)
                {
                    character.StartingDeck = input.Cards
                        .Where(card => card.Owner == character.Class && card.Rarity == DataRarity.Starter)
                        .Select(card => card.Id)
                        .ToList();
                    character.CardPool = input.Cards
                        .Where(card => card.Owner == character.Class || card.Owner == CharacterClass.Common)
                        .Select(card => card.Id)
                        .ToList();
                }

                foreach (var cardId in character.StartingDeck.Concat(character.CardPool))
                {
                    if (!cardIds.Contains(cardId))
                        throw new InvalidDataException($"캐릭터 '{character.Id}'가 존재하지 않는 카드 '{cardId}'를 참조합니다.");
                }
            }
            return input;
        }

        private static async Task<string> DownloadCsvAsync(string sheetName, string url)
        {
            using var request = UnityWebRequest.Get(url.Trim());
            request.timeout = 30;

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidDataException($"{sheetName} 시트를 받을 수 없습니다. ({request.responseCode}) {request.error}");

            var text = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidDataException($"{sheetName} 시트 응답이 비어 있습니다.");

            // 공유 권한이 없으면 CSV 대신 로그인/오류 HTML이 내려오는 경우가 많습니다.
            var trimmed = text.TrimStart();
            if (trimmed.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"{sheetName} 주소가 CSV 대신 HTML을 반환했습니다. 공개 CSV 주소와 공유 권한을 확인해 주세요.");

            return text;
        }

        /// <summary>배치 모드에서도 원격 CSV를 받을 수 있도록 완료까지 기다리는 다운로드입니다.</summary>
        private static string DownloadCsvBlocking(string sheetName, string url)
        {
            using var request = UnityWebRequest.Get(url.Trim());
            request.timeout = 30;
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                Thread.Sleep(10);

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidDataException($"{sheetName} 시트를 받을 수 없습니다. ({request.responseCode}) {request.error}");

            var text = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidDataException($"{sheetName} 시트 응답이 비어 있습니다.");

            var trimmed = text.TrimStart();
            if (trimmed.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"{sheetName} 주소가 CSV 대신 HTML을 반환했습니다. 공개 CSV 주소와 공유 권한을 확인해 주세요.");
            return text;
        }

        private static List<CardRow> ReadCards(CsvTable table)
        {
            // 기존 Total Card 탭은 기획자가 쓰던 열 이름을 유지하고 Unity 형식으로 변환합니다.
            if (table.Has("cardId", "cardName", "owner", "cardType", "rarity", "cost", "target",
                    "effect1", "value1", "effect2", "value2"))
                return ReadTotalCardRows(table);

            table.Require("Id", "Name", "Character", "Type", "Rarity", "Cost", "Description", "UpgradedDescription", "Effects", "UpgradedEffects", "Keywords");
            var result = new List<CardRow>();
            for (var row = 0; row < table.RowCount; row++)
            {
                var id = PositiveInt(table, row, "Id");
                result.Add(new CardRow
                {
                    Id = id,
                    Name = Required(table, row, "Name"),
                    Owner = EnumValue<CharacterClass>(table, row, "Character"),
                    Type = EnumValue<CardType>(table, row, "Type"),
                    Rarity = EnumValue<DataRarity>(table, row, "Rarity"),
                    Cost = NonNegativeInt(table, row, "Cost"),
                    Description = table.Get(row, "Description"),
                    UpgradedDescription = table.Get(row, "UpgradedDescription"),
                    Effects = Effects(table, row, "Effects"),
                    UpgradedEffects = Effects(table, row, "UpgradedEffects"),
                    Keywords = List(table.Get(row, "Keywords"))
                });
            }
            EnsureUnique(result.Select(x => x.Id), "카드 시트");
            return result;
        }

        private static List<CardRow> ReadTotalCardRows(CsvTable table)
        {
            var result = new List<CardRow>();
            for (var row = 0; row < table.RowCount; row++)
            {
                // Total Card의 두 번째 행은 string/int/enum 같은 자료형 설명 행입니다.
                if (!ShouldImportRow(table, row, "cardId"))
                    continue;

                var targetText = Required(table, row, "target");
                var target = string.Equals(targetText, "None", StringComparison.OrdinalIgnoreCase)
                    ? TargetType.Self
                    : EnumFromText<TargetType>(targetText, table, row, "target");
                var effects = new List<EffectSpec>();
                AddTotalCardEffect(effects, table, row, "effect1", "value1", target);
                AddTotalCardEffect(effects, table, row, "effect2", "value2", target);

                result.Add(new CardRow
                {
                    Id = PositiveInt(table, row, "cardId"),
                    Name = Required(table, row, "cardName"),
                    Owner = ParseProjectOwner(Required(table, row, "owner"), table, row),
                    Type = EnumValue<CardType>(table, row, "cardType"),
                    Rarity = EnumValue<DataRarity>(table, row, "rarity"),
                    Cost = NonNegativeInt(table, row, "cost"),
                    Description = table.Has("description") ? table.Get(row, "description") : string.Empty,
                    UpgradedDescription = string.Empty,
                    Effects = effects,
                    UpgradedEffects = new List<EffectSpec>(),
                    Keywords = new List<string>()
                });
            }
            EnsureUnique(result.Select(x => x.Id), "Total Card 시트");
            return result;
        }

        private static void AddTotalCardEffect(List<EffectSpec> effects, CsvTable table, int row,
            string effectColumn, string valueColumn, TargetType target)
        {
            var raw = Required(table, row, effectColumn);
            if (string.Equals(raw, "None", StringComparison.OrdinalIgnoreCase))
                return;

            if (!TryMapProjectEffect(raw, out var effectType))
                throw Error(table, row, $"{effectColumn}의 '{raw}' 효과를 Unity EffectType으로 변환할 수 없습니다.");

            effects.Add(new EffectSpec(effectType, target, NonNegativeInt(table, row, valueColumn)));
        }

        private static bool TryMapProjectEffect(string raw, out EffectType type)
        {
            var aliases = new Dictionary<string, EffectType>(StringComparer.OrdinalIgnoreCase)
            {
                ["Damage"] = EffectType.Damage,
                ["Block"] = EffectType.Block,
                ["Draw"] = EffectType.Draw,
                ["GainEnergy"] = EffectType.GainEnergy,
                ["Exhaust"] = EffectType.Exhaust,
                ["GainStrength"] = EffectType.Strength,
                ["GainRage"] = EffectType.Rage,
                ["ApplyBurn"] = EffectType.Burn,
                ["Ignite"] = EffectType.Ignite,
                ["GainOverheat"] = EffectType.Overheat,
                ["ApplyMentalSplit"] = EffectType.MindFracture,
                ["MultiplyMentalSplit"] = EffectType.AmplifyMindFracture,
                ["ApplyHallucination"] = EffectType.NextMindFractureBonus
            };
            return Enum.TryParse(raw.Trim(), true, out type) || aliases.TryGetValue(raw.Trim(), out type);
        }

        private static CharacterClass ParseProjectOwner(string raw, CsvTable table, int row)
        {
            return raw.Trim().ToLowerInvariant() switch
            {
                "steel" => CharacterClass.Warrior,
                "flame" => CharacterClass.FireMage,
                "abyss" => CharacterClass.Warlock,
                "public" => CharacterClass.Common,
                _ => throw Error(table, row, $"owner의 '{raw}' 값을 캐릭터 클래스로 변환할 수 없습니다.")
            };
        }

        private static List<CharacterRow> ReadCharacters(CsvTable table)
        {
            if ((table.Has("Id") || table.Has("characterId")) &&
                table.Has("characterName", "owner", "maxHealth", "resourceName", "startingEnergy", "enabled"))
                return ReadProjectCharacters(table);

            table.Require("Id", "Name", "Class", "MaxHealth", "StartingDeck", "CardPool", "ResourceName");
            var result = new List<CharacterRow>();
            for (var row = 0; row < table.RowCount; row++)
            {
                if (!ShouldImportRow(table, row, "Id"))
                    continue;
                result.Add(new CharacterRow
                {
                    Id = PositiveInt(table, row, "Id"),
                    Name = Required(table, row, "Name"),
                    Class = EnumValue<CharacterClass>(table, row, "Class"),
                    MaxHealth = PositiveInt(table, row, "MaxHealth"),
                    StartingEnergy = 3,
                    StartingDeck = IdList(table, row, "StartingDeck"),
                    CardPool = IdList(table, row, "CardPool"),
                    ResourceName = table.Get(row, "ResourceName")
                });
            }
            EnsureUnique(result.Select(x => x.Id), "캐릭터 시트");
            return result;
        }

        private static List<CharacterRow> ReadProjectCharacters(CsvTable table)
        {
            var idHeader = ProjectIdHeader(table, "characterId");
            var result = new List<CharacterRow>();
            for (var row = 0; row < table.RowCount; row++)
            {
                if (!ShouldImportRow(table, row, idHeader))
                    continue;

                result.Add(new CharacterRow
                {
                    Id = PositiveInt(table, row, idHeader),
                    Name = Required(table, row, "characterName"),
                    Class = ParseProjectOwner(Required(table, row, "owner"), table, row),
                    MaxHealth = PositiveInt(table, row, "maxHealth"),
                    StartingEnergy = NonNegativeInt(table, row, "startingEnergy"),
                    StartingDeck = new List<int>(),
                    CardPool = new List<int>(),
                    ResourceName = table.Get(row, "resourceName"),
                    DeriveCardLists = true
                });
            }
            EnsureUnique(result.Select(x => x.Id), "캐릭터 시트");
            return result;
        }

        private static List<EnemyRow> ReadEnemies(CsvTable table)
        {
            var projectFormat = (table.Has("Id") || table.Has("enemyId")) &&
                table.Has("enemyName", "tier", "minHealth", "maxHealth", "minGold", "maxGold",
                    "action1Name", "action1Weight", "action1Effect", "action1Target", "action1Value1",
                    "action1Value2", "action1Value3", "enabled");
            if (projectFormat)
                return ReadProjectEnemies(table);

            table.Require("Id", "Name", "Tier", "MinHealth", "MaxHealth", "MinGold", "MaxGold", "Actions");
            var result = new List<EnemyRow>();
            for (var row = 0; row < table.RowCount; row++)
            {
                if (!ShouldImportRow(table, row, "Id"))
                    continue;
                var minHealth = PositiveInt(table, row, "MinHealth");
                var maxHealth = PositiveInt(table, row, "MaxHealth");
                var minGold = NonNegativeInt(table, row, "MinGold");
                var maxGold = NonNegativeInt(table, row, "MaxGold");
                if (maxHealth < minHealth || maxGold < minGold)
                    throw Error(table, row, "Max values must be greater than or equal to min values.");

                result.Add(new EnemyRow
                {
                    Id = PositiveInt(table, row, "Id"),
                    Name = Required(table, row, "Name"),
                    Tier = EnumValue<EnemyTier>(table, row, "Tier"),
                    MinHealth = minHealth,
                    MaxHealth = maxHealth,
                    MinGold = minGold,
                    MaxGold = maxGold,
                    Actions = Actions(table, row)
                });
            }
            EnsureUnique(result.Select(x => x.Id), "적 시트");
            return result;
        }

        private static List<EnemyRow> ReadProjectEnemies(CsvTable table)
        {
            var idHeader = ProjectIdHeader(table, "enemyId");
            var result = new List<EnemyRow>();
            for (var row = 0; row < table.RowCount; row++)
            {
                if (!ShouldImportRow(table, row, idHeader))
                    continue;

                var minHealth = PositiveInt(table, row, "minHealth");
                var maxHealth = PositiveInt(table, row, "maxHealth");
                var minGold = NonNegativeInt(table, row, "minGold");
                var maxGold = NonNegativeInt(table, row, "maxGold");
                if (maxHealth < minHealth || maxGold < minGold)
                    throw Error(table, row, "최댓값은 최솟값보다 크거나 같아야 합니다.");

                result.Add(new EnemyRow
                {
                    Id = PositiveInt(table, row, idHeader),
                    Name = Required(table, row, "enemyName"),
                    Tier = EnumValue<EnemyTier>(table, row, "tier"),
                    MinHealth = minHealth,
                    MaxHealth = maxHealth,
                    MinGold = minGold,
                    MaxGold = maxGold,
                    Actions = ProjectEnemyActions(table, row)
                });
            }
            EnsureUnique(result.Select(x => x.Id), "적 시트");
            return result;
        }

        private static List<RelicRow> ReadRelics(CsvTable table)
        {
            if ((table.Has("Id") || table.Has("relicId")) &&
                table.Has("relicName", "rarity", "description", "trigger", "effect1", "target1",
                    "value1", "effect2", "target2", "value2", "enabled"))
                return ReadProjectRelics(table);

            table.Require("Id", "Name", "Rarity", "Description", "Trigger", "Effects");
            var result = new List<RelicRow>();
            for (var row = 0; row < table.RowCount; row++)
            {
                if (!ShouldImportRow(table, row, "Id"))
                    continue;
                result.Add(new RelicRow
                {
                    Id = PositiveInt(table, row, "Id"),
                    Name = Required(table, row, "Name"),
                    Rarity = EnumValue<DataRarity>(table, row, "Rarity"),
                    Description = table.Get(row, "Description"),
                    Trigger = EnumValue<RelicTrigger>(table, row, "Trigger"),
                    Effects = Effects(table, row, "Effects")
                });
            }
            EnsureUnique(result.Select(x => x.Id), "유물 시트");
            return result;
        }

        private static List<RelicRow> ReadProjectRelics(CsvTable table)
        {
            var idHeader = ProjectIdHeader(table, "relicId");
            var result = new List<RelicRow>();
            for (var row = 0; row < table.RowCount; row++)
            {
                if (!ShouldImportRow(table, row, idHeader))
                    continue;

                var effects = new List<EffectSpec>();
                AddProjectEffect(effects, table, row, "effect1", "target1", "value1");
                AddProjectEffect(effects, table, row, "effect2", "target2", "value2");
                result.Add(new RelicRow
                {
                    Id = PositiveInt(table, row, idHeader),
                    Name = Required(table, row, "relicName"),
                    Rarity = EnumValue<DataRarity>(table, row, "rarity"),
                    Description = table.Get(row, "description"),
                    Trigger = EnumValue<RelicTrigger>(table, row, "trigger"),
                    Effects = effects
                });
            }
            EnsureUnique(result.Select(x => x.Id), "유물 시트");
            return result;
        }

        private static string ProjectIdHeader(CsvTable table, string specificHeader) =>
            table.Has(specificHeader) ? specificHeader : "Id";

        private static void Apply(GameDatabase database, ImportData input)
        {
            // Script 연결이 완전히 끊긴 서브에셋은 Unity API에서도 null이므로,
            // 다운로드와 검증이 성공한 이 시점에만 루트 DB를 재생성하고 시트에서 다시 만듭니다.
            database = RecreateDatabaseIfMissingScripts(database);

            // 기존 ID와 같은 서브에셋은 재사용하므로 Sprite 등 시트 밖에서 지정한 참조가 유지됩니다.
            var allExisting = AssetDatabase.LoadAllAssetsAtPath(DatabasePath)
                .OfType<GameDataEntry>()
                .ToList();
            var existing = allExisting
                .Where(entry => entry.Id > 0)
                .GroupBy(entry => entry.Id)
                .ToDictionary(group => group.Key, group => group.First());
            var retained = new HashSet<GameDataEntry>();
            var cards = input.Cards.Select(row =>
            {
                var asset = GetOrCreate<CardData>(database, existing, row.Id);
                asset.EditorSetIdentity(row.Id, row.Name);
                asset.EditorApply(row.Owner, row.Type, row.Rarity, row.Cost, row.Description,
                    row.UpgradedDescription, row.Effects, row.UpgradedEffects, row.Keywords);
                retained.Add(asset);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();
            var characters = input.Characters.Select(row =>
            {
                var asset = GetOrCreate<CharacterData>(database, existing, row.Id);
                asset.EditorSetIdentity(row.Id, row.Name);
                asset.EditorApply(row.Class, row.MaxHealth, row.StartingEnergy, row.StartingDeck,
                    row.CardPool, row.ResourceName);
                retained.Add(asset);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();
            var enemies = input.Enemies.Select(row =>
            {
                var asset = GetOrCreate<EnemyData>(database, existing, row.Id);
                asset.EditorSetIdentity(row.Id, row.Name);
                asset.EditorApply(row.Tier, row.MinHealth, row.MaxHealth, row.MinGold, row.MaxGold, row.Actions);
                retained.Add(asset);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();
            var relics = input.Relics.Select(row =>
            {
                var asset = GetOrCreate<RelicData>(database, existing, row.Id);
                asset.EditorSetIdentity(row.Id, row.Name);
                asset.EditorApply(row.Rarity, row.Description, row.Trigger, row.Effects);
                retained.Add(asset);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            // 시트에서 제거된 행, ID가 0으로 깨진 항목, 중복 ID 서브에셋까지 모두 정리합니다.
            foreach (var orphan in allExisting.Where(entry => !retained.Contains(entry)).ToArray())
                UnityEngine.Object.DestroyImmediate(orphan, true);

            database.EditorSetEntries(cards, characters, enemies, relics);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(DatabasePath, ImportAssetOptions.ForceUpdate);
        }

        private static GameDatabase RecreateDatabaseIfMissingScripts(GameDatabase database)
        {
            if (!File.Exists(DatabasePath) ||
                !File.ReadAllText(DatabasePath).Contains("m_Script: {fileID: 0}"))
                return database;

            // 끊긴 객체는 Unity API에서 실제 null이므로 에셋 전체를 재생성해야 제거할 수 있습니다.
            AssetDatabase.DeleteAsset(DatabasePath);
            database = ScriptableObject.CreateInstance<GameDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            Debug.Log("[게임 데이터] Script 연결이 끊긴 기존 데이터베이스를 원격 시트 기준으로 재생성합니다.");
            return database;
        }

        private static GameDatabase GetOrCreateDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database != null)
                return database;

            EnsureFolder("Assets/03.Data");
            database = ScriptableObject.CreateInstance<GameDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            AssetDatabase.SaveAssets();
            return database;
        }

        private static T GetOrCreate<T>(GameDatabase database, IReadOnlyDictionary<int, GameDataEntry> existing,
            int id) where T : GameDataEntry
        {
            if (existing.TryGetValue(id, out var current))
            {
                if (current is T typed)
                    return typed;
                throw new InvalidDataException($"ID '{id}'의 데이터 종류가 {current.GetType().Name}에서 {typeof(T).Name}(으)로 바뀌었습니다. 먼저 새 ID를 사용해 주세요.");
            }

            var created = ScriptableObject.CreateInstance<T>();
            created.name = $"{typeof(T).Name}_{id}";
            AssetDatabase.AddObjectToAsset(created, database);
            return created;
        }

        private static List<EnemyActionSpec> Actions(CsvTable table, int row)
        {
            var raw = table.Get(row, "Actions");
            var result = new List<EnemyActionSpec>();
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            foreach (var actionText in raw.Split('|'))
            {
                var parts = actionText.Split('~');
                if (parts.Length != 3 || !Enum.TryParse(parts[0].Trim(), true, out IntentType intent) ||
                    !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) || weight < 0f)
                    throw Error(table, row, $"행동 '{actionText}' 형식이 잘못되었습니다. Intent~Weight~Effect[&Effect] 형식을 사용하세요.");

                var effects = ParseEffects(parts[2], '&', table, row, "Actions");
                result.Add(new EnemyActionSpec(intent, weight, effects));
            }
            return result;
        }

        private static List<EnemyActionSpec> ProjectEnemyActions(CsvTable table, int row)
        {
            var result = new List<EnemyActionSpec>();
            for (var slot = 1; slot <= 3; slot++)
            {
                var prefix = $"action{slot}";
                var actionName = table.Get(row, prefix + "Name");
                var effectName = table.Get(row, prefix + "Effect");
                if (string.IsNullOrWhiteSpace(actionName) && string.IsNullOrWhiteSpace(effectName))
                    continue;
                if (string.IsNullOrWhiteSpace(actionName) || string.IsNullOrWhiteSpace(effectName))
                    throw Error(table, row, $"{prefix}의 이름과 효과를 모두 입력해 주세요.");
                if (string.Equals(effectName, "None", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!TryMapProjectEffect(effectName, out var effectType))
                    throw Error(table, row, $"{prefix}Effect의 '{effectName}' 효과를 Unity EffectType으로 변환할 수 없습니다.");

                var target = EnumFromText<TargetType>(Required(table, row, prefix + "Target"),
                    table, row, prefix + "Target");
                var effect = new EffectSpec(effectType, target,
                    NonNegativeInt(table, row, prefix + "Value1"),
                    NonNegativeInt(table, row, prefix + "Value2"),
                    NonNegativeInt(table, row, prefix + "Value3"));
                var intent = InferIntent(actionName, effectType);
                result.Add(new EnemyActionSpec(actionName, intent,
                    NonNegativeFloat(table, row, prefix + "Weight"), new List<EffectSpec> { effect }));
            }
            return result;
        }

        private static IntentType InferIntent(string actionName, EffectType effectType)
        {
            if (Enum.TryParse(actionName.Trim(), true, out IntentType intent))
                return intent;

            return effectType switch
            {
                EffectType.Damage => IntentType.Attack,
                EffectType.Block => IntentType.Defend,
                EffectType.Strength or EffectType.Rage or EffectType.Overheat => IntentType.Buff,
                EffectType.Weak or EffectType.Vulnerable or EffectType.Burn or EffectType.MindFracture => IntentType.Debuff,
                _ => IntentType.Special
            };
        }

        private static void AddProjectEffect(List<EffectSpec> effects, CsvTable table, int row,
            string effectColumn, string targetColumn, string valueColumn)
        {
            var effectName = table.Get(row, effectColumn);
            if (string.IsNullOrWhiteSpace(effectName) ||
                string.Equals(effectName, "None", StringComparison.OrdinalIgnoreCase))
                return;
            if (!TryMapProjectEffect(effectName, out var effectType))
                throw Error(table, row, $"{effectColumn}의 '{effectName}' 효과를 Unity EffectType으로 변환할 수 없습니다.");

            var target = EnumFromText<TargetType>(Required(table, row, targetColumn), table, row, targetColumn);
            effects.Add(new EffectSpec(effectType, target, NonNegativeInt(table, row, valueColumn)));
        }

        private static List<EffectSpec> Effects(CsvTable table, int row, string column) =>
            ParseEffects(table.Get(row, column), '|', table, row, column);

        private static List<EffectSpec> ParseEffects(string raw, char separator, CsvTable table, int row, string column)
        {
            var result = new List<EffectSpec>();
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            foreach (var effectText in raw.Split(separator))
            {
                var parts = effectText.Split(':');
                if (parts.Length < 3 || parts.Length > 5 ||
                    !Enum.TryParse(parts[0].Trim(), true, out EffectType type) ||
                    !Enum.TryParse(parts[1].Trim(), true, out TargetType target) ||
                    !int.TryParse(parts[2].Trim(), out var amount))
                    throw Error(table, row, $"{column} 효과 '{effectText}' 형식이 잘못되었습니다. Type:Target:Amount[:SecondaryAmount:Duration] 형식을 사용하세요.");

                var secondary = parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]) ? ParseInt(parts[3], table, row, column) : 0;
                var duration = parts.Length >= 5 && !string.IsNullOrWhiteSpace(parts[4]) ? ParseInt(parts[4], table, row, column) : 0;
                result.Add(new EffectSpec(type, target, amount, secondary, duration));
            }
            return result;
        }

        private static T EnumValue<T>(CsvTable table, int row, string column) where T : struct
        {
            var raw = Required(table, row, column);
            return EnumFromText<T>(raw, table, row, column);
        }

        private static T EnumFromText<T>(string raw, CsvTable table, int row, string column) where T : struct
        {
            if (!Enum.TryParse(raw, true, out T result))
                throw Error(table, row, $"{column}의 '{raw}' 값은 올바른 {typeof(T).Name} 값이 아닙니다.");
            return result;
        }

        private static int PositiveInt(CsvTable table, int row, string column)
        {
            var value = ParseInt(Required(table, row, column), table, row, column);
            if (value <= 0)
                throw Error(table, row, $"{column} 값은 0보다 커야 합니다.");
            return value;
        }

        private static int NonNegativeInt(CsvTable table, int row, string column)
        {
            var value = ParseInt(Required(table, row, column), table, row, column);
            if (value < 0)
                throw Error(table, row, $"{column} 값은 음수일 수 없습니다.");
            return value;
        }

        private static float NonNegativeFloat(CsvTable table, int row, string column)
        {
            var raw = Required(table, row, column);
            if (!float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                value < 0f)
                throw Error(table, row, $"{column}의 '{raw}' 값은 0 이상의 숫자여야 합니다.");
            return value;
        }

        private static bool ShouldImportRow(CsvTable table, int row, string idColumn)
        {
            if (table.IsTypeDeclarationRow(row) || string.IsNullOrWhiteSpace(table.Get(row, idColumn)))
                return false;
            if (!table.Has("enabled"))
                return true;

            var raw = table.Get(row, "enabled");
            return bool.TryParse(raw, out var enabled)
                ? enabled
                : raw == "1" || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseInt(string raw, CsvTable table, int row, string column)
        {
            if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                throw Error(table, row, $"{column}의 '{raw}' 값은 올바른 정수가 아닙니다.");
            return value;
        }

        private static string Required(CsvTable table, int row, string column)
        {
            var value = table.Get(row, column);
            if (string.IsNullOrWhiteSpace(value))
                throw Error(table, row, $"{column} 값은 필수입니다.");
            return value;
        }

        private static List<string> List(string raw) => string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw.Split('|').Select(value => value.Trim()).Where(value => value.Length > 0).ToList();

        private static List<int> IdList(CsvTable table, int row, string column)
        {
            var raw = table.Get(row, column);
            if (string.IsNullOrWhiteSpace(raw))
                return new List<int>();

            var result = new List<int>();
            foreach (var value in raw.Split('|').Select(value => value.Trim()).Where(value => value.Length > 0))
            {
                var id = ParseInt(value, table, row, column);
                if (id <= 0)
                    throw Error(table, row, $"{column}의 카드 ID '{value}'는 0보다 커야 합니다.");
                result.Add(id);
            }
            return result;
        }

        private static void EnsureUnique(IEnumerable<int> ids, string source)
        {
            var unique = new HashSet<int>();
            foreach (var id in ids)
            {
                if (!unique.Add(id))
                    throw new InvalidDataException($"{source}: ID '{id}'가 중복됩니다.");
            }
        }

        private static InvalidDataException Error(CsvTable table, int row, string message) =>
            new($"CSV {table.SourceLine(row)}행: {message}");

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private sealed class ImportData
        {
            public List<CardRow> Cards;
            public List<CharacterRow> Characters;
            public List<EnemyRow> Enemies;
            public List<RelicRow> Relics;
        }

        private sealed class CardRow
        {
            public int Id;
            public string Name, Description, UpgradedDescription;
            public CharacterClass Owner;
            public CardType Type;
            public DataRarity Rarity;
            public int Cost;
            public List<EffectSpec> Effects, UpgradedEffects;
            public List<string> Keywords;
        }

        private sealed class CharacterRow
        {
            public int Id;
            public string Name, ResourceName;
            public CharacterClass Class;
            public int MaxHealth, StartingEnergy;
            public List<int> StartingDeck, CardPool;
            public bool DeriveCardLists;
        }

        private sealed class EnemyRow
        {
            public int Id;
            public string Name;
            public EnemyTier Tier;
            public int MinHealth, MaxHealth, MinGold, MaxGold;
            public List<EnemyActionSpec> Actions;
        }

        private sealed class RelicRow
        {
            public int Id;
            public string Name, Description;
            public DataRarity Rarity;
            public RelicTrigger Trigger;
            public List<EffectSpec> Effects;
        }
    }
}
