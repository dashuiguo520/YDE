// Services/DebugService.cs
using System;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YamlDataEditor.Services
{
    public static class DebugService
    {
        // 添加缺失的AnalyzeYamlStructure方法
        public static void AnalyzeYamlStructure(string filePath)
        {
            try
            {
                var yamlContent = File.ReadAllText(filePath, Encoding.UTF8);
                Console.WriteLine("=== YAML文件内容 ===");
                Console.WriteLine(yamlContent);
                Console.WriteLine("====================");

                // 使用YamlStream进行低级解析
                using var reader = new StringReader(yamlContent);
                var stream = new YamlStream();
                stream.Load(reader);

                Console.WriteLine($"文档数量: {stream.Documents.Count}");

                foreach (var document in stream.Documents)
                {
                    AnalyzeYamlNode(document.RootNode, 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分析失败: {ex.Message}");
            }
        }

        private static void AnalyzeYamlNode(YamlNode node, int depth)
        {
            var indent = new string(' ', depth * 2);

            if (node is YamlScalarNode scalarNode)
            {
                Console.WriteLine($"{indent}标量: {scalarNode.Value}");
            }
            else if (node is YamlMappingNode mappingNode)
            {
                Console.WriteLine($"{indent}映射:");
                foreach (var child in mappingNode.Children)
                {
                    var key = (YamlScalarNode)child.Key;
                    Console.WriteLine($"{indent}  键: {key.Value}");
                    AnalyzeYamlNode(child.Value, depth + 2);
                }
            }
            else if (node is YamlSequenceNode sequenceNode)
            {
                Console.WriteLine($"{indent}序列:");
                int index = 0;
                foreach (var child in sequenceNode.Children)
                {
                    Console.WriteLine($"{indent}  项[{index}]:");
                    AnalyzeYamlNode(child, depth + 2);
                    index++;
                }
            }
        }

        // 添加新的测试方法
        public static void TestYamlParsing(string filePath)
        {
            try
            {
                var yamlContent = File.ReadAllText(filePath, Encoding.UTF8);
                Console.WriteLine("=== 测试YAML解析 ===");

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                // 尝试解析为动态对象分析结构
                var dynamicData = deserializer.Deserialize<dynamic>(yamlContent);
                Console.WriteLine($"动态对象类型: {dynamicData?.GetType()}");

                // 分析结构
                AnalyzeDynamicStructure(dynamicData, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试解析失败: {ex.Message}");
            }
        }

        private static void AnalyzeDynamicStructure(dynamic data, int depth)
        {
            if (data == null) return;

            var indent = new string(' ', depth * 2);

            try
            {
                if (data is System.Collections.IDictionary dict)
                {
                    Console.WriteLine($"{indent}字典 (Count: {dict.Count}):");
                    foreach (var key in dict.Keys)
                    {
                        Console.WriteLine($"{indent}  Key: {key}");
                        AnalyzeDynamicStructure(dict[key], depth + 2);
                    }
                }
                else if (data is System.Collections.IEnumerable list && !(data is string))
                {
                    Console.WriteLine($"{indent}列表:");
                    int index = 0;
                    foreach (var item in list)
                    {
                        Console.WriteLine($"{indent}  Item[{index}]:");
                        AnalyzeDynamicStructure(item, depth + 2);
                        index++;
                    }
                }
                else
                {
                    Console.WriteLine($"{indent}值: {data} (类型: {data?.GetType()})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{indent}分析错误: {ex.Message}");
            }
        }

        public static void SaveYamlSample(string filePath)
        {
            var sampleYaml = @"# YAML格式示例
# 格式1: 直接列表
- id: 501
  aegisName: Red_Potion
  name: 红色药水
  type: Healing
  subType: Potion
  buy: 10
  weight: 7

- id: 502
  aegisName: Orange_Potion
  name: 橙色药水
  type: Healing
  subType: Potion
  buy: 30
  weight: 7

# 格式2: 字典格式
item_dict:
  503:
    aegisName: Yellow_Potion
    name: 黄色药水
    type: Healing
    subType: Potion
    buy: 50
  504:
    aegisName: White_Potion
    name: 白色药水
    type: Healing
    subType: Potion
    buy: 100

# 格式3: 包装格式
items:
  - id: 505
    aegisName: Blue_Potion
    name: 蓝色药水
    type: Healing
    subType: Potion
    buy: 200
  - id: 506
    aegisName: Green_Potion
    name: 绿色药水
    type: Healing
    subType: Potion
    buy: 300";

            try
            {
                File.WriteAllText(filePath, sampleYaml, Encoding.UTF8);
                Console.WriteLine($"示例YAML文件已保存到: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存示例文件失败: {ex.Message}");
            }
        }
    }
}