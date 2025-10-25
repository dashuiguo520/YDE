using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using YamlDataEditor.Models;
using YamlDataEditor.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static YamlDataEditor.Services.YamlService;

namespace YamlDataEditor.Forms
{
    public partial class MainForm : Form
    {
        private DataService _dataService;
        private List<Item> _currentItems;
        private DataGridView dataGridView;
        private ToolStrip toolStrip;
        private TextBox txtSearch;
        private ComboBox cmbType;
        private ComboBox cmbSubType;
        private Button btnFilter;
        private Button btnClearFilter;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel toolStripStatusLabel;
        private ToolStripButton btnLoad;

        // 当前文件路径列表
        private List<string> _currentFilePaths = new List<string>();

        public MainForm()
        {
            InitializeComponent();
            _dataService = new DataService();
            _currentItems = new List<Item>();

            SetupEncodingSupport();
            SetupControls();
            SetupDataGridView();
            SetupToolStrip();

            // 初始化数据网格数据源
            dataGridView.DataSource = new BindingList<Item>(_currentItems);
        }

        private void SetupEncodingSupport()
        {
            try
            {
                // 注册编码提供程序以支持中文编码
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Console.WriteLine("编码支持已初始化");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编码支持初始化失败: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            // 窗体基本设置
            this.SuspendLayout();
            this.Text = "YAML数据编辑器 - .NET 8.0";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 创建控件
            CreateControls();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CreateControls()
        {
            // 搜索面板
            var panelSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = SystemColors.ControlLight,
                Padding = new Padding(10, 10, 10, 0)
            };

            // 搜索文本框
            txtSearch = new TextBox
            {
                Location = new Point(10, 15),
                Size = new Size(200, 23),
                PlaceholderText = "搜索名称或Aegis名称",
                Font = new Font("Microsoft YaHei UI", 9)
            };
            txtSearch.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) ApplyFilter(); };

            // 类型筛选
            cmbType = new ComboBox
            {
                Location = new Point(220, 15),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9)
            };
            cmbType.Items.Add("选择类型");
            cmbType.SelectedIndex = 0;

            // 子类型筛选
            cmbSubType = new ComboBox
            {
                Location = new Point(380, 15),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9)
            };
            cmbSubType.Items.Add("选择子类型");
            cmbSubType.SelectedIndex = 0;

            // 筛选按钮
            btnFilter = new Button
            {
                Location = new Point(540, 14),
                Size = new Size(70, 30),
                Text = "筛选",
                Font = new Font("Microsoft YaHei UI", 9)
            };
            btnFilter.Click += (s, e) => ApplyFilter();

            // 清除筛选按钮
            btnClearFilter = new Button
            {
                Location = new Point(620, 14),
                Size = new Size(70, 30),
                Text = "清除",
                Font = new Font("Microsoft YaHei UI", 9)
            };
            btnClearFilter.Click += (s, e) => ClearFilter();

            panelSearch.Controls.AddRange(new Control[] {
                txtSearch,
                cmbType,
                cmbSubType,
                btnFilter,
                btnClearFilter
            });

            // 工具栏
            toolStrip = new ToolStrip { Dock = DockStyle.Top };

            // 数据网格
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Microsoft YaHei UI", 9)
            };
            dataGridView.CellDoubleClick += DataGridView_CellDoubleClick;

            statusStrip = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel { Text = "就绪" };
            statusStrip.Items.Add(toolStripStatusLabel);
            statusStrip.Dock = DockStyle.Bottom;

            // 添加到窗体
            this.Controls.Add(dataGridView);
            this.Controls.Add(panelSearch);
            this.Controls.Add(toolStrip);
            this.Controls.Add(statusStrip);
        }

        private void SetupControls()
        {
            // 初始化筛选下拉框
            cmbType.Items.Add("全部");
            cmbSubType.Items.Add("全部");
            cmbType.SelectedIndex = 0;
            cmbSubType.SelectedIndex = 0;
        }

        // 修改SetupToolStrip方法，移除打开和另存为，增加加载按钮
        private void SetupToolStrip()
        {
            // 移除打开和另存为按钮，增加加载按钮
            btnLoad = new ToolStripButton("加载");
            btnLoad.Click += LoadButton_Click;

            var addButton = new ToolStripButton("添加");
            addButton.Click += AddButton_Click;

            var deleteButton = new ToolStripButton("删除");
            deleteButton.Click += DeleteButton_Click;

            var refreshButton = new ToolStripButton("刷新");
            refreshButton.Click += RefreshButton_Click;

            toolStrip.Items.Add(btnLoad);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(addButton);
            toolStrip.Items.Add(deleteButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(refreshButton);
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                toolStripStatusLabel.Text = "正在加载数据...";

                // 从设置文件获取数据库路径
                string databasePath = GetDatabasePathFromSettings();

                if (string.IsNullOrEmpty(databasePath) || !Directory.Exists(databasePath))
                {
                    MessageBox.Show("数据库路径未设置或不存在，请先配置系统设置", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    toolStripStatusLabel.Text = "加载失败：路径未设置";
                    return;
                }

                // 查找并加载所有相关的YAML文件
                LoadAllItemFiles(databasePath);

                // 更新筛选下拉框
                UpdateFilterComboBoxes();

                // 刷新数据网格显示
                dataGridView.Refresh();

                toolStripStatusLabel.Text = $"成功加载 {_currentItems.Count} 个物品，来自 {_currentFilePaths.Count} 个文件";
                MessageBox.Show($"成功加载 {_currentItems.Count} 个物品，来自 {_currentFilePaths.Count} 个文件", "成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 调试信息
                if (_currentItems.Count > 0)
                {
                    Console.WriteLine("=== 加载的物品详情 ===");
                    foreach (var item in _currentItems.Take(5))
                    {
                        Console.WriteLine($"ID: {item.Id}, AegisName: {item.AegisName}, Name: {item.Name}");
                    }
                    Console.WriteLine("... (更多物品已加载)");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"加载文件失败:\n\n错误类型: {ex.GetType().Name}\n错误信息: {ex.Message}";

                if (ex.InnerException != null)
                {
                    errorMessage += $"\n内部错误: {ex.InnerException.Message}";
                }

                MessageBox.Show(errorMessage, "加载错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                toolStripStatusLabel.Text = "加载失败";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private string GetDatabasePathFromSettings()
        {
            string settingsPath = Path.Combine(Application.StartupPath, "editor_settings.config");

            if (!File.Exists(settingsPath))
                return null;

            try
            {
                var lines = File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2 && parts[0] == "DatabasePath")
                    {
                        return parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取设置文件失败: {ex.Message}");
            }

            return null;
        }

        private void LoadAllItemFiles(string databasePath)
        {
            _currentItems.Clear();
            _currentFilePaths.Clear();
            _dataService.ClearData();

            string mainFilePath = Path.Combine(databasePath, "item_db.yml");

            Console.WriteLine($"数据库路径: {databasePath}");
            Console.WriteLine($"主文件路径: {mainFilePath}");
            Console.WriteLine($"主文件存在: {File.Exists(mainFilePath)}");

            if (!File.Exists(mainFilePath))
            {
                MessageBox.Show($"找不到主文件: {mainFilePath}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Encoding encoding = GetEncodingFromSettings();

                // 尝试使用新的结构化加载方法
                var (mainItems, importPaths) = _dataService.LoadStructuredFile(mainFilePath, encoding);

                if (mainItems.Count > 0)
                {
                    // 主文件包含物品，添加到当前列表
                    _currentItems.AddRange(mainItems);
                    _currentFilePaths.Add(mainFilePath);
                    Console.WriteLine($"从主文件加载了 {mainItems.Count} 个物品");
                }
                else
                {
                    Console.WriteLine("主文件不包含物品数据，仅包含导入配置");
                    // 不将主文件添加到文件路径列表，因为它不包含实际物品
                }

                // 加载导入文件
                int totalImportItems = 0;
                foreach (var importPath in importPaths)
                {
                    string fullImportPath = ResolveImportPath(importPath, databasePath);
                    if (File.Exists(fullImportPath))
                    {
                        var importItems = _dataService.LoadFromFile(fullImportPath, encoding);
                        _currentItems.AddRange(importItems);
                        _currentFilePaths.Add(fullImportPath);
                        totalImportItems += importItems.Count;
                        Console.WriteLine($"从导入文件 {fullImportPath} 加载了 {importItems.Count} 个物品");
                    }
                    else
                    {
                        Console.WriteLine($"导入文件不存在: {fullImportPath}");
                    }
                }

                Console.WriteLine($"从导入文件总共加载了 {totalImportItems} 个物品");

                // 更新数据网格
                UpdateDataGridView();

                // 更新状态栏
                toolStripStatusLabel.Text = $"已加载 {_currentItems.Count} 个物品，来自 {_currentFilePaths.Count} 个文件";

                Console.WriteLine($"最终加载物品数: {_currentItems.Count}");
                Console.WriteLine($"加载文件数: {_currentFilePaths.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载失败: {ex.Message}");
                MessageBox.Show($"加载文件失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDataGridView()
        {
            // 确保在UI线程上执行
            if (dataGridView.InvokeRequired)
            {
                dataGridView.Invoke(new Action(UpdateDataGridView));
                return;
            }

            // 保存当前选择状态（如果有）
            int selectedIndex = dataGridView.CurrentRow?.Index ?? -1;

            // 暂停绘制以提高性能
            dataGridView.SuspendLayout();

            try
            {
                // 重新绑定数据源
                dataGridView.DataSource = null;
                dataGridView.DataSource = new BindingList<Item>(_currentItems);

                // 恢复选择状态（如果有）
                if (selectedIndex >= 0 && selectedIndex < dataGridView.Rows.Count)
                {
                    dataGridView.Rows[selectedIndex].Selected = true;
                    dataGridView.FirstDisplayedScrollingRowIndex = selectedIndex;
                }

                // 更新状态栏
                toolStripStatusLabel.Text = $"已加载 {_currentItems.Count} 个物品";

                Console.WriteLine($"数据网格已更新，显示 {_currentItems.Count} 个物品");
            }
            finally
            {
                // 恢复绘制
                dataGridView.ResumeLayout();
            }
        }

        private void ParseFooterAndImports(YamlDotNet.RepresentationModel.YamlNode rootNode, string databasePath)
        {
            try
            {
                if (rootNode is YamlDotNet.RepresentationModel.YamlMappingNode mappingNode)
                {
                    Console.WriteLine("开始解析Footer和Imports...");

                    // 检查Body是否存在
                    var bodyKey = new YamlDotNet.RepresentationModel.YamlScalarNode("Body");
                    bool hasBody = mappingNode.Children.ContainsKey(bodyKey);
                    Console.WriteLine($"Body存在: {hasBody}");

                    var footerKey = new YamlDotNet.RepresentationModel.YamlScalarNode("Footer");
                    if (mappingNode.Children.ContainsKey(footerKey))
                    {
                        Console.WriteLine("找到Footer节点");
                        var footerNode = mappingNode.Children[footerKey];

                        if (footerNode is YamlDotNet.RepresentationModel.YamlMappingNode footerMapping)
                        {
                            var importsKey = new YamlDotNet.RepresentationModel.YamlScalarNode("Imports");
                            if (footerMapping.Children.ContainsKey(importsKey))
                            {
                                var importsNode = footerMapping.Children[importsKey];

                                if (importsNode is YamlDotNet.RepresentationModel.YamlSequenceNode importsSequence)
                                {
                                    Console.WriteLine($"找到 {importsSequence.Children.Count} 个导入项");

                                    foreach (var importItem in importsSequence.Children)
                                    {
                                        if (importItem is YamlDotNet.RepresentationModel.YamlMappingNode importMapping)
                                        {
                                            var pathKey = new YamlDotNet.RepresentationModel.YamlScalarNode("Path");
                                            if (importMapping.Children.ContainsKey(pathKey))
                                            {
                                                var pathNode = importMapping.Children[pathKey];
                                                string importPath = pathNode.ToString();

                                                string fullImportPath = ResolveImportPath(importPath, databasePath);

                                                Console.WriteLine($"导入路径: {importPath} -> {fullImportPath}");
                                                Console.WriteLine($"文件存在: {File.Exists(fullImportPath)}");

                                                if (File.Exists(fullImportPath))
                                                {
                                                    // 检查是否已经加载过（避免重复）
                                                    if (!_currentFilePaths.Contains(fullImportPath))
                                                    {
                                                        LoadItemsFromFile(fullImportPath);
                                                        _currentFilePaths.Add(fullImportPath);
                                                        Console.WriteLine($"成功加载导入文件: {fullImportPath}");
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"文件已加载，跳过: {fullImportPath}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"导入文件不存在: {fullImportPath}");
                                                    TryAlternativePaths(importPath, databasePath);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("未找到Footer节点");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析Footer和Imports时出错: {ex.Message}");
            }
        }

        private string ResolveImportPath(string importPath, string databasePath)
        {
            // 如果已经是绝对路径，直接返回
            if (Path.IsPathRooted(importPath))
                return importPath;

            // 方案1：直接组合（当前逻辑）
            string path1 = Path.Combine(databasePath, importPath);
            if (File.Exists(path1))
                return path1;

            // 方案2：从databasePath的父目录开始组合（处理db/re/item_db.yml情况）
            string parentPath = Directory.GetParent(databasePath)?.FullName;
            if (parentPath != null)
            {
                string path2 = Path.Combine(parentPath, importPath);
                if (File.Exists(path2))
                    return path2;
            }

            // 方案3：从项目根目录开始（假设databasePath是子目录）
            string projectRoot = FindProjectRoot(databasePath);
            if (projectRoot != null)
            {
                string path3 = Path.Combine(projectRoot, importPath);
                if (File.Exists(path3))
                    return path3;
            }

            // 方案4：只使用文件名，在databasePath中查找
            string fileName = Path.GetFileName(importPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                string path4 = Path.Combine(databasePath, fileName);
                if (File.Exists(path4))
                    return path4;
            }

            // 默认返回方案1（即使文件不存在）
            return path1;
        }

        private string FindProjectRoot(string currentPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(currentPath);
                while (dir != null)
                {
                    // 查找包含 db 目录的根目录
                    if (Directory.Exists(Path.Combine(dir.FullName, "db")))
                        return dir.FullName;

                    dir = dir.Parent;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找项目根目录失败: {ex.Message}");
            }

            return null;
        }

        private void TryAlternativePaths(string importPath, string databasePath)
        {
            Console.WriteLine("尝试替代路径...");

            // 只使用文件名搜索
            string fileName = Path.GetFileName(importPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                // 在当前数据库路径搜索
                string[] files = Directory.GetFiles(databasePath, fileName, SearchOption.AllDirectories);
                foreach (string file in files.Take(3)) // 只显示前3个结果
                {
                    Console.WriteLine($"找到可能文件: {file}");
                }

                if (files.Length > 0)
                {
                    Console.WriteLine($"建议使用: {files[0]}");
                }
            }

            // 显示目录结构用于诊断
            Console.WriteLine("当前数据库路径内容:");
            try
            {
                var dirs = Directory.GetDirectories(databasePath);
                var files = Directory.GetFiles(databasePath);

                Console.WriteLine("目录:");
                foreach (var dir in dirs.Take(5))
                {
                    Console.WriteLine($"  {Path.GetFileName(dir)}");
                }

                Console.WriteLine("文件:");
                foreach (var file in files.Take(5))
                {
                    Console.WriteLine($"  {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法读取目录内容: {ex.Message}");
            }
        }

        private void LoadItemsFromFile(string filePath)
        {
            try
            {
                // 从设置文件获取编码
                Encoding encoding = GetEncodingFromSettings();

                var items = _dataService.LoadFromFile(filePath, encoding);
                _currentItems.AddRange(items);

                Console.WriteLine($"从 {filePath} 加载了 {items.Count} 个物品，使用编码: {encoding.EncodingName}");
                UpdateDataGridView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载文件 {filePath} 失败: {ex.Message}");
                MessageBox.Show($"加载文件 {filePath} 失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private Encoding GetEncodingFromSettings()
        {
            string settingsPath = Path.Combine(Application.StartupPath, "editor_settings.config");

            if (!File.Exists(settingsPath))
                return Encoding.UTF8; // 默认使用UTF-8

            try
            {
                var lines = File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2 && parts[0] == "Encoding")
                    {
                        if (int.TryParse(parts[1], out int encodingIndex))
                        {
                            return GetEncodingFromIndex(encodingIndex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取编码设置失败: {ex.Message}");
            }

            return Encoding.UTF8; // 默认使用UTF-8
        }

        private Encoding GetEncodingFromIndex(int index)
        {
            switch (index)
            {
                case 0: return Encoding.UTF8;
                case 1: return new UTF8Encoding(true);
                case 2: return Encoding.GetEncoding("GB2312");
                case 3: return Encoding.GetEncoding("GBK");
                case 4: return Encoding.GetEncoding("GB18030");
                default: return Encoding.UTF8;
            }
        }

        // 修复数据网格设置
        private void SetupDataGridView()
        {
            dataGridView.AutoGenerateColumns = false;
            dataGridView.ReadOnly = true;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // 确保DataGridView使用正确的字体显示中文
            dataGridView.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9);
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);

            // 清空现有列
            dataGridView.Columns.Clear();

            // 添加列并设置数据绑定
            AddColumn("Id", "ID", 60);
            AddColumn("AegisName", "Aegis名称", 120);
            AddColumn("Name", "名称", 150);
            AddColumn("Type", "类型", 100);
            AddColumn("SubType", "子类型", 100);
            AddColumn("Buy", "购买价格", 100);
            AddColumn("Weight", "重量", 80);
        }

        private void AddColumn(string dataPropertyName, string headerText, int width)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = dataPropertyName,
                HeaderText = headerText,
                Width = width,
                ReadOnly = true
            };
            dataGridView.Columns.Add(column);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var newItem = new Item();
            using (var editor = new ItemEditorForm(newItem, "添加新物品"))
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    _dataService.AddItem(editor.EditedItem);
                    _currentItems = _dataService.GetItems();
                    dataGridView.DataSource = new BindingList<Item>(_currentItems);
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow?.DataBoundItem is Item selectedItem)
            {
                var result = MessageBox.Show($"确定要删除物品 '{selectedItem.Name}' 吗？",
                    "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _dataService.RemoveItem(selectedItem);
                    _currentItems = _dataService.GetItems();
                    dataGridView.DataSource = new BindingList<Item>(_currentItems);
                }
            }
            else
            {
                MessageBox.Show("请先选择一个物品", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            _currentItems = _dataService.GetItems();
            dataGridView.DataSource = new BindingList<Item>(_currentItems);
        }

        private void DataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView.Rows[e.RowIndex].DataBoundItem is Item item)
            {
                using (var editor = new ItemEditorForm(item, "编辑物品"))
                {
                    if (editor.ShowDialog() == DialogResult.OK)
                    {
                        // 更新数据
                        var index = _currentItems.IndexOf(item);
                        if (index >= 0)
                        {
                            _currentItems[index] = editor.EditedItem;
                            dataGridView.InvalidateRow(e.RowIndex);
                        }
                    }
                }
            }
        }

        private void ApplyFilter()
        {
            try
            {
                string searchText = txtSearch.Text.Trim();

                // 处理类型筛选
                string typeFilter = null;
                if (cmbType.SelectedIndex > 0) // 0是"选择类型"，不筛选
                {
                    typeFilter = cmbType.SelectedItem?.ToString();
                    // 如果选择的是"全部"，则设置为空
                    if (typeFilter == "全部")
                        typeFilter = null;
                }

                // 处理子类型筛选
                string subTypeFilter = null;
                if (cmbSubType.SelectedIndex > 0) // 0是"选择子类型"，不筛选
                {
                    subTypeFilter = cmbSubType.SelectedItem?.ToString();
                    if (subTypeFilter == "全部")
                        subTypeFilter = null;
                }

                Console.WriteLine($"应用筛选: 搜索='{searchText}', 类型='{typeFilter}', 子类型='{subTypeFilter}'");

                _currentItems = _dataService.FilterItems(searchText, typeFilter, subTypeFilter);

                // 更新数据网格
                dataGridView.DataSource = new BindingList<Item>(_currentItems);

                // 更新状态栏
                toolStripStatusLabel.Text = $"显示 {_currentItems.Count} 个物品（已筛选）";

                Console.WriteLine($"筛选完成: 显示 {_currentItems.Count} 个物品");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"筛选失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFilter()
        {
            txtSearch.Text = string.Empty;

            // 重置为提示文本
            cmbType.SelectedIndex = 0; // "选择类型"
            cmbSubType.SelectedIndex = 0; // "选择子类型"

            // 重新加载所有物品
            _currentItems = _dataService.GetItems();
            dataGridView.DataSource = new BindingList<Item>(_currentItems);

            // 更新状态栏
            toolStripStatusLabel.Text = $"显示所有 {_currentItems.Count} 个物品";

            Console.WriteLine("筛选已清除");
        }

        private void UpdateFilterComboBoxes()
        {
            var currentType = cmbType.SelectedItem?.ToString();
            var currentSubType = cmbSubType.SelectedItem?.ToString();

            // 保存当前是否选择了提示文本
            bool wasTypeHint = currentType == "选择类型" || currentType == "全部";
            bool wasSubTypeHint = currentSubType == "选择子类型" || currentSubType == "全部";

            // 获取筛选数据
            var typeFilters = _dataService.GetTypeFilters();
            var subTypeFilters = _dataService.GetSubTypeFilters();

            // 更新类型下拉框
            cmbType.Items.Clear();

            // 添加提示文本选项
            cmbType.Items.Add("选择类型");

            // 添加筛选数据
            cmbType.Items.AddRange(typeFilters.ToArray());

            // 更新子类型下拉框
            cmbSubType.Items.Clear();

            // 添加提示文本选项
            cmbSubType.Items.Add("选择子类型");

            // 添加筛选数据
            cmbSubType.Items.AddRange(subTypeFilters.ToArray());

            // 恢复选择状态
            if (!string.IsNullOrEmpty(currentType) && cmbType.Items.Contains(currentType))
            {
                cmbType.SelectedItem = currentType;
            }
            else if (wasTypeHint || typeFilters.Count == 0)
            {
                // 如果之前选择的是提示文本，或者没有数据，选择提示文本
                cmbType.SelectedIndex = 0;
            }
            else
            {
                // 否则选择第一个实际的数据项
                cmbType.SelectedIndex = 1; // 跳过提示文本
            }

            if (!string.IsNullOrEmpty(currentSubType) && cmbSubType.Items.Contains(currentSubType))
            {
                cmbSubType.SelectedItem = currentSubType;
            }
            else if (wasSubTypeHint || subTypeFilters.Count == 0)
            {
                cmbSubType.SelectedIndex = 0;
            }
            else
            {
                cmbSubType.SelectedIndex = 1;
            }

            Console.WriteLine($"更新筛选下拉框完成: 类型项数={cmbType.Items.Count}, 子类型项数={cmbSubType.Items.Count}");
        }
    }
}