using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AICommandPrompt.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        private string _apiKey;
        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // UI-specific commands and events are removed.
        // public ICommand SaveCommand { get; }
        // public ICommand CancelCommand { get; }
        // public delegate void RequestCloseDialog(bool? dialogResult);
        // public event RequestCloseDialog CloseDialogRequested;

        private static readonly string AppName = "AICommandPrompt";
        private static readonly string SettingsFileName = "settings.json";
        private static readonly byte[] Entropy = Encoding.Unicode.GetBytes("AICommandPromptSalt");

        private string _settingsFilePath;

        public SettingsViewModel()
        {
            InitializeSettingsPath();
            // SaveCommand and CancelCommand initializations removed.
        }

        private void InitializeSettingsPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsFilePath = Path.Combine(appDataPath, AppName, SettingsFileName);
        }

        private class SettingsData
        {
            public string EncryptedApiKey { get; set; }
        }

        public async Task LoadApiKeyAsync()
        {
            ErrorMessage = null; // Clear previous errors
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(_settingsFilePath);
                    if (string.IsNullOrWhiteSpace(json)) // Handle empty settings file
                    {
                        ApiKey = string.Empty;
                        return;
                    }
                    var settings = JsonSerializer.Deserialize<SettingsData>(json);
                    if (settings != null && !string.IsNullOrEmpty(settings.EncryptedApiKey))
                    {
                        byte[] encryptedData = Convert.FromBase64String(settings.EncryptedApiKey);
                        byte[] decryptedData = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
                        ApiKey = Encoding.UTF8.GetString(decryptedData);
                    }
                    else
                    {
                        ApiKey = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading API key: {ex.Message}");
                    ApiKey = string.Empty;
                    ErrorMessage = $"Error loading API key: {ex.Message}"; // User feedback
                }
            }
            else
            {
                ApiKey = string.Empty;
            }
        }

        public bool UpdateApiKey(string newApiKey)
        {
            ErrorMessage = null; // Clear previous errors

            if (string.IsNullOrWhiteSpace(newApiKey))
            {
                try
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        File.Delete(_settingsFilePath);
                    }
                    ApiKey = string.Empty; // Update in-memory API key
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting settings file: {ex.Message}");
                    ErrorMessage = $"Error clearing API key: {ex.Message}";
                    return false;
                }
            }

            try
            {
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(newApiKey);
                byte[] encryptedData = ProtectedData.Protect(dataToEncrypt, Entropy, DataProtectionScope.CurrentUser);
                string encryptedBase64 = Convert.ToBase64String(encryptedData);

                var settings = new SettingsData { EncryptedApiKey = encryptedBase64 };
                string json = JsonSerializer.Serialize(settings);

                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)); // Ensure directory exists
                File.WriteAllText(_settingsFilePath, json);

                ApiKey = newApiKey; // Update in-memory API key
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving API key: {ex.Message}");
                ErrorMessage = $"Error saving API key: {ex.Message}";
                return false;
            }
        }

        // OnSave, CanSave, OnCancel methods are removed.
    }
}
