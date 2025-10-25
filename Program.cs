using YamlDataEditor.Forms;

namespace YamlDataEditor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Æô¶¯Ö÷ÈÝÆ÷´°Ìå
            Application.Run(new MainContainerForm());
        }
    }
}