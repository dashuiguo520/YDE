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

        // 清空数据
        public void ClearData()
        {
            _items.Clear();
        }

        // 加载文件并返回物品列表
        public List<Item> LoadFromFile(string filePath, Encoding encoding = null)
        {
            try
            {
                var items = _yamlService.LoadFromFile(filePath, encoding);
                _items.AddRange(items);
                Console.WriteLine($"数据服务: 成功加载 {items.Count} 个物品");
                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据服务加载失败: {ex.Message}");
                throw;
            }
        }

        public (List<Item> items, List<string> importPaths) LoadStructuredFile(string filePath, Encoding encoding = null)
        {
            try
            {
                var result = _yamlService.LoadStructuredFile(filePath, encoding, out List<string> importPaths);
                return (result, importPaths);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据服务加载结构化文件失败: {ex.Message}");
                return (new List<Item>(), new List<string>());
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

        // 在 DataService 类中
        public List<string> GetTypeFilters()
        {
            var types = _items.Where(i => !string.IsNullOrEmpty(i.Type))
                             .Select(i => i.Type)
                             .Distinct()
                             .OrderBy(t => t)
                             .ToList();
            return types;
        }

        public List<string> GetSubTypeFilters()
        {
            var subTypes = _items.Where(i => !string.IsNullOrEmpty(i.SubType))
                                .Select(i => i.SubType)
                                .Distinct()
                                .OrderBy(st => st)
                                .ToList();
            return subTypes;
        }
    }
}