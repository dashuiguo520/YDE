// EncodingSelectionDialog.cs
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace YamlDataEditor
{
    public partial class EncodingSelectionDialog : Form
    {
        private ComboBox encodingComboBox;
        private Button okButton;
        private Button cancelButton;

        public Encoding SelectedEncoding { get; private set; }

        public EncodingSelectionDialog()
        {
            InitializeComponent();
            SelectedEncoding = Encoding.UTF8;
        }

        private void InitializeComponent()
        {
            this.Text = "选择文件编码";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var label = new Label
            {
                Text = "请选择文件编码:",
                Location = new Point(10, 10),
                Size = new Size(200, 20)
            };

            encodingComboBox = new ComboBox
            {
                Location = new Point(10, 35),
                Size = new Size(260, 21),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 添加常见编码选项
            encodingComboBox.Items.Add(new EncodingItem("UTF-8 (推荐)", Encoding.UTF8));
            encodingComboBox.Items.Add(new EncodingItem("UTF-8 (带BOM)", new UTF8Encoding(true)));
            encodingComboBox.Items.Add(new EncodingItem("GB2312 (简体中文)", Encoding.GetEncoding("GB2312")));
            encodingComboBox.Items.Add(new EncodingItem("GBK (简体中文)", Encoding.GetEncoding("GBK")));
            encodingComboBox.Items.Add(new EncodingItem("GB18030 (简体中文)", Encoding.GetEncoding("GB18030")));
            encodingComboBox.Items.Add(new EncodingItem("Big5 (繁体中文)", Encoding.GetEncoding("Big5")));
            encodingComboBox.Items.Add(new EncodingItem("Unicode (UTF-16LE)", Encoding.Unicode));
            encodingComboBox.SelectedIndex = 0;

            okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(100, 70),
                Size = new Size(75, 23)
            };

            cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(185, 70),
                Size = new Size(75, 23)
            };

            this.Controls.AddRange(new Control[] { label, encodingComboBox, okButton, cancelButton });

            okButton.Click += (s, e) =>
            {
                if (encodingComboBox.SelectedItem is EncodingItem item)
                {
                    SelectedEncoding = item.Encoding;
                }
            };
        }

        private class EncodingItem
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