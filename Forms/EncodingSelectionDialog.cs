// EncodingSelectionDialog.cs
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace YamlDataEditor.Forms
{
    public partial class EncodingSelectionDialog : Form
    {
        public ComboBox encodingComboBox; // 改为public以便访问
        private Button okButton;
        private Button cancelButton;
        private Button btnAutoDetect;
        private Label lblDetectionResult;

        public Encoding SelectedEncoding { get; private set; }

        public EncodingSelectionDialog()
        {
            InitializeComponent();
            SelectedEncoding = Encoding.GetEncoding("GB2312"); // 默认改为GB2312
        }

        private void InitializeComponent()
        {
            this.Text = "选择文件编码";
            this.Size = new Size(400, 250); // 增加高度以容纳新控件
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var label = new Label
            {
                Text = "请选择文件编码(推荐为GB2312处理中文):",
                Location = new Point(10, 10),
                Size = new Size(360, 20)
            };

            encodingComboBox = new ComboBox
            {
                Location = new Point(10, 35),
                Size = new Size(360, 21),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 添加常见编码选项，特别加强中文编码支持
            encodingComboBox.Items.Add(new EncodingItem("GB2312 (简体中文 - 推荐)", Encoding.GetEncoding("GB2312")));
            encodingComboBox.Items.Add(new EncodingItem("GBK (简体中文)", Encoding.GetEncoding("GBK")));
            encodingComboBox.Items.Add(new EncodingItem("GB18030 (简体中文)", Encoding.GetEncoding("GB18030")));
            encodingComboBox.Items.Add(new EncodingItem("UTF-8 (通用)", Encoding.UTF8));
            encodingComboBox.Items.Add(new EncodingItem("UTF-8 (带BOM)", new UTF8Encoding(true)));
            encodingComboBox.Items.Add(new EncodingItem("Big5 (繁体中文)", Encoding.GetEncoding("Big5")));
            encodingComboBox.Items.Add(new EncodingItem("Unicode (UTF-16LE)", Encoding.Unicode));
            encodingComboBox.Items.Add(new EncodingItem("BigEndianUnicode (UTF-16BE)", Encoding.BigEndianUnicode));

            // 设置默认选择为GB2312
            var gb2312Item = encodingComboBox.Items.OfType<EncodingItem>()
                .FirstOrDefault(item => item.Encoding.BodyName.Contains("gb2312"));
            if (gb2312Item != null)
            {
                encodingComboBox.SelectedItem = gb2312Item;
            }
            else
            {
                encodingComboBox.SelectedIndex = 0;
            }

            lblDetectionResult = new Label
            {
                Text = "提示：如果文件包含中文，请选择GB2312或GBK编码",
                Location = new Point(10, 65),
                Size = new Size(360, 30),
                ForeColor = Color.Blue
            };

            btnAutoDetect = new Button
            {
                Text = "自动检测编码",
                Location = new Point(10, 100),
                Size = new Size(100, 25)
            };
            btnAutoDetect.Click += BtnAutoDetect_Click;

            okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 140),
                Size = new Size(80, 30)
            };

            cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 140),
                Size = new Size(80, 30)
            };

            this.Controls.AddRange(new Control[] {
                label, encodingComboBox, lblDetectionResult,
                btnAutoDetect, okButton, cancelButton
            });

            okButton.Click += (s, e) =>
            {
                if (encodingComboBox.SelectedItem is EncodingItem item)
                {
                    SelectedEncoding = item.Encoding;
                }
            };

            // 监听编码选择变化
            encodingComboBox.SelectedIndexChanged += (s, e) =>
            {
                if (encodingComboBox.SelectedItem is EncodingItem item)
                {
                    UpdateEncodingInfo(item.Encoding);
                }
            };

            // 初始化编码信息
            if (encodingComboBox.SelectedItem is EncodingItem selectedItem)
            {
                UpdateEncodingInfo(selectedItem.Encoding);
            }
        }

        private void BtnAutoDetect_Click(object sender, EventArgs e)
        {
            // 这里可以添加自动检测编码的逻辑
            // 暂时显示提示信息
            lblDetectionResult.Text = "自动检测功能需要选择文件路径，请在主界面操作";
            lblDetectionResult.ForeColor = Color.Orange;
        }

        private void UpdateEncodingInfo(Encoding encoding)
        {
            string info = $"编码名称: {encoding.EncodingName}, 代码页: {encoding.CodePage}";
            if (encoding.BodyName.Contains("gb") || encoding.BodyName.Contains("big5"))
            {
                info += " (中文编码)";
            }
            lblDetectionResult.Text = info;
        }

        public class EncodingItem
        {
            public string DisplayName { get; }
            public Encoding Encoding { get; }

            public EncodingItem(string displayName, Encoding encoding)
            {
                DisplayName = displayName;
                Encoding = encoding;
            }

            public override string ToString() => DisplayName;
        }
    }
}