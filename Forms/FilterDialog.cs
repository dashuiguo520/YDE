using YamlDataEditor.Models;

namespace YamlDataEditor.Forms
{
    public partial class FilterDialog : Form
    {
        // 正确定义控件变量
        private TextBox txtSearch;
        private ComboBox cmbType;
        private ComboBox cmbSubType;
        private Button btnOK;
        private Button btnCancel;
        private Label lblSearch;
        private Label lblType;
        private Label lblSubType;

        public string SearchText => txtSearch.Text;
        public string TypeFilter => cmbType.SelectedItem?.ToString() == "全部" ? "" : cmbType.SelectedItem?.ToString();
        public string SubTypeFilter => cmbSubType.SelectedItem?.ToString() == "全部" ? "" : cmbSubType.SelectedItem?.ToString();

        private List<Item> _allItems;

        public FilterDialog(List<Item> allItems = null)
        {
            _allItems = allItems ?? new List<Item>();
            InitializeComponent();
            PopulateComboBoxes();
        }

        // 添加InitializeComponent方法
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "筛选物品";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 创建和配置控件
            lblSearch = new Label
            {
                Text = "搜索:",
                Location = new Point(20, 20),
                Size = new Size(40, 20)
            };

            txtSearch = new TextBox
            {
                Location = new Point(70, 17),
                Size = new Size(150, 20),
                PlaceholderText = "输入名称搜索"
            };

            lblType = new Label
            {
                Text = "类型:",
                Location = new Point(20, 50),
                Size = new Size(40, 20)
            };

            cmbType = new ComboBox
            {
                Location = new Point(70, 47),
                Size = new Size(150, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            lblSubType = new Label
            {
                Text = "子类型:",
                Location = new Point(20, 80),
                Size = new Size(50, 20)
            };

            cmbSubType = new ComboBox
            {
                Location = new Point(70, 77),
                Size = new Size(150, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnOK = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(240, 15),
                Size = new Size(70, 30)
            };

            btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(240, 55),
                Size = new Size(70, 30)
            };

            // 添加控件到窗体
            this.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch, lblType, cmbType,
                lblSubType, cmbSubType, btnOK, btnCancel
            });

            this.ResumeLayout(false);
        }

        private void PopulateComboBoxes()
        {
            cmbType.Items.Clear();
            cmbSubType.Items.Clear();

            // 添加"全部"选项
            cmbType.Items.Add("全部");
            cmbSubType.Items.Add("全部");

            if (_allItems != null && _allItems.Any())
            {
                // 获取唯一的类型和子类型
                var types = _allItems.Select(i => i.Type)
                                   .Where(t => !string.IsNullOrEmpty(t))
                                   .Distinct()
                                   .OrderBy(t => t);

                var subTypes = _allItems.Select(i => i.SubType)
                                      .Where(st => !string.IsNullOrEmpty(st))
                                      .Distinct()
                                      .OrderBy(st => st);

                cmbType.Items.AddRange(types.ToArray());
                cmbSubType.Items.AddRange(subTypes.ToArray());
            }

            cmbType.SelectedIndex = 0;
            cmbSubType.SelectedIndex = 0;
        }
    }
}