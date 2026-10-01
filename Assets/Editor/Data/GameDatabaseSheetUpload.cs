using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteelFlameAbyss.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SteelFlameAbyss.Editor.Data
{
    /// <summary>GameDatabase의 현재 값을 Google Apps Script 웹앱을 통해 시트에 업로드합니다.</summary>
    internal static class GameDatabaseSheetUpload
    {
        private const string UploadTokenEditorPrefsKey = "SteelFlameAbyss.GameData.UploadToken";

        internal static string UploadToken
        {
            get => EditorPrefs.GetString(UploadTokenEditorPrefsKey, string.Empty);
            set => EditorPrefs.SetString(UploadTokenEditorPrefsKey, value ?? string.Empty);
        }

        internal static async Task UploadAsync(GameDatabase database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            if (!Uri.TryCreate(database.SheetUploadUrl?.Trim(), UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("업로드 연결 설정에 HTTPS Google Apps Script 웹앱 URL을 입력해 주세요.");

            AssetDatabase.SaveAssets();
            var payload = BuildPayload(database);
            var json = JsonUtility.ToJson(payload);
            using var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 30
            };
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"시트 업로드 실패 ({request.responseCode}): {request.error}\n{request.downloadHandler.text}");

            var response = request.downloadHandler.text;
            if (!string.IsNullOrWhiteSpace(response) &&
                response.IndexOf("\"ok\":false", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException($"시트가 업로드 요청을 거부했습니다.\n{response}");

            Debug.Log($"[게임 데이터] 시트 업로드 완료: 카드 {database.Cards.Count}, " +
                      $"캐릭터 {database.Characters.Count}, 적 {database.Enemies.Count}, 유물 {database.Relics.Count}");
        }

        private static UploadPayload BuildPayload(GameDatabase database)
        {
            var payload = new UploadPayload { token = UploadToken };
            payload.tables.Add(new UploadTable
            {
                name = database.CardsSheetName,
                rows = database.Cards.Where(value => value != null).Select(CardRow).ToList()
            });
            payload.tables.Add(new UploadTable
            {
                name = database.CharactersSheetName,
                rows = database.Characters.Where(value => value != null).Select(CharacterRow).ToList()
            });
            payload.tables.Add(new UploadTable
            {
                name = database.EnemiesSheetName,
                rows = database.Enemies.Where(value => value != null).Select(EnemyRow).ToList()
            });
            if (database.Relics.Count > 0)
            {
                payload.tables.Add(new UploadTable
                {
                    name = database.RelicsSheetName,
                    rows = database.Relics.Where(value => value != null).Select(RelicRow).ToList()
                });
            }
            return payload;
        }

        private static UploadRow CardRow(CardData card)
        {
            var row = NewRow(card);
            Add(row, "cardId", card.Id);
            Add(row, "cardName", card.DisplayName);
            Add(row, "owner", LegacyOwner(card.Owner));
            Add(row, "cardType", card.CardType.ToString());
            Add(row, "rarity", card.Rarity.ToString());
            Add(row, "cost", card.Cost.ToString(CultureInfo.InvariantCulture));
            Add(row, "target", card.Effects.Count > 0 ? card.Effects[0].Target.ToString() : string.Empty);
            Add(row, "effect1", card.Effects.Count > 0 ? LegacyEffect(card.Effects[0].Type) : "None");
            Add(row, "value1", card.Effects.Count > 0 ? card.Effects[0].Amount.ToString(CultureInfo.InvariantCulture) : "0");
            Add(row, "effect2", card.Effects.Count > 1 ? LegacyEffect(card.Effects[1].Type) : "None");
            Add(row, "value2", card.Effects.Count > 1 ? card.Effects[1].Amount.ToString(CultureInfo.InvariantCulture) : "0");

            Add(row, "Character", card.Owner.ToString());
            Add(row, "Type", card.CardType.ToString());
            Add(row, "Rarity", card.Rarity.ToString());
            Add(row, "Cost", card.Cost.ToString(CultureInfo.InvariantCulture));
            Add(row, "Description", card.Description);
            Add(row, "UpgradedDescription", card.UpgradedDescription);
            Add(row, "Effects", Effects(card.Effects));
            Add(row, "UpgradedEffects", Effects(card.UpgradedEffects));
            Add(row, "Keywords", string.Join("|", card.Keywords));
            return row;
        }

        private static UploadRow CharacterRow(CharacterData character)
        {
            var row = NewRow(character);
            Add(row, "Class", character.Class.ToString());
            Add(row, "MaxHealth", character.MaxHealth.ToString(CultureInfo.InvariantCulture));
            Add(row, "StartingDeck", string.Join("|", character.StartingDeckIds));
            Add(row, "CardPool", string.Join("|", character.CardPoolIds));
            Add(row, "ResourceName", character.ResourceName);
            return row;
        }

        private static UploadRow EnemyRow(EnemyData enemy)
        {
            var row = NewRow(enemy);
            Add(row, "Tier", enemy.Tier.ToString());
            Add(row, "MinHealth", enemy.MinHealth.ToString(CultureInfo.InvariantCulture));
            Add(row, "MaxHealth", enemy.MaxHealth.ToString(CultureInfo.InvariantCulture));
            Add(row, "MinGold", enemy.MinGold.ToString(CultureInfo.InvariantCulture));
            Add(row, "MaxGold", enemy.MaxGold.ToString(CultureInfo.InvariantCulture));
            Add(row, "Actions", string.Join("|", enemy.Actions.Select(action =>
                $"{action.Intent}~{action.Weight.ToString(CultureInfo.InvariantCulture)}~{Effects(action.Effects, '&')}")));
            return row;
        }

        private static UploadRow RelicRow(RelicData relic)
        {
            var row = NewRow(relic);
            Add(row, "Rarity", relic.Rarity.ToString());
            Add(row, "Description", relic.Description);
            Add(row, "Trigger", relic.Trigger.ToString());
            Add(row, "Effects", Effects(relic.Effects));
            return row;
        }

        private static UploadRow NewRow(GameDataEntry entry)
        {
            var row = new UploadRow { id = entry.Id };
            Add(row, "Id", entry.Id);
            Add(row, "Name", entry.DisplayName);
            return row;
        }

        private static void Add(UploadRow row, string name, string value) =>
            row.fields.Add(new UploadField { name = name, value = value ?? string.Empty });

        private static string Effects(IReadOnlyList<EffectSpec> effects, char separator = '|') =>
            string.Join(separator.ToString(), effects.Select(effect =>
                $"{effect.Type}:{effect.Target}:{effect.Amount}:{effect.SecondaryAmount}:{effect.Duration}"));

        private static string LegacyOwner(CharacterClass owner) => owner switch
        {
            CharacterClass.Warrior => "Steel",
            CharacterClass.FireMage => "Flame",
            CharacterClass.Warlock => "Abyss",
            _ => "Public"
        };

        private static string LegacyEffect(EffectType type) => type switch
        {
            EffectType.Strength => "GainStrength",
            EffectType.Rage => "GainRage",
            EffectType.Burn => "ApplyBurn",
            EffectType.MindFracture => "ApplyMentalSplit",
            EffectType.AmplifyMindFracture => "MultiplyMentalSplit",
            EffectType.CreateStatusCard => "ApplyHallucination",
            _ => type.ToString()
        };

        [Serializable]
        private sealed class UploadPayload
        {
            public string token;
            public List<UploadTable> tables = new();
        }

        [Serializable]
        private sealed class UploadTable
        {
            public string name;
            public List<UploadRow> rows = new();
        }

        [Serializable]
        private sealed class UploadRow
        {
            public string id;
            public List<UploadField> fields = new();
        }

        [Serializable]
        private sealed class UploadField
        {
            public string name;
            public string value;
        }
    }
}
