$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$code = @'
var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
var sync = assemblies.Select(a => a.GetType("GameDatabaseCsvSync")).First(t => t != null);
var tableType = assemblies.Select(a => a.GetType("CsvTable")).First(t => t != null);
var upload = assemblies.Select(a => a.GetType("GameDatabaseSheetUpload")).First(t => t != null);
var read = sync.GetMethod("ReadCards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var parse = tableType.GetMethod("Parse");
var serialize = upload.GetMethod("CardRow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
string header = "cardId,cardName,owner,cardType,rarity,cost,shopPrice,target,effect1,value1,effect2,value2,upgradeCost,upgradeValue1,upgradeValue2,description,enabled";
string types = "int,string,enum,enum,enum,int,int,enum,enum,int,enum,int,int,int,int,string,bool";
System.Func<string, System.Collections.IList> readCsv = csv => (System.Collections.IList)read.Invoke(null, new[] { parse.Invoke(null, new object[] { "test", csv }) });
int checks = 0;
foreach (int price in new[] { 0, 40, 50, 80 })
{
    string row = "10001,Test,Steel,Attack,Common,1," + price + ",SingleEnemy,Damage,6,None,0,1,9,0,damage,TRUE";
    var rows = readCsv(header + "\n" + types + "\n" + row);
    var parsed = rows[0];
    if (rows.Count != 1 || (int)parsed.GetType().GetField("ShopPrice").GetValue(parsed) != price) throw new System.Exception("Price parse failed");
    if ((int)parsed.GetType().GetField("Cost").GetValue(parsed) != 1) throw new System.Exception("Cost column shifted");
    checks += 2;
    var card = UnityEngine.ScriptableObject.CreateInstance<CardData>();
    try
    {
        card.EditorSetIdentity(10001, "Test");
        card.EditorApply(CharacterClass.Warrior, CardType.Attack, DataRarity.Common, 1, "damage", "", new System.Collections.Generic.List<EffectSpec>(), new System.Collections.Generic.List<EffectSpec>(), new System.Collections.Generic.List<string>(), price);
        if (card.ShopPrice != price) throw new System.Exception("SO price failed");
        var output = Newtonsoft.Json.Linq.JObject.Parse(UnityEngine.JsonUtility.ToJson(serialize.Invoke(null, new object[] { card })));
        var priceFields = output["fields"].Where(f => (string)f["name"] == "shopPrice").ToArray();
        if (priceFields.Length != 1 || (string)priceFields[0]["value"] != price.ToString()) throw new System.Exception("Upload price failed");
        checks += 2;
    }
    finally { UnityEngine.Object.DestroyImmediate(card); }
}
foreach (string price in new[] { "", "-1", "abc", "1.5", "2147483648" })
{
    bool rejected = false;
    try { readCsv(header + "\n10001,Test,Steel,Attack,Common,1," + price + ",SingleEnemy,Damage,6,None,0,1,9,0,damage,TRUE"); }
    catch (System.Reflection.TargetInvocationException e) { rejected = e.InnerException is System.IO.InvalidDataException; }
    if (!rejected) throw new System.Exception("Invalid price accepted: " + price);
    checks++;
}
bool missingRejected = false;
try { readCsv(header.Replace(",shopPrice", "") + "\n10001,Test,Steel,Attack,Common,1,SingleEnemy,Damage,6,None,0,1,9,0,damage,TRUE"); }
catch (System.Reflection.TargetInvocationException e) { missingRejected = e.InnerException is System.IO.InvalidDataException; }
if (!missingRejected) throw new System.Exception("Missing price accepted");
checks++;
var standard = readCsv("Id,Name,Character,Type,Rarity,Cost,ShopPrice,Description,UpgradedDescription,Effects,UpgradedEffects,Keywords\n10001,Test,Warrior,Attack,Common,1,80,damage,,Damage:SingleEnemy:6,,");
if ((int)standard[0].GetType().GetField("ShopPrice").GetValue(standard[0]) != 80) throw new System.Exception("Standard schema failed");
checks++;
return "PASS: " + checks + " shop price import / SO / upload checks (no remote upload)";
'@
$result = unity command eval --code $code --project-path $projectPath --caller plugin --skill unity-cli --format json | ConvertFrom-Json
if (-not $result.success -or -not $result.data.result.success) {
    throw ($result | ConvertTo-Json -Depth 10)
}
$result.data.result.result
