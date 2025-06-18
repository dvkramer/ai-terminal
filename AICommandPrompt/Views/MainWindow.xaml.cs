using System.Windows;
using System.Windows.Input; // Required for KeyEventArgs, Key, Keyboard, ModifierKeys
using AICommandPrompt.ViewModels; // Required for MainWindowViewModel

// The following usings might be removable if not used by InitializeComponent or base Window class after cleanup.
// For now, kept to be safe as per previous subtask's reasoning, but likely not needed for this specific logic.
using System.Windows.Documents;
using System.ComponentModel;

namespace AICommandPrompt.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChatMessageInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check if Enter key is pressed
            if (e.Key == Key.Enter)
            {
                // Check if Shift or Ctrl modifiers are NOT pressed
                if (!(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
                {
                    // Get the ViewModel
                    if (DataContext is MainWindowViewModel viewModel)
                    {
                        // Check if the SendMessageCommand can be executed
                        if (viewModel.SendMessageCommand.CanExecute())
                        {
                            viewModel.SendMessageCommand.Execute();
                        }
                    }
                    // Mark the event as handled to prevent TextBox from processing Enter (i.e., inserting a newline)
                    e.Handled = true;
                }
                // If Shift+Enter or Ctrl+Enter is pressed, e.Handled is not set to true,
                // so the TextBox will process it as a newline since AcceptsReturn="True".
            }
        }
    }
}
