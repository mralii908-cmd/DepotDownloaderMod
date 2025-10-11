using System.Windows;

namespace DepotDownloaderGUI.Views
{
    public partial class LoginDialog : Window
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public bool RememberPassword { get; private set; }
        public bool Success { get; private set; }

        public LoginDialog()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Username = UsernameTextBox.Text;
            Password = PasswordBox.Password;
            RememberPassword = RememberPasswordCheckBox.IsChecked ?? false;
            Success = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Success = false;
            DialogResult = false;
            Close();
        }
    }
}
