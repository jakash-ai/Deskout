using System.Windows;
using System.Windows.Input;

namespace Deskout.Views
{
    public partial class CustomInputDialog : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public CustomInputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            TitleTextBlock.Text = title;
            PromptTextBlock.Text = prompt;
            InputTextBox.Text = defaultValue;
            
            // Focus and select the text
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text;
            DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkBtn_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CancelBtn_Click(sender, e);
            }
        }
    }
}
