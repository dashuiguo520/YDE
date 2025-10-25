using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Dynamic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;
using YamlDataEditor.Models;

namespace YamlDataEditor.Services
{
    public class YamlService
    {
        public List<Item> LoadFromFile(string filePath, Encoding encoding = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            try
            {
                encoding = encoding ?? Encoding.UTF8;
                var yamlContent = File.ReadAllText(filePath, encoding);

                Console.WriteLine($"=== 尝试解析文件: {filePath} ===");

                // 新增：优先尝试完整结构解析
                var items = ParseCompleteStructure(yamlContent, filePath, out _);
                if (items.Count > 0)
                {
                    return items;
                }

                // 如果完整结构解析失败，回退到原有解析逻辑
                return ParseYamlContent(yamlContent, filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"加载YAML文件失败: {ex.Message}", ex);
            }
        }

        private List<Item> ParseYamlContent(string yamlContent, string filePath = "")
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            try
            {
                Console.WriteLine($"开始解析YAML内容，文件: {filePath}");

                // 方法1: 尝试解析为包含Header和Body的结构
                try
                {
                    var yamlDoc = deserializer.Deserialize<YamlDocumentStructure>(yamlContent);
                    if (yamlDoc?.Body != null && yamlDoc.Body.Count > 0)
                    {
                        Console.WriteLine($"方法1成功: Header-Body结构，找到 {yamlDoc.Body.Count} 个物品");
                        return yamlDoc.Body;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"方法1失败: {ex.Message}");
                }

                // 方法2: 尝试直接解析为物品列表
                try
                {
                    var items = deserializer.Deserialize<List<Item>>(yamlContent);
                    if (items != null && items.Count > 0)
                    {
                        Console.WriteLine($"方法2成功: 直接解析为列表，找到 {items.Count} 个物品");
                        return items;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"方法2失败: {ex.Message}");
                }

                // 方法3: 尝试解析为字典<string, Item>格式
                try
                {
                    var dict = deserializer.Deserialize<Dictionary<string, Item>>(yamlContent);
                    if (dict != null && dict.Count > 0)
                    {
                        var items = dict.Values.Where(item => item != null).ToList();
                        Console.WriteLine($"方法3成功: 字典格式，找到 {items.Count} 个物品");
                        return items;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"方法3失败: {ex.Message}");
                }

                // 方法4: 使用动态解析处理复杂结构
                try
                {
                    var dynamicData = deserializer.Deserialize<dynamic>(yamlContent);
                    var items = ExtractItemsFromDynamic(dynamicData);
                    if (items != null && items.Count > 0)
                    {
                        Console.WriteLine($"方法4成功: 动态解析，找到 {items.Count} 个物品");
                        return items;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"方法4失败: {ex.Message}");
                }

                // 方法5: 使用低级YAML流解析
                try
                {
                    var items = ParseWithYamlStream(yamlContent);
                    if (items != null && items.Count > 0)
                    {
                        Console.WriteLine($"方法5成功: YAML流解析，找到 {items.Count} 个物品");
                        return items;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"方法5失败: {ex.Message}");
                }

                Console.WriteLine("所有解析方法都失败");
                return new List<Item>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YAML解析失败: {ex.Message}");
                return new List<Item>();
            }
        }

        private List<Item> ParseWithYamlStream(string yamlContent)
        {
            var items = new List<Item>();

            using (var reader = new StringReader(yamlContent))
            {
                var stream = new YamlStream();
                stream.Load(reader);

                foreach (var document in stream.Documents)
                {
                    ExtractItemsFromYamlNode(document.RootNode, items);
                }
            }

            return items;
        }

        private void ExtractItemsFromYamlNode(YamlNode node, List<Item> items)
        {
            if (node is YamlSequenceNode sequenceNode)
            {
                foreach (var childNode in sequenceNode.Children)
                {
                    if (childNode is YamlMappingNode mappingNode)
                    {
                        var item = ConvertYamlMappingToItem(mappingNode);
                        if (item != null && item.Id > 0)
                        {
                            items.Add(item);
                        }
                    }
                }
            }
            else if (node is YamlMappingNode mappingNode)
            {
                foreach (var entry in mappingNode.Children)
                {
                    if (entry.Value is YamlMappingNode valueMapping)
                    {
                        var item = ConvertYamlMappingToItem(valueMapping);
                        if (item != null && item.Id > 0)
                        {
                            items.Add(item);
                        }
                    }
                    else if (entry.Value is YamlSequenceNode valueSequence)
                    {
                        ExtractItemsFromYamlNode(valueSequence, items);
                    }
                }
            }
        }

        private Item ConvertYamlMappingToItem(YamlMappingNode mappingNode)
        {
            var item = new Item();

            foreach (var entry in mappingNode.Children)
            {
                var key = ((YamlScalarNode)entry.Key).Value;
                var valueNode = entry.Value;

                SetPropertyFromYamlNode(item, key, valueNode);
            }

            return item;
        }

        private void SetPropertyFromYamlNode(Item item, string key, YamlNode valueNode)
        {
            try
            {
                if (valueNode is YamlScalarNode scalarNode)
                {
                    var value = scalarNode.Value;

                    switch (key.ToLower())
                    {
                        case "id":
                            if (int.TryParse(value, out int id))
                                item.Id = id;
                            break;
                        case "aegisname":
                            item.AegisName = value ?? "";
                            break;
                        case "name":
                            item.Name = value ?? "";
                            break;
                        case "type":
                            item.Type = value ?? "";
                            break;
                        case "subtype":
                            item.SubType = value ?? "";
                            break;
                        case "buy":
                            if (int.TryParse(value, out int buy))
                                item.Buy = buy;
                            break;
                        case "sell":
                            if (int.TryParse(value, out int sell))
                                item.Sell = sell;
                            break;
                        case "weight":
                            if (int.TryParse(value, out int weight))
                                item.Weight = weight;
                            break;
                        case "attack":
                            if (int.TryParse(value, out int attack))
                                item.Attack = attack;
                            break;
                        case "magicattack":
                            if (int.TryParse(value, out int magicAttack))
                                item.MagicAttack = magicAttack;
                            break;
                        case "range":
                            if (int.TryParse(value, out int range))
                                item.Range = range;
                            break;
                        case "slots":
                            if (int.TryParse(value, out int slots))
                                item.Slots = slots;
                            break;
                        case "weaponlevel":
                            if (int.TryParse(value, out int weaponLevel))
                                item.WeaponLevel = weaponLevel;
                            break;
                        case "equiplevelmin":
                            if (int.TryParse(value, out int equipLevelMin))
                                item.EquipLevelMin = equipLevelMin;
                            break;
                        case "refineable":
                            if (bool.TryParse(value, out bool refineable))
                                item.Refineable = refineable;
                            else if (value?.ToLower() == "true" || value == "1")
                                item.Refineable = true;
                            else if (value?.ToLower() == "false" || value == "0")
                                item.Refineable = false;
                            break;
                        case "script":
                            item.Script = value ?? "";
                            break;
                        case "equipscript":
                            item.EquipScript = value ?? "";
                            break;
                        case "unequipscript":
                            item.UnEquipScript = value ?? "";
                            break;
                    }
                }
                else if (valueNode is YamlMappingNode mappingNode)
                {
                    // 处理嵌套对象
                    switch (key.ToLower())
                    {
                        case "jobs":
                            item.Jobs = ParseJobRequirementsFromYaml(mappingNode);
                            break;
                        case "locations":
                            item.Locations = ParseLocationRequirementsFromYaml(mappingNode);
                            break;
                        case "trade":
                            item.Trade = ParseTradeRestrictionsFromYaml(mappingNode);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置属性 {key} 失败: {ex.Message}");
            }
        }

        private JobRequirements ParseJobRequirementsFromYaml(YamlMappingNode mappingNode)
        {
            var jobs = new JobRequirements();

            foreach (var entry in mappingNode.Children)
            {
                var jobKey = ((YamlScalarNode)entry.Key).Value.ToLower();
                var valueNode = entry.Value;

                if (valueNode is YamlScalarNode scalarNode)
                {
                    bool value = false;
                    if (bool.TryParse(scalarNode.Value, out value) ||
                        scalarNode.Value == "1" || scalarNode.Value?.ToLower() == "true")
                    {
                        value = true;
                    }

                    switch (jobKey)
                    {
                        case "alchemist": jobs.Alchemist = value; break;
                        case "archer": jobs.Archer = value; break;
                        case "assassin": jobs.Assassin = value; break;
                        case "swordman": jobs.Swordman = value; break;
                        case "mage": jobs.Mage = value; break;
                        case "merchant": jobs.Merchant = value; break;
                        case "acolyte": jobs.Acolyte = value; break;
                        case "thief": jobs.Thief = value; break;
                    }
                }
            }

            return jobs;
        }

        private LocationRequirements ParseLocationRequirementsFromYaml(YamlMappingNode mappingNode)
        {
            var locations = new LocationRequirements();

            foreach (var entry in mappingNode.Children)
            {
                var locationKey = ((YamlScalarNode)entry.Key).Value.ToLower();
                var valueNode = entry.Value;

                if (valueNode is YamlScalarNode scalarNode)
                {
                    bool value = false;
                    if (bool.TryParse(scalarNode.Value, out value) ||
                        scalarNode.Value == "1" || scalarNode.Value?.ToLower() == "true")
                    {
                        value = true;
                    }

                    switch (locationKey)
                    {
                        case "right_hand": locations.Right_Hand = value; break;
                        case "both_hand": locations.Both_Hand = value; break;
                        case "head": locations.Head = value; break;
                        case "body": locations.Body = value; break;
                        case "garment": locations.Garment = value; break;
                        case "shoes": locations.Shoes = value; break;
                        case "accessory": locations.Accessory = value; break;
                    }
                }
            }

            return locations;
        }

        private TradeRestrictions ParseTradeRestrictionsFromYaml(YamlMappingNode mappingNode)
        {
            var trade = new TradeRestrictions();

            foreach (var entry in mappingNode.Children)
            {
                var tradeKey = ((YamlScalarNode)entry.Key).Value.ToLower();
                var valueNode = entry.Value;

                if (valueNode is YamlScalarNode scalarNode)
                {
                    bool value = false;
                    if (bool.TryParse(scalarNode.Value, out value) ||
                        scalarNode.Value == "1" || scalarNode.Value?.ToLower() == "true")
                    {
                        value = true;
                    }

                    switch (tradeKey)
                    {
                        case "nodrop": trade.NoDrop = value; break;
                        case "notrade": trade.NoTrade = value; break;
                        case "nosell": trade.NoSell = value; break;
                        case "nostorage": trade.NoStorage = value; break;
                        case "novend": trade.NoVend = value; break;
                        case "notraderoom": trade.NoTradeRoom = value; break;
                        case "nocart": trade.NoCart = value; break;
                        case "noguildstorage": trade.NoGuildStorage = value; break;
                        case "nomail": trade.NoMail = value; break;
                        case "noauction": trade.NoAuction = value; break;
                    }
                }
            }

            return trade;
        }

        // 动态解析方法
        private List<Item> ExtractItemsFromDynamic(dynamic dynamicData)
        {
            var items = new List<Item>();

            try
            {
                if (dynamicData is System.Collections.IDictionary dict)
                {
                    // 查找Body字段
                    if (dict.Contains("Body"))
                    {
                        var body = dict["Body"];
                        if (body is System.Collections.IEnumerable collection)
                        {
                            foreach (var itemData in collection)
                            {
                                var item = ConvertDynamicToItem(itemData);
                                if (item != null && item.Id > 0)
                                {
                                    items.Add(item);
                                }
                            }
                        }
                    }
                    else
                    {
                        // 如果没有Body，尝试直接解析
                        foreach (System.Collections.DictionaryEntry kvp in dict)
                        {
                            var item = ConvertDynamicToItem(kvp.Value);
                            if (item != null && item.Id > 0)
                            {
                                items.Add(item);
                            }
                        }
                    }
                }
                else if (dynamicData is System.Collections.IEnumerable list && !(dynamicData is string))
                {
                    foreach (var itemData in list)
                    {
                        var item = ConvertDynamicToItem(itemData);
                        if (item != null && item.Id > 0)
                        {
                            items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"动态提取物品失败: {ex.Message}");
            }

            return items;
        }

        private Item ConvertDynamicToItem(dynamic dynamicData)
        {
            if (dynamicData == null) return null;

            try
            {
                var item = new Item();

                if (dynamicData is System.Collections.IDictionary dict)
                {
                    foreach (System.Collections.DictionaryEntry kvp in dict)
                    {
                        var key = kvp.Key.ToString();
                        var value = kvp.Value;

                        SetPropertyFromDictionary(item, key, value);
                    }
                }
                else
                {
                    // 使用反射设置属性
                    var properties = typeof(Item).GetProperties();
                    foreach (var prop in properties)
                    {
                        try
                        {
                            SetPropertyFromDynamic(item, prop, dynamicData);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"设置属性 {prop.Name} 失败: {ex.Message}");
                        }
                    }
                }

                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换动态对象失败: {ex.Message}");
                return null;
            }
        }

        private void SetPropertyFromDynamic(Item item, System.Reflection.PropertyInfo prop, dynamic dynamicData)
        {
            try
            {
                // 尝试从动态对象中获取值
                var value = GetValueFromDynamic(prop.Name, dynamicData);
                if (value == null) return;

                if (prop.PropertyType == typeof(int))
                {
                    int intValue;
                    if (int.TryParse(value.ToString(), out intValue))
                    {
                        prop.SetValue(item, intValue);
                    }
                }
                else if (prop.PropertyType == typeof(int?))
                {
                    if (value.ToString() == "null")
                    {
                        prop.SetValue(item, null);
                    }
                    else
                    {
                        int intValue;
                        if (int.TryParse(value.ToString(), out intValue))
                        {
                            prop.SetValue(item, intValue);
                        }
                    }
                }
                else if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(item, value?.ToString() ?? "");
                }
                else if (prop.PropertyType == typeof(bool?))
                {
                    if (value is bool b)
                        prop.SetValue(item, b);
                    else if (value.ToString().ToLower() == "true" || value.ToString() == "1")
                        prop.SetValue(item, true);
                    else if (value.ToString().ToLower() == "false" || value.ToString() == "0")
                        prop.SetValue(item, false);
                    else
                        prop.SetValue(item, null);
                }
                else
                {
                    // 复杂类型，尝试直接赋值
                    prop.SetValue(item, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置属性 {prop.Name} 失败: {ex.Message}");
            }
        }

        private void SetPropertyFromDictionary(Item item, string key, object value)
        {
            try
            {
                switch (key.ToLower())
                {
                    case "id":
                        if (int.TryParse(value?.ToString(), out int id))
                            item.Id = id;
                        break;
                    case "aegisname":
                        item.AegisName = value?.ToString() ?? "";
                        break;
                    case "name":
                        item.Name = value?.ToString() ?? "";
                        break;
                    case "type":
                        item.Type = value?.ToString() ?? "";
                        break;
                    case "subtype":
                        item.SubType = value?.ToString() ?? "";
                        break;
                    case "buy":
                        if (int.TryParse(value?.ToString(), out int buy))
                            item.Buy = buy;
                        break;
                    case "sell":
                        if (int.TryParse(value?.ToString(), out int sell))
                            item.Sell = sell;
                        break;
                    case "weight":
                        if (int.TryParse(value?.ToString(), out int weight))
                            item.Weight = weight;
                        break;
                    case "attack":
                        if (int.TryParse(value?.ToString(), out int attack))
                            item.Attack = attack;
                        break;
                    case "magicattack":
                        if (int.TryParse(value?.ToString(), out int magicAttack))
                            item.MagicAttack = magicAttack;
                        break;
                    case "range":
                        if (int.TryParse(value?.ToString(), out int range))
                            item.Range = range;
                        break;
                    case "slots":
                        if (int.TryParse(value?.ToString(), out int slots))
                            item.Slots = slots;
                        break;
                    case "weaponlevel":
                        if (int.TryParse(value?.ToString(), out int weaponLevel))
                            item.WeaponLevel = weaponLevel;
                        break;
                    case "equiplevelmin":
                        if (int.TryParse(value?.ToString(), out int equipLevelMin))
                            item.EquipLevelMin = equipLevelMin;
                        break;
                    case "refineable":
                        if (value is bool refineableBool)
                            item.Refineable = refineableBool;
                        else if (value?.ToString().ToLower() == "true" || value?.ToString() == "1")
                            item.Refineable = true;
                        else if (value?.ToString().ToLower() == "false" || value?.ToString() == "0")
                            item.Refineable = false;
                        else
                            item.Refineable = null;
                        break;
                    case "script":
                        item.Script = value?.ToString() ?? "";
                        break;
                    case "equipscript":
                        item.EquipScript = value?.ToString() ?? "";
                        break;
                    case "unequipscript":
                        item.UnEquipScript = value?.ToString() ?? "";
                        break;
                    case "jobs":
                        if (value is System.Collections.IDictionary jobsDict)
                        {
                            item.Jobs = ParseJobRequirements(jobsDict);
                        }
                        break;
                    case "locations":
                        if (value is System.Collections.IDictionary locationsDict)
                        {
                            item.Locations = ParseLocationRequirements(locationsDict);
                        }
                        break;
                    case "trade":
                        if (value is System.Collections.IDictionary tradeDict)
                        {
                            item.Trade = ParseTradeRestrictions(tradeDict);
                        }
                        break;
                    default:
                        Console.WriteLine($"未知属性: {key} = {value}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置字典属性 {key} 失败: {ex.Message}");
            }
        }

        // 添加辅助方法来解析复杂对象
        private JobRequirements ParseJobRequirements(System.Collections.IDictionary jobsDict)
        {
            var jobs = new JobRequirements();

            foreach (System.Collections.DictionaryEntry kvp in jobsDict)
            {
                var jobKey = kvp.Key.ToString().ToLower();
                var jobValue = kvp.Value;

                if (jobValue is bool jobBool)
                {
                    switch (jobKey)
                    {
                        case "alchemist": jobs.Alchemist = jobBool; break;
                        case "archer": jobs.Archer = jobBool; break;
                        case "assassin": jobs.Assassin = jobBool; break;
                        case "swordman": jobs.Swordman = jobBool; break;
                        case "mage": jobs.Mage = jobBool; break;
                        case "merchant": jobs.Merchant = jobBool; break;
                        case "acolyte": jobs.Acolyte = jobBool; break;
                        case "thief": jobs.Thief = jobBool; break;
                    }
                }
            }

            return jobs;
        }

        // 在 YamlService 类中添加或修改方法
        private List<Item> ParseCompleteStructure(string yamlContent, string filePath, out List<string> importPaths)
        {
            importPaths = new List<string>();

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                // 尝试解析为完整结构
                var document = deserializer.Deserialize<YamlDocumentStructure>(yamlContent);

                if (document != null)
                {
                    Console.WriteLine($"成功解析完整结构: Header.Type={document.Header?.Type}");

                    // 提取导入路径
                    if (document.Footer?.Imports != null)
                    {
                        importPaths.AddRange(document.Footer.Imports.Select(imp => imp.Path));
                        Console.WriteLine($"找到 {importPaths.Count} 个导入文件");
                    }

                    // 只有当 Body 存在且不为空时才返回物品
                    if (document.Body != null && document.Body.Count > 0)
                    {
                        Console.WriteLine($"Body 包含 {document.Body.Count} 个物品");
                        return document.Body;
                    }
                    else
                    {
                        Console.WriteLine("Body 为空或不存在，返回空列表");
                        return new List<Item>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析完整结构失败: {ex.Message}");
            }

            return new List<Item>();
        }

        private LocationRequirements ParseLocationRequirements(System.Collections.IDictionary locationsDict)
        {
            var locations = new LocationRequirements();

            foreach (System.Collections.DictionaryEntry kvp in locationsDict)
            {
                var locationKey = kvp.Key.ToString().ToLower();
                var locationValue = kvp.Value;

                if (locationValue is bool locationBool)
                {
                    switch (locationKey)
                    {
                        case "right_hand": locations.Right_Hand = locationBool; break;
                        case "both_hand": locations.Both_Hand = locationBool; break;
                        case "head": locations.Head = locationBool; break;
                        case "body": locations.Body = locationBool; break;
                        case "garment": locations.Garment = locationBool; break;
                        case "shoes": locations.Shoes = locationBool; break;
                        case "accessory": locations.Accessory = locationBool; break;
                    }
                }
            }

            return locations;
        }

        private TradeRestrictions ParseTradeRestrictions(System.Collections.IDictionary tradeDict)
        {
            var trade = new TradeRestrictions();

            foreach (System.Collections.DictionaryEntry kvp in tradeDict)
            {
                var tradeKey = kvp.Key.ToString().ToLower();
                var tradeValue = kvp.Value;

                if (tradeValue is bool tradeBool)
                {
                    switch (tradeKey)
                    {
                        case "nodrop": trade.NoDrop = tradeBool; break;
                        case "notrade": trade.NoTrade = tradeBool; break;
                        case "nosell": trade.NoSell = tradeBool; break;
                        case "nostorage": trade.NoStorage = tradeBool; break;
                        case "novend": trade.NoVend = tradeBool; break;
                        case "notraderoom": trade.NoTradeRoom = tradeBool; break;
                        case "nocart": trade.NoCart = tradeBool; break;
                        case "noguildstorage": trade.NoGuildStorage = tradeBool; break;
                        case "nomail": trade.NoMail = tradeBool; break;
                        case "noauction": trade.NoAuction = tradeBool; break;
                    }
                }
            }

            return trade;
        }

        private object GetValueFromDynamic(string propertyName, dynamic dynamicData)
        {
            try
            {
                var nameVariants = new[]
                {
                    propertyName.ToLower(),
                    propertyName,
                    char.ToLower(propertyName[0]) + propertyName.Substring(1),
                    propertyName.ToUpper()
                };

                foreach (var name in nameVariants)
                {
                    try
                    {
                        if (dynamicData is IDictionary<object, object> dict)
                        {
                            if (dict.ContainsKey(name))
                                return dict[name];
                        }
                        else
                        {
                            // 尝试直接访问属性
                            var prop = dynamicData.GetType().GetProperty(name);
                            if (prop != null)
                                return prop.GetValue(dynamicData);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void SaveToFile(string filePath, List<Item> items, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                var yaml = serializer.Serialize(items);

                // 使用指定的编码保存
                File.WriteAllText(filePath, yaml, encoding);

                Console.WriteLine($"YAML服务: 成功保存 {items.Count} 个物品到 {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YAML服务保存失败: {ex.Message}");
                throw;
            }
        }

        // 在 YamlService 类中添加
        public List<Item> LoadStructuredFile(string filePath, Encoding encoding, out List<string> importPaths)
        {
            importPaths = new List<string>();
            var items = new List<Item>();

            try
            {
                var yamlContent = File.ReadAllText(filePath, encoding);

                // 使用低级YAML解析提取结构
                using (var reader = new StringReader(yamlContent))
                {
                    var yamlStream = new YamlDotNet.RepresentationModel.YamlStream();
                    yamlStream.Load(reader);

                    if (yamlStream.Documents.Count > 0)
                    {
                        var rootNode = yamlStream.Documents[0].RootNode;
                        ExtractFromStructuredYaml(rootNode, items, importPaths);
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析结构化文件失败 {filePath}: {ex.Message}");
                return new List<Item>();
            }
        }

        private void ExtractFromStructuredYaml(YamlDotNet.RepresentationModel.YamlNode rootNode, List<Item> items, List<string> importPaths)
        {
            if (rootNode is YamlDotNet.RepresentationModel.YamlMappingNode mappingNode)
            {
                // 1. 提取Body中的物品
                var bodyKey = new YamlDotNet.RepresentationModel.YamlScalarNode("Body");
                if (mappingNode.Children.ContainsKey(bodyKey))
                {
                    var bodyNode = mappingNode.Children[bodyKey];
                    if (bodyNode is YamlDotNet.RepresentationModel.YamlSequenceNode bodySequence)
                    {
                        // 使用现有的转换方法
                        foreach (var itemNode in bodySequence.Children)
                        {
                            if (itemNode is YamlDotNet.RepresentationModel.YamlMappingNode itemMapping)
                            {
                                var item = ConvertYamlMappingToItem(itemMapping);
                                if (item != null && item.Id > 0)
                                {
                                    items.Add(item);
                                }
                            }
                        }
                    }
                }

                // 2. 提取Footer中的导入路径
                var footerKey = new YamlDotNet.RepresentationModel.YamlScalarNode("Footer");
                if (mappingNode.Children.ContainsKey(footerKey))
                {
                    var footerNode = mappingNode.Children[footerKey];
                    if (footerNode is YamlDotNet.RepresentationModel.YamlMappingNode footerMapping)
                    {
                        var importsKey = new YamlDotNet.RepresentationModel.YamlScalarNode("Imports");
                        if (footerMapping.Children.ContainsKey(importsKey))
                        {
                            var importsNode = footerMapping.Children[importsKey];
                            if (importsNode is YamlDotNet.RepresentationModel.YamlSequenceNode importsSequence)
                            {
                                foreach (var importItem in importsSequence.Children)
                                {
                                    if (importItem is YamlDotNet.RepresentationModel.YamlMappingNode importMapping)
                                    {
                                        var pathKey = new YamlDotNet.RepresentationModel.YamlScalarNode("Path");
                                        if (importMapping.Children.ContainsKey(pathKey))
                                        {
                                            importPaths.Add(importMapping.Children[pathKey].ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 添加数据结构类
        public class YamlDocumentStructure
        {
            public YamlHeader Header { get; set; }
            public List<Item> Body { get; set; } = new List<Item>();
            public YamlFooter Footer { get; set; }
        }

        public class YamlHeader
        {
            public string Type { get; set; } = string.Empty;
            public int Version { get; set; }
        }

        public class YamlFooter
        {
            public List<YamlImport> Imports { get; set; }
        }

        public class YamlImport
        {
            public string Path { get; set; }
        }
    }
}