using System.Windows;
using System.Windows.Input;

namespace SubTask.PanelNavigation
{
    /// <summary>
    /// Interaction logic for PausePopUp.xaml
    /// </summary>
    public partial class PausePopUp : Window
    {
        public PausePopUp()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
