using System.Windows;
using System.Windows.Input; // Required for KeyEventArgs, Key, Keyboard, ModifierKeys
using AICommandPrompt.ViewModels; // Required for MainWindowViewModel
using System.Collections.Specialized; // Required for NotifyCollectionChangedEventArgs
using System.Windows.Controls;      // Required for ScrollViewer (though often x:Name makes it directly accessible)
using System.Windows.Threading;     // Required for Dispatcher and DispatcherPriority
using System;                       // Required for Action

// The following usings might be removable if not used by InitializeComponent or base Window class after cleanup.
// For now, kept to be safe as per previous subtask's reasoning, but likely not needed for this specific logic.
// using System.Windows.Documents; // Likely not needed for this logic
// using System.ComponentModel; // Likely not needed for this logic

namespace AICommandPrompt.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Unloaded += MainWindow_Unloaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = this.DataContext as MainWindowViewModel;
            if (_viewModel != null && _viewModel.ChatMessages != null)
            {
                _viewModel.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
            }
        }

        private void ChatMessages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // Ensure scrollviewer is not null and is visible
                if (ChatScrollViewer != null && ChatScrollViewer.Visibility == Visibility.Visible)
                {
                    // Using Dispatcher to ensure the scroll happens after the layout update
                    // that adds the new item. Priority can be adjusted. Input or Background usually work.
                    ChatScrollViewer.Dispatcher.BeginInvoke(
                        new Action(() => ChatScrollViewer.ScrollToEnd()),
                        DispatcherPriority.Background); // Or DispatcherPriority.Input
                }
            }
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.ChatMessages != null)
            {
                _viewModel.ChatMessages.CollectionChanged -= ChatMessages_CollectionChanged;
            }
        }

        private void ChatMessageInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check if Enter key is pressed
            if (e.Key == Key.Enter)
            {
                // Check if Shift or Ctrl modifiers are NOT pressed
                // The original prompt mentioned only Shift, but existing code also checked Ctrl.
                // Keeping existing behavior as it's more permissive for newlines if Ctrl+Enter is also a common user pattern.
                if (!(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
                {
                    // Get the ViewModel (already available as _viewModel if Loaded has run)
                    // Using DataContext directly as in original code is also fine, but _viewModel is cleaner if available.
                    var currentViewModel = _viewModel ?? this.DataContext as MainWindowViewModel;
                    if (currentViewModel != null)
                    {
                        // Check if the SendMessageCommand can be executed
                        if (currentViewModel.SendMessageCommand.CanExecute(null)) // Pass null for parameter if command doesn't expect one
                        {
                            currentViewModel.SendMessageCommand.Execute(null); // Pass null for parameter
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
