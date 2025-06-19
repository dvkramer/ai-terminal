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

            SendMessageCommand = new DelegateCommand(async () => await ExecuteSendMessageAsync(), CanExecuteSendMessage);
            // No need for ObservesProperty if manually calling RaiseCanExecuteChanged in CurrentInputMessage setter and IsAgentProcessing setter

            ChatMessages.Add(new ChatMessage("Welcome! Type your command request below and press Send.", MessageSender.AI, MessageDisplayType.AIStatus));
        }

        private bool CanExecuteSendMessage() => !string.IsNullOrWhiteSpace(CurrentInputMessage) && !_isAgentProcessing;

        private async Task ExecuteSendMessageAsync() // Renamed to async Task
        {
            if (!CanExecuteSendMessage()) return;

            var userMessage = new ChatMessage(CurrentInputMessage, MessageSender.User);
            // Add user message to chat BEFORE clearing CurrentInputMessage
            Application.Current.Dispatcher.Invoke(() => ChatMessages.Add(userMessage));

            string messageToProcess = CurrentInputMessage; // Capture message before clearing
            // CurrentInputMessage = string.Empty; // Clear input AFTER potential /api command processing or before agent call

            if (messageToProcess.Trim().StartsWith("/api ", StringComparison.OrdinalIgnoreCase))
            {
                CurrentInputMessage = string.Empty; // Clear input now as we are handling the /api command
                string extractedKey = messageToProcess.Trim().Substring("/api ".Length).Trim();
                bool success = _settingsViewModel.UpdateApiKey(extractedKey);
                string feedbackText = success ? "API Key updated successfully." : $"Failed to update API Key: {_settingsViewModel.ErrorMessage}";
                var displayType = success ? MessageDisplayType.AIStatus : MessageDisplayType.ErrorText;
                var statusMessage = new ChatMessage(feedbackText, MessageSender.AI, displayType);

                Application.Current.Dispatcher.Invoke(() => ChatMessages.Add(statusMessage));

                if(success) // Only update agent's key if ViewModel update was successful
                {
                    _agent.SetApiKey(_settingsViewModel.ApiKey); // _settingsViewModel.ApiKey would have been updated by UpdateApiKey
                }
                // No change to _isAgentProcessing or SendMessageCommand.RaiseCanExecuteChanged() as we return immediately
                return;
            }

            // Normal message processing if not an /api command
            _isAgentProcessing = true;
            SendMessageCommand.RaiseCanExecuteChanged();
            CurrentInputMessage = string.Empty; // Clear input before long-running agent call

            try
            {
                _agent.SetApiKey(_settingsViewModel.ApiKey); // Agent gets current key before processing

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

        // OpenSettings method removed
    }
}
