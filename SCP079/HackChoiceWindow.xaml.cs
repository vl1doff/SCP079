using System.Windows;

namespace SCP079
{
    public partial class HackChoiceWindow : Window
    {
        public enum Choice { None, Explain, Force }
        public Choice Result { get; private set; } = Choice.None;

        public HackChoiceWindow()
        {
            InitializeComponent();
        }

        private void Explain_Click(object sender, RoutedEventArgs e)
        {
            Result = Choice.Explain;
            DialogResult = true;
        }

        private void Force_Click(object sender, RoutedEventArgs e)
        {
            Result = Choice.Force;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = Choice.None;
            DialogResult = false;
        }
    }
}