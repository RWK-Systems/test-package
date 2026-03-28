using System.Windows;

namespace TestPackage.Configurator
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow(string previewText)
        {
            InitializeComponent();
            PreviewText.Text = previewText;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
