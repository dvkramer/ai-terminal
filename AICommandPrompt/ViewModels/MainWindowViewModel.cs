using System; // Added
using Prism.Mvvm;
using Prism.Commands;
using System.Windows.Input;
using AICommandPrompt.Views;
using AICommandPrompt.Models;
using AICommandPrompt.Services;
using AICommandPrompt.Agent; // Added for Agent class
using System.Windows;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for ToList()
using System.ComponentModel;

namespace AICommandPrompt.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "AI Command Prompt (Chat Agent)";
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        private string _currentInputMessage;
        public string CurrentInputMessage
        {
            get => _currentInputMessage;
            set
            {
                SetProperty(ref _currentInputMessage, value);
                SendMessageCommand.RaiseCanExecuteChanged(); // Update CanExecute
            }
        }

        public ObservableCollection<ChatMessage> ChatMessages { get; } = new ObservableCollection<ChatMessage>();

        public ICommand OpenSettingsCommand { get; }
        public DelegateCommand SendMessageCommand { get; }

        private readonly SettingsViewModel _settingsViewModel;
        private readonly Agent.Agent _agent;
        private bool _isAgentProcessing = false; // To disable Send button during processing

        public MainWindowViewModel()
        {
            _settingsViewModel = new SettingsViewModel();
            // Instantiate services needed by the Agent
            var geminiService = new GeminiService();
            var powerShellService = new PowerShellService();
            _agent = new Agent.Agent(geminiService, powerShellService);

            OpenSettingsCommand = new DelegateCommand(OpenSettings);
            SendMessageCommand = new DelegateCommand(async () => await ExecuteSendMessageAsync(), CanExecuteSendMessage);
            // No need for ObservesProperty if manually calling RaiseCanExecuteChanged in CurrentInputMessage setter and IsAgentProcessing setter

            ChatMessages.Add(new ChatMessage("Welcome! Type your command request below and press Send.", MessageSender.AI, MessageDisplayType.AIStatus));
        }

        private bool CanExecuteSendMessage() => !string.IsNullOrWhiteSpace(CurrentInputMessage) && !_isAgentProcessing;

        private async Task ExecuteSendMessageAsync() // Renamed to async Task
        {
            if (!CanExecuteSendMessage()) return;

            var userMessage = new ChatMessage(CurrentInputMessage, MessageSender.User);
            ChatMessages.Add(userMessage);
            string messageToProcess = CurrentInputMessage;
            CurrentInputMessage = string.Empty;

            _isAgentProcessing = true;
            SendMessageCommand.RaiseCanExecuteChanged();

            try
            {
                _agent.SetApiKey(_settingsViewModel.ApiKey);

                // Agent now uses its own internal history, which it updates with userMessage
                List<ChatMessage> aiResponses = await _agent.ProcessUserMessageAsync(userMessage);

                Application.Current.Dispatcher.Invoke(() => // Ensure UI updates are on the UI thread
                {
                    foreach (var response in aiResponses)
                    {
                        ChatMessages.Add(response);
                    }
                });
            }
            catch (Exception ex)
            {
                 Application.Current.Dispatcher.Invoke(() =>
                 {
                    ChatMessages.Add(new ChatMessage($"Critical Agent Error: {ex.Message}", MessageSender.AI, MessageDisplayType.ErrorText));
                 });
            }
            finally
            {
                _isAgentProcessing = false;
                SendMessageCommand.RaiseCanExecuteChanged();
                // TODO: Implement auto-scrolling for the ChatMessages ListBox/ItemsControl
            }
        }

        private void OpenSettings()
        {
            var settingsDialogViewModel = new SettingsViewModel();
            var settingsView = new SettingsView
            {
                DataContext = settingsDialogViewModel
            };

            var dialogWindow = new Window
            {
                Title = "Settings - AI Command Prompt",
                Content = settingsView,
                Width = 450,
                Height = 200,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize
            };

            settingsDialogViewModel.CloseDialogRequested += (dialogResult) =>
            {
                dialogWindow.DialogResult = dialogResult;
                dialogWindow.Close();

                if (dialogResult == true)
                {
                    _settingsViewModel.ApiKey = settingsDialogViewModel.ApiKey;
                    // Update the agent's API key immediately if settings are changed
                    _agent.SetApiKey(_settingsViewModel.ApiKey);
                }
            };
            dialogWindow.ShowDialog();
        }
    }
}
