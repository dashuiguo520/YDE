using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using YamlDataEditor.Forms;

namespace YamlDataEditor
{
    public partial class MainContainerForm : Form
    {
        private Panel leftPanel;
        private Panel rightPanel;
        private TreeView menuTreeView;
        private Control currentControl;

        // 功能窗体实例
        private MainForm itemEditorForm;
        private SettingsForm settingsForm;

        // 设置文件路径
        private readonly string settingsFilePath;

        public MainContainerForm()
        {
            settingsFilePath = Path.Combine(Application.StartupPath, "editor_settings.config");
            InitializeComponent();
            SetupLayout();
            InitializeMenu();

            // 检查是否有保存的设置，如果有则显示物品编辑器，否则显示设置
            if (HasValidSettings())
            {
                ShowItemEditor();
                if (menuTreeView.Nodes.Count > 0 && menuTreeView.Nodes[0].Nodes.Count > 1)
                {
                    menuTreeView.SelectedNode = menuTreeView.Nodes[0].Nodes[1]; // 选中物品数据节点
                }
            }
            else
            {
                ShowSettings();
                if (menuTreeView.Nodes.Count > 0 && menuTreeView.Nodes[0].Nodes.Count > 0)
                {
                    menuTreeView.SelectedNode = menuTreeView.Nodes[0].Nodes[0]; // 选中设置节点
                }
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体基本设置
            this.Text = "YAML数据编辑器 - 主容器";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);

            this.ResumeLayout(false);
        }

        private void SetupLayout()
        {
            // 左侧菜单面板
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 右侧内容面板
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            // 菜单树形视图
            menuTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft YaHei UI", 10),
                FullRowSelect = true,
                HideSelection = false,
                ShowLines = true,
                ShowRootLines = true,
                Indent = 20
            };

            menuTreeView.AfterSelect += MenuTreeView_AfterSelect;

            leftPanel.Controls.Add(menuTreeView);

            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);
        }

        private void InitializeMenu()
        {
            // 创建根节点
            TreeNode rootNode = new TreeNode("功能菜单");
            rootNode.Expand();

            // 添加功能节点
            TreeNode settingsNode = new TreeNode("⚙️ 系统设置");
            TreeNode itemsNode = new TreeNode("📦 物品数据编辑");

            // 设置节点标签用于识别
            settingsNode.Tag = "Settings";
            itemsNode.Tag = "Items";

            rootNode.Nodes.Add(settingsNode);
            rootNode.Nodes.Add(itemsNode);

            menuTreeView.Nodes.Add(rootNode);
        }

        private void MenuTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag == null) return;

            string tag = e.Node.Tag.ToString();

            switch (tag)
            {
                case "Settings":
                    ShowSettings();
                    break;
                case "Items":
                    ShowItemEditor();
                    break;
            }
        }

        private bool HasValidSettings()
        {
            if (!File.Exists(settingsFilePath))
                return false;

            try
            {
                var lines = File.ReadAllLines(settingsFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2 && parts[0] == "DatabasePath")
                    {
                        return Directory.Exists(parts[1]);
                    }
                }
            }
            catch
            {
                // 忽略错误，返回false
            }
            return false;
        }

        private void ShowSettings()
        {
            if (settingsForm == null)
            {
                settingsForm = new SettingsForm();
                settingsForm.TopLevel = false;
                settingsForm.FormBorderStyle = FormBorderStyle.None;
                settingsForm.Dock = DockStyle.Fill;
            }

            ShowControl(settingsForm);
        }

        private void ShowItemEditor()
        {
            if (itemEditorForm == null)
            {
                itemEditorForm = new MainForm();
                itemEditorForm.TopLevel = false;
                itemEditorForm.FormBorderStyle = FormBorderStyle.None;
                itemEditorForm.Dock = DockStyle.Fill;
            }

            ShowControl(itemEditorForm);
        }

        private void ShowControl(Control control)
        {
            // 清除当前显示的控件
            rightPanel.Controls.Clear();

            if (currentControl != null)
            {
                currentControl.Visible = false;
            }

            // 添加新控件
            rightPanel.Controls.Add(control);
            control.Visible = true;
            control.BringToFront();

            currentControl = control;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 清理资源
            itemEditorForm?.Close();
            settingsForm?.Close();

            base.OnFormClosing(e);
        }
    }
}