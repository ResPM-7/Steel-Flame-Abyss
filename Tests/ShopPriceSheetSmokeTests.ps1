#상점 가격 열 제거 후 카드 동기화와 업로드 검증
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$code = @'
var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
var sync = assemblies.Select(a => a.GetType("GameDatabaseCsvSync")).First(t => t != null);
var tableType = assemblies.Select(a => a.GetType("CsvTable")).First(t => t != null);
var upload = assemblies.Select(a => a.GetType("GameDatabaseSheetUpload")).First(t => t != null);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var read = sync.GetMethod("ReadCards", flags);
var parse = tableType.GetMethod("Parse");
var serialize = upload.GetMethod("CardRow", flags);
System.Func<string, System.Collections.IList> readCsv = csv => (System.Collections.IList)read.Invoke(null, new[] { parse.Invoke(null, new object[] { "test", csv }) });
string header = "cardId,cardName,owner,cardType,rarity,cost,target,effect1,value1,effect2,value2,upgradeCost,upgradeValue1,upgradeValue2,description,enabled";
string types = "int,string,enum,enum,enum,int,enum,enum,int,enum,int,int,int,int,string,bool";
string row = "20001,Test,Flame,Attack,Starter,1,SingleEnemy,Damage,5,ApplyBurn,2,1,7,3,damage burn,TRUE";
var rows = readCsv(header + "\n" + types + "\n" + row + "\n" + row.Replace("20001", "20002").Replace("TRUE", "FALSE"));
if (rows.Count != 1) throw new System.Exception("Type/disabled row filtering failed");
var parsed = rows[0];
if ((int)parsed.GetType().GetField("Cost").GetValue(parsed) != 1) throw new System.Exception("Cost shifted");
var effects = (System.Collections.Generic.List<EffectSpec>)parsed.GetType().GetField("Effects").GetValue(parsed);
if (effects.Count != 2 || effects[0].Amount != 5 || effects[1].Type != EffectType.Burn || effects[1].Amount != 2 || effects[1].Target != TargetType.SingleEnemy) throw new System.Exception("Effect columns shifted");
var standard = readCsv("Id,Name,Character,Type,Rarity,Cost,Description,UpgradedDescription,Effects,UpgradedEffects,Keywords\n10001,Test,Warrior,Attack,Common,1,damage,,Damage:SingleEnemy:6,,");
if (standard.Count != 1) throw new System.Exception("Standard schema failed");
var legacy = readCsv(header.Replace("cost,target", "cost,shopPrice,target") + "\n" + row.Replace("1,SingleEnemy", "1,40,SingleEnemy"));
if (legacy.Count != 1) throw new System.Exception("Legacy extra column failed");
var card = UnityEngine.ScriptableObject.CreateInstance<CardData>();
try
{
    card.EditorSetIdentity(20001,"Test");
    card.EditorApply(CharacterClass.FireMage, CardType.Attack, DataRarity.Starter, 1, "damage burn", "", effects, new System.Collections.Generic.List<EffectSpec>(), new System.Collections.Generic.List<string>());
    var output = Newtonsoft.Json.Linq.JObject.Parse(UnityEngine.JsonUtility.ToJson(serialize.Invoke(null, new object[] { card })));
    if (output["fields"].Any(f => string.Equals((string)f["name"], "shopPrice", System.StringComparison.OrdinalIgnoreCase))) throw new System.Exception("Upload includes removed price");
    if (!output["fields"].Any(f => (string)f["name"] == "effect2" && (string)f["value"] == "ApplyBurn")) throw new System.Exception("Upload burn lost");
    if (new UnityEditor.SerializedObject(card).FindProperty("shopPrice") != null) throw new System.Exception("SO still contains price");
}
finally { UnityEngine.Object.DestroyImmediate(card); }
return "PASS: new schema, type/disabled rows, cost/target/effects alignment, standard schema, legacy extra column, SO removal, upload removal (no remote upload)";
'@
$result = unity command eval --code $code --project-path $projectPath --caller plugin --skill unity-cli --format json | ConvertFrom-Json
if (-not $result.success -or -not $result.data.result.success) {
    throw ($result | ConvertTo-Json -Depth 10)
}
$result.data.result.result
