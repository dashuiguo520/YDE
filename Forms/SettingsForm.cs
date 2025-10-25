using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text;

namespace YamlDataEditor.Forms
{
    public partial class SettingsForm : Form
    {
        // 设置属性
        public string DatabasePath { get; private set; }
        public Encoding SelectedEncoding { get; private set; }

        // 控件
        private TextBox txtDatabasePath;
        private Button btnBrowseDatabase;
        private ComboBox cmbEncoding;
        private Label lblDatabasePath;
        private Label lblEncoding;

        // 设置文件路径
        private readonly string settingsFilePath;

        public SettingsForm()
        {
            settingsFilePath = Path.Combine(Application.StartupPath, "editor_settings.config");
            InitializeComponent();
            LoadCurrentSettings();

            // 设置控件变化事件
            txtDatabasePath.TextChanged += SettingChanged;
            cmbEncoding.SelectedIndexChanged += SettingChanged;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体设置
            this.Text = "系统设置";
            this.Size = new Size(500, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(20);

            // 创建控件
            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            int yPos = 20;
            int labelWidth = 120;
            int controlWidth = 300;
            int buttonWidth = 80;

            // 数据库路径设置
            lblDatabasePath = new Label
            {
                Text = "数据库路径:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20),
                Font = new Font("Microsoft YaHei UI", 9)
            };

            txtDatabasePath = new TextBox
            {
                Location = new Point(150, yPos - 3),
                Size = new Size(controlWidth, 25),
                Font = new Font("Microsoft YaHei UI", 9)
            };

            btnBrowseDatabase = new Button
            {
                Text = "浏览...",
                Location = new Point(460, yPos - 3),
                Size = new Size(buttonWidth, 25),
                Font = new Font("Microsoft YaHei UI", 9)
            };
            btnBrowseDatabase.Click += BtnBrowseDatabase_Click;

            yPos += 40;

            // 文件编码设置
            lblEncoding = new Label
            {
                Text = "默认文件编码:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 20),
                Font = new Font("Microsoft YaHei UI", 9)
            };

            cmbEncoding = new ComboBox
            {
                Location = new Point(150, yPos - 3),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9)
            };

            // 添加编码选项 - 使用与之前相同的顺序
            cmbEncoding.Items.Add("UTF-8 (推荐)");
            cmbEncoding.Items.Add("UTF-8 (带BOM)");
            cmbEncoding.Items.Add("GB2312 (简体中文)");
            cmbEncoding.Items.Add("GBK (简体中文)");
            cmbEncoding.Items.Add("GB18030 (简体中文)");
            cmbEncoding.SelectedIndex = 0;

            // 添加到窗体
            this.Controls.AddRange(new Control[]
            {
                lblDatabasePath, txtDatabasePath, btnBrowseDatabase,
                lblEncoding, cmbEncoding
            });
        }

        private void BtnBrowseDatabase_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "选择数据库文件路径";
                folderDialog.ShowNewFolderButton = true;

                // 设置默认路径
                if (Directory.Exists(@"D:\rathena\db\re"))
                {
                    folderDialog.SelectedPath = @"D:\rathena\db\re";
                }
                else if (!string.IsNullOrEmpty(txtDatabasePath.Text) && Directory.Exists(txtDatabasePath.Text))
                {
                    folderDialog.SelectedPath = txtDatabasePath.Text;
                }
                else
                {
                    folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtDatabasePath.Text = folderDialog.SelectedPath;
                    // 路径改变时会自动触发保存
                }
            }
        }

        // 使用您提供的 GetEncodingFromComboBox 方法
        private Encoding GetEncodingFromComboBox()
        {
            switch (cmbEncoding.SelectedIndex)
            {
                case 0: return Encoding.UTF8;
                case 1: return new UTF8Encoding(true);
                case 2: return Encoding.GetEncoding("GB2312");
                case 3: return Encoding.GetEncoding("GBK");
                case 4: return Encoding.GetEncoding("GB18030");
                default: return Encoding.UTF8;
            }
        }

        private void LoadCurrentSettings()
        {
            // 默认路径
            string defaultPath = @"D:\rathena\db\re";

            // 从配置文件加载上次的设置
            string loadedPath = defaultPath;
            int loadedEncodingIndex = 0;

            if (File.Exists(settingsFilePath))
            {
                try
                {
                    var lines = File.ReadAllLines(settingsFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            switch (parts[0])
                            {
                                case "DatabasePath":
                                    // 检查路径是否存在，如果不存在则使用默认路径
                                    if (Directory.Exists(parts[1]))
                                    {
                                        loadedPath = parts[1];
                                    }
                                    else
                                    {
                                        Console.WriteLine($"上次设置的路径不存在，使用默认路径: {defaultPath}");
                                    }
                                    break;
                                case "Encoding":
                                    if (int.TryParse(parts[1], out int encodingIndex) &&
                                        encodingIndex >= 0 && encodingIndex < cmbEncoding.Items.Count)
                                    {
                                        loadedEncodingIndex = encodingIndex;
                                    }
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载设置失败: {ex.Message}");
                }
            }

            // 设置控件值
            txtDatabasePath.Text = loadedPath;
            cmbEncoding.SelectedIndex = loadedEncodingIndex;

            // 更新属性
            DatabasePath = loadedPath;
            SelectedEncoding = GetEncodingFromComboBox();
        }

        // 设置改变时自动保存
        private void SettingChanged(object sender, EventArgs e)
        {
            SaveSettingsToFile();
        }

        private void SaveSettingsToFile()
        {
            try
            {
                // 更新属性
                DatabasePath = txtDatabasePath.Text;
                SelectedEncoding = GetEncodingFromComboBox();

                var settings = new System.Text.StringBuilder();
                settings.AppendLine($"DatabasePath={DatabasePath}");
                settings.AppendLine($"Encoding={cmbEncoding.SelectedIndex}");

                File.WriteAllText(settingsFilePath, settings.ToString(), Encoding.UTF8);
                Console.WriteLine($"设置已自动保存: 路径={DatabasePath}, 编码={cmbEncoding.SelectedItem}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置文件失败: {ex.Message}");
            }
        }

        // 添加一个公共方法，供外部获取当前设置
        public (string path, Encoding encoding) GetCurrentSettings()
        {
            return (DatabasePath, SelectedEncoding);
        }
    }
}