
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using YamlDataEditor.Models;
using YamlDataEditor.Services;

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

        // 添加当前文件路径字段
        private string _currentFilePath;

        public MainForm()
        {
            InitializeComponent();
            _dataService = new DataService();
            _currentItems = new List<Item>();
            _currentFilePath = string.Empty; // 初始化文件路径

            // 设置编码支持
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
                BackColor = SystemColors.ControlLight
            };

            // 搜索文本框
            txtSearch = new TextBox
            {
                Location = new Point(10, 15),
                Size = new Size(150, 20),
                PlaceholderText = "搜索名称或Aegis名称"
            };
            txtSearch.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) ApplyFilter(); };

            // 类型筛选
            cmbType = new ComboBox
            {
                Location = new Point(170, 15),
                Size = new Size(120, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 子类型筛选
            cmbSubType = new ComboBox
            {
                Location = new Point(300, 15),
                Size = new Size(120, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 筛选按钮
            btnFilter = new Button
            {
                Location = new Point(430, 14),
                Size = new Size(60, 23),
                Text = "筛选"
            };
            btnFilter.Click += (s, e) => ApplyFilter();

            // 清除筛选按钮
            btnClearFilter = new Button
            {
                Location = new Point(500, 14),
                Size = new Size(60, 23),
                Text = "清除"
            };
            btnClearFilter.Click += (s, e) => ClearFilter();

            panelSearch.Controls.AddRange(new Control[] {
                new Label { Text = "搜索:", Location = new Point(10, 0), Size = new Size(40, 13) },
                txtSearch,
                new Label { Text = "类型:", Location = new Point(170, 0), Size = new Size(40, 13) },
                cmbType,
                new Label { Text = "子类型:", Location = new Point(300, 0), Size = new Size(50, 13) },
                cmbSubType,
                btnFilter,
                btnClearFilter
            });

            // 工具栏
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top
            };

            // 数据网格
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
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

        // 修改SetupToolStrip方法，确保有保存按钮
        private void SetupToolStrip()
        {
            var openButton = new ToolStripButton("打开");
            openButton.Click += OpenButton_Click;

            var saveButton = new ToolStripButton("保存");
            saveButton.Click += SaveButton_Click;

            var saveAsButton = new ToolStripButton("另存为");
            saveAsButton.Click += SaveAsButton_Click;

            var addButton = new ToolStripButton("添加");
            addButton.Click += AddButton_Click;

            var deleteButton = new ToolStripButton("删除");
            deleteButton.Click += DeleteButton_Click;

            var refreshButton = new ToolStripButton("刷新");
            refreshButton.Click += RefreshButton_Click;

            var debugButton = new ToolStripButton("调试");
            debugButton.Click += DebugButton_Click;

            toolStrip.Items.Add(openButton);
            toolStrip.Items.Add(saveButton);
            toolStrip.Items.Add(saveAsButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(addButton);
            toolStrip.Items.Add(deleteButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(refreshButton);
            toolStrip.Items.Add(debugButton);
        }

        private void DebugButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "YAML文件|*.yaml;*.yml";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DebugService.AnalyzeYamlStructure(dialog.FileName);
                    DebugService.TestYamlParsing(dialog.FileName); // 新增测试
                }
            }
        }

        private Encoding currentFileEncoding = Encoding.UTF8;
        // Forms/MainForm.cs - 修复数据绑定部分
        private void OpenButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "YAML文件|*.yaml;*.yml|所有文件|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Cursor = Cursors.WaitCursor;

                        // 记录当前文件路径
                        _currentFilePath = dialog.FileName;

                        _dataService.LoadData(dialog.FileName);
                        _currentItems = _dataService.GetItems();

                        // 修复数据绑定
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() =>
                            {
                                dataGridView.DataSource = new BindingList<Item>(_currentItems);
                            }));
                        }
                        else
                        {
                            dataGridView.DataSource = new BindingList<Item>(_currentItems);
                        }

                        // 更新筛选下拉框
                        UpdateFilterComboBoxes();

                        // 刷新数据网格显示
                        dataGridView.Refresh();

                        toolStripStatusLabel.Text = $"成功加载 {_currentItems.Count} 个物品";
                        MessageBox.Show($"成功加载 {_currentItems.Count} 个物品", "成功",
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
            AddColumn("Buy", "出售价格", 80);
            AddColumn("Weight", "重量", 80);
            AddColumn("Attack", "攻击", 80);
            AddColumn("MagicAttack", "魔攻", 80);
            AddColumn("Range", "攻击距离", 80);
            AddColumn("Slots", "插槽", 80);
            AddColumn("WeaponLevel", "武器等级", 80);
            AddColumn("EquipLevelMin", "装备等级", 80);
            AddColumn("Refineable", "可精炼", 80);
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

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取当前显示的项目列表
                var items = _dataService.GetItems();
                if (items == null || items.Count == 0)
                {
                    MessageBox.Show("没有数据可保存", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string filePath = _currentFilePath;

                // 如果没有当前文件路径，显示另存为对话框
                if (string.IsNullOrEmpty(filePath))
                {
                    using (var dialog = new SaveFileDialog())
                    {
                        dialog.Filter = "YAML文件|*.yaml;*.yml|所有文件|*.*";
                        dialog.DefaultExt = "yaml";

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            filePath = dialog.FileName;
                            _currentFilePath = filePath; // 记录新的文件路径
                        }
                        else
                        {
                            return; // 用户取消了保存
                        }
                    }
                }

                // 执行保存
                _dataService.SaveData(filePath);
                MessageBox.Show("保存成功", "成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                toolStripStatusLabel.Text = $"文件已保存到: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabel.Text = "保存失败";
            }
        }

        private void SaveAsButton_Click(object sender, EventArgs e)
        {
            // 直接调用SaveButton_Click，因为逻辑相同
            SaveButton_Click(sender, e);
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
                _currentItems = _dataService.FilterItems(
                    txtSearch.Text.Trim(),
                    cmbType.SelectedItem?.ToString(),
                    cmbSubType.SelectedItem?.ToString());

                dataGridView.DataSource = new BindingList<Item>(_currentItems);
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
            cmbType.SelectedIndex = 0;
            cmbSubType.SelectedIndex = 0;

            _currentItems = _dataService.GetItems();
            dataGridView.DataSource = new BindingList<Item>(_currentItems);
        }

        private void UpdateFilterComboBoxes()
        {
            var currentType = cmbType.SelectedItem?.ToString();
            var currentSubType = cmbSubType.SelectedItem?.ToString();

            cmbType.Items.Clear();
            cmbType.Items.AddRange(_dataService.GetTypeFilters().ToArray());

            cmbSubType.Items.Clear();
            cmbSubType.Items.AddRange(_dataService.GetSubTypeFilters().ToArray());

            // 尝试恢复之前的选择
            if (!string.IsNullOrEmpty(currentType))
                cmbType.SelectedItem = currentType;
            else
                cmbType.SelectedIndex = 0;

            if (!string.IsNullOrEmpty(currentSubType))
                cmbSubType.SelectedItem = currentSubType;
            else
                cmbSubType.SelectedIndex = 0;
        }
    }
}