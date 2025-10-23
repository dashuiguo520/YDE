using YamlDataEditor.Models;

namespace YamlDataEditor.Forms
{
    public partial class ItemEditorForm : Form
    {
        public Item EditedItem { get; private set; }
        private PropertyGrid propertyGrid;

        public ItemEditorForm(Item item, string title = "编辑物品")
        {
            InitializeComponent();
            EditedItem = CloneItem(item);
            this.Text = title;
            SetupPropertyGrid();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "物品编辑器";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            // 属性网格
            propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                SelectedObject = EditedItem,
                ToolbarVisible = true,
                HelpVisible = true
            };

            // 按钮面板
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45
            };

            var okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 30),
                Location = new Point(300, 8)
            };

            var cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 30),
                Location = new Point(390, 8)
            };

            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);

            this.Controls.Add(propertyGrid);
            this.Controls.Add(buttonPanel);
        }

        private void SetupPropertyGrid()
        {
            propertyGrid.SelectedObject = EditedItem;
            // 设置属性网格的属性排序和显示
            propertyGrid.PropertySort = PropertySort.Categorized;

            // 确保属性网格使用正确的字体
            propertyGrid.Font = new Font("Microsoft YaHei UI", 9);

            // 设置属性网格的行高以适应多行文本
            propertyGrid.LargeButtons = false;
            propertyGrid.ToolbarVisible = true;
            propertyGrid.HelpVisible = true;

            // 刷新属性网格
            propertyGrid.Refresh();

        }

        private Item CloneItem(Item source)
        {
            // 简单的深拷贝实现
            return new Item
            {
                Id = source.Id,
                AegisName = source.AegisName,
                Name = source.Name,
                Type = source.Type,
                SubType = source.SubType,
                Buy = source.Buy,
                Sell = source.Sell,
                Weight = source.Weight,
                Attack = source.Attack,
                MagicAttack = source.MagicAttack,
                Range = source.Range,
                Slots = source.Slots,
                Jobs = new JobRequirements
                {
                    Alchemist = source.Jobs.Alchemist,
                    Archer = source.Jobs.Archer,
                    Assassin = source.Jobs.Assassin,
                    Swordman = source.Jobs.Swordman,
                    Mage = source.Jobs.Mage,
                    Merchant = source.Jobs.Merchant,
                    Acolyte = source.Jobs.Acolyte,
                    Thief = source.Jobs.Thief
                },
                Locations = new LocationRequirements
                {
                    Right_Hand = source.Locations.Right_Hand,
                    Both_Hand = source.Locations.Both_Hand,
                    Head = source.Locations.Head,
                    Body = source.Locations.Body,
                    Garment = source.Locations.Garment,
                    Shoes = source.Locations.Shoes,
                    Accessory = source.Locations.Accessory
                },
                WeaponLevel = source.WeaponLevel,
                EquipLevelMin = source.EquipLevelMin,
                Refineable = source.Refineable,
                Script = source.Script,
                EquipScript = source.EquipScript,
                UnEquipScript = source.UnEquipScript,
                Trade = new TradeRestrictions
                {
                    NoDrop = source.Trade.NoDrop,
                    NoTrade = source.Trade.NoTrade,
                    NoSell = source.Trade.NoSell,
                    NoStorage = source.Trade.NoStorage,
                    NoVend = source.Trade.NoVend,
                    NoTradeRoom = source.Trade.NoTradeRoom,
                    NoCart = source.Trade.NoCart,
                    NoGuildStorage = source.Trade.NoGuildStorage,
                    NoMail = source.Trade.NoMail,
                    NoAuction = source.Trade.NoAuction
                }
            };
        }
    }
}