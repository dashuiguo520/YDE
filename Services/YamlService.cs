// Services/YamlService.cs
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDataEditor.Forms;
using YamlDataEditor.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YamlDataEditor.Services
{
    public class YamlService
    {
        private Encoding _currentEncoding = Encoding.UTF8;

        // 记录当前使用的编码
        public Encoding CurrentEncoding => _currentEncoding;

        public List<Item> LoadFromFile(string filePath, Encoding encoding = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            try
            {
                // 使用指定的编码或自动检测
                _currentEncoding = encoding ?? DetectFileEncodingWithFallback(filePath);
                Console.WriteLine($"YamlService: 使用编码 {_currentEncoding.EncodingName} (代码页: {_currentEncoding.CodePage})");

                // 使用检测到的编码读取文件
                var yamlContent = File.ReadAllText(filePath, _currentEncoding);

                // 记录文件内容预览（用于调试）
                LogFileContentPreview(yamlContent);

                return ParseYamlContent(yamlContent);
            }
            catch (Exception ex)
            {
                // 如果是编码问题，提供更详细的错误信息
                if (IsEncodingError(ex))
                {
                    throw new Exception($"编码问题导致加载失败: {ex.Message}\n" +
                                      $"尝试的编码: {_currentEncoding.EncodingName}\n" +
                                      "请尝试使用GB2312或GBK编码重新加载文件。", ex);
                }
                throw new Exception($"加载YAML文件失败: {ex.Message}", ex);
            }
        }

        // 改进的编码检测方法，特别加强GB2312支持
        private Encoding DetectFileEncodingWithFallback(string filePath)
        {
            try
            {
                // 方法1: 检测BOM标记
                var encodingByBom = DetectEncodingByBom(filePath);
                if (encodingByBom != null)
                {
                    Console.WriteLine($"通过BOM检测到编码: {encodingByBom.EncodingName}");
                    return encodingByBom;
                }

                // 方法2: 尝试常见的中文编码
                var chineseEncodings = new Encoding[]
                {
                    Encoding.GetEncoding("GB2312"), // 优先尝试GB2312
                    Encoding.GetEncoding("GBK"),
                    Encoding.GetEncoding("GB18030"),
                    Encoding.UTF8,
                    Encoding.GetEncoding("Big5")
                };

                // 读取文件前1KB内容进行检测
                var fileBytes = ReadFileHeader(filePath, 1024);
                var fileContentSample = Encoding.ASCII.GetString(fileBytes); // 先用ASCII读取避免异常

                foreach (var encoding in chineseEncodings)
                {
                    try
                    {
                        var testContent = encoding.GetString(fileBytes);
                        if (IsValidChineseText(testContent))
                        {
                            Console.WriteLine($"通过内容分析检测到编码: {encoding.EncodingName}");
                            return encoding;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                // 方法3: 统计字符分布（更精确的检测）
                var bestEncoding = DetectByCharacterDistribution(fileBytes);
                if (bestEncoding != null)
                {
                    Console.WriteLine($"通过字符分布检测到编码: {bestEncoding.EncodingName}");
                    return bestEncoding;
                }

                // 默认使用GB2312（因为大多数中文文件使用此编码）
                Console.WriteLine("使用默认编码: GB2312");
                return Encoding.GetEncoding("GB2312");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编码检测失败: {ex.Message}, 使用默认编码GB2312");
                return Encoding.GetEncoding("GB2312");
            }
        }

        // 通过BOM检测编码
        private Encoding DetectEncodingByBom(string filePath)
        {
            var bom = new byte[4];
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode;
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode;
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;

            return null;
        }

        // 通过字符分布检测编码
        private Encoding DetectByCharacterDistribution(byte[] fileBytes)
        {
            var encodings = new Encoding[]
            {
                Encoding.GetEncoding("GB2312"),
                Encoding.GetEncoding("GBK"),
                Encoding.UTF8,
                Encoding.GetEncoding("Big5")
            };

            Encoding bestEncoding = null;
            double bestScore = 0;

            foreach (var encoding in encodings)
            {
                try
                {
                    var content = encoding.GetString(fileBytes);
                    var score = CalculateEncodingConfidence(content, encoding);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestEncoding = encoding;
                    }

                    Console.WriteLine($"编码 {encoding.EncodingName} 置信度: {score:P2}");
                }
                catch
                {
                    continue;
                }
            }

            return bestScore > 0.5 ? bestEncoding : null;
        }

        // 计算编码置信度
        private double CalculateEncodingConfidence(string content, Encoding encoding)
        {
            if (string.IsNullOrEmpty(content)) return 0;

            double score = 0;
            int validCharCount = 0;
            int totalCharCount = Math.Min(content.Length, 500);

            for (int i = 0; i < totalCharCount; i++)
            {
                char c = content[i];

                // 检查是否在常见的中文字符范围内
                if (c >= 0x4e00 && c <= 0x9fff) // 基本CJK汉字
                {
                    score += 2.0;
                    validCharCount++;
                }
                else if (c >= 0x3400 && c <= 0x4dbf) // 扩展A
                {
                    score += 1.5;
                    validCharCount++;
                }
                else if (c >= 0x20000 && c <= 0x2a6df) // 扩展B
                {
                    score += 1.2;
                    validCharCount++;
                }
                else if (IsChinesePunctuation(c))
                {
                    score += 1.0;
                    validCharCount++;
                }
                else if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                {
                    score += 0.1; // 普通字符得分较低
                }
                else if (c == '�' || c == '\0') // 无效字符扣分
                {
                    score -= 5.0;
                }
            }

            if (validCharCount == 0) return 0;

            // 归一化到0-1范围
            double normalizedScore = score / (totalCharCount * 2.0);
            return Math.Max(0, Math.Min(1, normalizedScore));
        }

        // 检查是否中文标点符号
        private bool IsChinesePunctuation(char c)
        {
            var chinesePunctuations = new[] { '。', '，', '！', '？', '；', '：', '「', '」', '『', '』', '（', '）', '【', '】' };
            return chinesePunctuations.Contains(c);
        }

        // 读取文件头部
        private byte[] ReadFileHeader(string filePath, int maxLength)
        {
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[Math.Min(maxLength, file.Length)];
                file.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        // 检查是否是有效的文本
        private bool IsValidChineseText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            int chineseCharCount = 0;
            int totalCharCount = Math.Min(text.Length, 1000);

            for (int i = 0; i < totalCharCount; i++)
            {
                char c = text[i];

                if (c >= 0x4e00 && c <= 0x9fff) // 基本CJK汉字
                {
                    chineseCharCount++;
                }

                // 如果找到无效字符，立即返回false
                if (c == '�' || c == '\0')
                {
                    return false;
                }

                // 如果找到足够的中文字符，认为是有效文本
                if (chineseCharCount >= 3) return true;
            }

            return chineseCharCount > 0;
        }

        // 检查是否是编码相关的错误
        private bool IsEncodingError(Exception ex)
        {
            var errorLower = ex.Message.ToLower();
            var encodingKeywords = new[] { "encoding", "code page", "gb2312", "gbk", "utf", "decode", "编码", "代码页" };
            return encodingKeywords.Any(keyword => errorLower.Contains(keyword));
        }

        // 记录文件内容预览
        private void LogFileContentPreview(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("文件内容为空");
                return;
            }

            var previewLength = Math.Min(200, content.Length);
            var preview = content.Substring(0, previewLength);

            Console.WriteLine("=== 文件内容预览 ===");
            Console.WriteLine(preview);
            Console.WriteLine("===================");

            // 记录字符编码信息
            Console.WriteLine($"内容长度: {content.Length} 字符");
            Console.WriteLine($"前10个字符的Unicode编码:");
            for (int i = 0; i < Math.Min(10, content.Length); i++)
            {
                Console.WriteLine($"  '{content[i]}' = U+{(int)content[i]:X4}");
            }
        }

        // 保存文件时使用相同的编码
        public void SaveToFile(string filePath, List<Item> items, Encoding encoding = null)
        {
            encoding ??= _currentEncoding; // 使用加载时的编码，如果没有指定则使用当前编码

            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                var yaml = serializer.Serialize(items);

                // 使用指定的编码保存，确保中文正确
                File.WriteAllText(filePath, yaml, encoding);

                Console.WriteLine($"YAML服务: 使用编码 {encoding.EncodingName} 成功保存 {items.Count} 个物品");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YAML服务保存失败: {ex.Message}");
                throw;
            }
        }

        // 新增：带进度显示的YAML解析方法
        public List<Item> ParseYamlContentWithProgress(string yamlContent, ProgressDialog progressDialog)
        {
            var items = new List<Item>();

            try
            {
                progressDialog.UpdateProgress(50, 100, "正在解析YAML数据...");

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                // 使用动态解析以便可以报告进度
                var dynamicData = deserializer.Deserialize<dynamic>(yamlContent);
                progressDialog.UpdateProgress(60, 100, "正在提取物品数据...");

                items = ExtractItemsFromDynamicWithProgress(dynamicData, progressDialog);

                progressDialog.UpdateProgress(80, 100, $"已解析 {items.Count} 个物品");
            }
            catch (Exception ex)
            {
                progressDialog.SetStatus($"解析失败: {ex.Message}");
                throw;
            }

            return items;
        }

        // 新增：带进度显示的动态数据提取
        private List<Item> ExtractItemsFromDynamicWithProgress(dynamic dynamicData, ProgressDialog progressDialog)
        {
            var items = new List<Item>();
            int totalItems = 0;
            int processedItems = 0;

            try
            {
                // 先计算总项目数
                if (dynamicData is IDictionary<object, object> dict)
                {
                    if (dict.ContainsKey("Body"))
                    {
                        var body = dict["Body"];
                        if (body is IEnumerable<object> collection)
                        {
                            totalItems = collection.Count();
                        }
                    }
                    else
                    {
                        totalItems = dict.Count;
                    }
                }
                else if (dynamicData is IEnumerable<object> list)
                {
                    totalItems = list.Count();
                }

                progressDialog.UpdateProgress(65, 100, $"正在处理 {totalItems} 个物品...");

                // 实际提取物品
                if (dynamicData is IDictionary<object, object> dict2)
                {
                    if (dict2.ContainsKey("Body"))
                    {
                        var body = dict2["Body"];
                        if (body is IEnumerable<object> collection)
                        {
                            foreach (var itemData in collection)
                            {
                                var item = ConvertDynamicToItem(itemData);
                                if (item != null && item.Id > 0)
                                {
                                    items.Add(item);
                                }
                                processedItems++;

                                // 每处理10个物品更新一次进度
                                if (processedItems % 10 == 0 || processedItems == totalItems)
                                {
                                    int progress = 65 + (int)((double)processedItems / totalItems * 25);
                                    progressDialog.UpdateProgress(progress, 100,
                                        $"正在处理物品 {processedItems}/{totalItems}...");
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var kvp in dict2)
                        {
                            var item = ConvertDynamicToItem(kvp.Value);
                            if (item != null && item.Id > 0)
                            {
                                items.Add(item);
                            }
                            processedItems++;

                            if (processedItems % 10 == 0 || processedItems == totalItems)
                            {
                                int progress = 65 + (int)((double)processedItems / totalItems * 25);
                                progressDialog.UpdateProgress(progress, 100,
                                    $"正在处理物品 {processedItems}/{totalItems}...");
                            }
                        }
                    }
                }
                else if (dynamicData is IEnumerable<object> list)
                {
                    foreach (var itemData in list)
                    {
                        var item = ConvertDynamicToItem(itemData);
                        if (item != null && item.Id > 0)
                        {
                            items.Add(item);
                        }
                        processedItems++;

                        if (processedItems % 10 == 0 || processedItems == totalItems)
                        {
                            int progress = 65 + (int)((double)processedItems / totalItems * 25);
                            progressDialog.UpdateProgress(progress, 100,
                                $"正在处理物品 {processedItems}/{totalItems}...");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progressDialog.SetStatus($"处理物品数据时出错: {ex.Message}");
                throw;
            }

            return items;
        }

        // 添加文件编码检测方法
        private Encoding DetectFileEncoding(string filePath)
        {
            try
            {
                // 读取文件的前几个字节来检测BOM
                var bom = new byte[4];
                using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    file.Read(bom, 0, 4);
                }

                // 检测BOM标记
                if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
                if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
                if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
                if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
                if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;

                // 如果没有BOM，尝试检测中文编码
                var content = File.ReadAllText(filePath, Encoding.Default);
                if (ContainsChineseCharacters(content))
                {
                    // 尝试常见的中文编码
                    var encodings = new Encoding[]
                    {
                Encoding.UTF8,
                Encoding.GetEncoding("GB2312"),
                Encoding.GetEncoding("GBK"),
                Encoding.GetEncoding("GB18030"),
                Encoding.GetEncoding("Big5") // 繁体中文
                    };

                    foreach (var encoding in encodings)
                    {
                        try
                        {
                            var testContent = File.ReadAllText(filePath, encoding);
                            if (!ContainsInvalidCharacters(testContent))
                            {
                                return encoding;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                return Encoding.UTF8; // 默认使用UTF-8
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        // 辅助方法：检测是否包含中文字符
        private bool ContainsChineseCharacters(string text)
        {
            foreach (char c in text)
            {
                if (c >= 0x4e00 && c <= 0x9fff) // 基本CJK汉字
                    return true;
                if (c >= 0x3400 && c <= 0x4dbf) // 扩展A
                    return true;
                if (c >= 0x20000 && c <= 0x2a6df) // 扩展B
                    return true;
            }
            return false;
        }

        // 辅助方法：检测是否包含无效字符
        private bool ContainsInvalidCharacters(string text)
        {
            return text.Contains("�") || text.Contains("\0") || text.Contains("ÿ");
        }

        private List<Item> ParseYamlContent(string yamlContent)
        {
            // 在方法开始处只定义一次deserializer
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            try
            {
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

                Console.WriteLine("所有解析方法都失败");
                return new List<Item>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YAML解析失败: {ex.Message}");
                return new List<Item>();
            }
        }

        // 添加缺失的SetPropertyFromDynamic方法
        // 修复SetPropertyFromDynamic方法中的变量使用问题
        private void SetPropertyFromDynamic(Item item, System.Reflection.PropertyInfo prop, dynamic dynamicData)
        {
            try
            {
                // 尝试从动态对象中获取值
                var value = GetValueFromDynamic(prop.Name, dynamicData);
                if (value == null) return;

                // 修复intValue变量使用问题
                if (prop.PropertyType == typeof(int))
                {
                    // 先声明变量，再在TryParse中使用
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

        // 添加数据结构类
        public class YamlDocumentStructure
        {
            public YamlHeader Header { get; set; }
            public List<Item> Body { get; set; } = new List<Item>();
        }

        public class YamlHeader
        {
            public string Type { get; set; } = string.Empty;
            public int Version { get; set; }
        }

        // 添加动态解析方法
        private List<Item> ExtractItemsFromDynamic(dynamic dynamicData)
        {
            var items = new List<Item>();

            try
            {
                if (dynamicData is IDictionary<object, object> dict)
                {
                    // 查找Body字段
                    if (dict.ContainsKey("Body"))
                    {
                        var body = dict["Body"];
                        if (body is IEnumerable<object> collection)
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
                        foreach (var kvp in dict)
                        {
                            var item = ConvertDynamicToItem(kvp.Value);
                            if (item != null && item.Id > 0)
                            {
                                items.Add(item);
                            }
                        }
                    }
                }
                else if (dynamicData is IEnumerable<object> list)
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

                if (dynamicData is IDictionary<object, object> dict)
                {
                    foreach (var kvp in dict)
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

        // YamlService.cs - 完整的SetPropertyFromDictionary方法
        private void SetPropertyFromDictionary(Item item, string key, object value)
        {
            try
            {
                switch (key.ToLower())
                {
                    // ========== 基本信息 ==========
                    case "id":
                        int id;
                        if (int.TryParse(value?.ToString(), out id))
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

                    // ========== 属性信息 ==========
                    case "buy":
                        int buy;
                        if (int.TryParse(value?.ToString(), out buy))
                            item.Buy = buy;
                        break;
                    case "weight":
                        int weight;
                        if (int.TryParse(value?.ToString(), out weight))
                            item.Weight = weight;
                        break;
                    case "attack":
                        int attack;
                        if (int.TryParse(value?.ToString(), out attack))
                            item.Attack = attack;
                        break;
                    case "magicattack":
                        int magicAttack;
                        if (int.TryParse(value?.ToString(), out magicAttack))
                            item.MagicAttack = magicAttack;
                        break;
                    case "range":
                        int range;
                        if (int.TryParse(value?.ToString(), out range))
                            item.Range = range;
                        break;
                    case "slots":
                        int slots;
                        if (int.TryParse(value?.ToString(), out slots))
                            item.Slots = slots;
                        break;
                    case "weaponlevel":
                        int weaponLevel;
                        if (int.TryParse(value?.ToString(), out weaponLevel))
                            item.WeaponLevel = weaponLevel;
                        break;
                    case "equiplevelmin":
                        int equipLevelMin;
                        if (int.TryParse(value?.ToString(), out equipLevelMin))
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

                    // ========== 职业限制 ==========
                    case "jobs":
                        if (value is IDictionary<object, object> jobsDict)
                        {
                            item.Jobs = ParseJobRequirements(jobsDict);
                        }
                        break;

                    // ========== 装备位置 ==========
                    case "locations":
                        if (value is IDictionary<object, object> locationsDict)
                        {
                            item.Locations = ParseLocationRequirements(locationsDict);
                        }
                        break;

                    // ========== 脚本 ==========
                    case "script":
                        item.Script = value?.ToString() ?? "";
                        break;

                    case "equipscript":
                        item.EquipScript = value?.ToString() ?? "";
                        break;

                    case "unequipscript":
                        item.UnEquipScript = value?.ToString() ?? "";
                        break;

                    // ========== 交易限制 ==========
                    case "trade":
                        if (value is IDictionary<object, object> tradeDict)
                        {
                            item.Trade = ParseTradeRestrictions(tradeDict);
                        }
                        break;

                    // ========== 其他可能的属性 ==========
                    case "defense":
                        int defense;
                        if (int.TryParse(value?.ToString(), out defense))
                        {
                            // 如果Item类有Defense属性，可以在这里设置
                            // item.Defense = defense;
                        }
                        break;

                    case "sell":
                        int sell;
                        if (int.TryParse(value?.ToString(), out sell))
                        {
                            // 如果Item类有Sell属性，可以在这里设置
                            // item.Sell = sell;
                        }
                        break;

                    default:
                        // 记录未知属性以便调试
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
        private JobRequirements ParseJobRequirements(IDictionary<object, object> jobsDict)
        {
            var jobs = new JobRequirements();

            foreach (var kvp in jobsDict)
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

        private LocationRequirements ParseLocationRequirements(IDictionary<object, object> locationsDict)
        {
            var locations = new LocationRequirements();

            foreach (var kvp in locationsDict)
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

        private TradeRestrictions ParseTradeRestrictions(IDictionary<object, object> tradeDict)
        {
            var trade = new TradeRestrictions();

            foreach (var kvp in tradeDict)
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
    }
}