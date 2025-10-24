// Services/DataService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDataEditor.Models;

namespace YamlDataEditor.Services
{
    public class DataService
    {
        private List<Item> _items = new List<Item>();
        private readonly YamlService _yamlService = new YamlService();

        public void SetItems(List<Item> items)
        {
            _items = items ?? new List<Item>();
        }

        // DataService.cs - 修改LoadData方法
        public void LoadData(string filePath, Encoding encoding = null)
        {
            try
            {
                _items = _yamlService.LoadFromFile(filePath, encoding);
                Console.WriteLine($"数据服务: 成功加载 {_items.Count} 个物品");

                // 验证加载的数据
                if (_items.Count > 0)
                {
                    var firstItem = _items.First();
                    Console.WriteLine($"第一个物品: ID={firstItem.Id}, Name={firstItem.Name}, AegisName={firstItem.AegisName}");

                    // 检查中文显示
                    if (!string.IsNullOrEmpty(firstItem.Name))
                    {
                        Console.WriteLine($"名称显示测试: '{firstItem.Name}'");
                        foreach (char c in firstItem.Name)
                        {
                            Console.WriteLine($"字符: {c} (Unicode: {(int)c:X4})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据服务加载失败: {ex.Message}");
                _items = new List<Item>();
                throw;
            }
        }

        public void SaveData(string filePath)
        {
            try
            {
                _yamlService.SaveToFile(filePath, _items);
                Console.WriteLine($"数据服务: 成功保存 {_items.Count} 个物品到 {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据服务保存失败: {ex.Message}");
                throw;
            }
        }

        public List<Item> GetItems()
        {
            return _items;
        }

        public void AddItem(Item item)
        {
            _items.Add(item);
        }

        public void RemoveItem(Item item)
        {
            _items.Remove(item);
        }

        public List<Item> FilterItems(string searchText, string typeFilter, string subTypeFilter)
        {
            var query = _items.AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(i =>
                    (i.Name != null && i.Name.Contains(searchText)) ||
                    (i.AegisName != null && i.AegisName.Contains(searchText)));
            }

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "全部")
                query = query.Where(i => i.Type == typeFilter);

            if (!string.IsNullOrEmpty(subTypeFilter) && subTypeFilter != "全部")
                query = query.Where(i => i.SubType == subTypeFilter);

            return query.ToList();
        }

        public List<string> GetTypeFilters()
        {
            var types = _items.Where(i => !string.IsNullOrEmpty(i.Type))
                             .Select(i => i.Type)
                             .Distinct()
                             .OrderBy(t => t)
                             .ToList();
            types.Insert(0, "全部");
            return types;
        }

        public List<string> GetSubTypeFilters()
        {
            var subTypes = _items.Where(i => !string.IsNullOrEmpty(i.SubType))
                                 .Select(i => i.SubType)
                                 .Distinct()
                                 .OrderBy(st => st)
                                 .ToList();
            subTypes.Insert(0, "全部");
            return subTypes;
        }
    }
}