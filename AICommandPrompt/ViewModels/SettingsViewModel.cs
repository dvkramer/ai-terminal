using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public delegate void RequestCloseDialog(bool? dialogResult);
        public event RequestCloseDialog CloseDialogRequested;

        private static readonly string AppName = "AICommandPrompt";
        private static readonly string SettingsFileName = "settings.json";
        private static readonly byte[] Entropy = Encoding.Unicode.GetBytes("AICommandPromptSalt");

        private string _settingsFilePath;

        public SettingsViewModel()
        {
            InitializeSettingsPath();
            SaveCommand = new DelegateCommand(OnSave, CanSave).ObservesProperty(() => ApiKey);
            CancelCommand = new DelegateCommand(OnCancel);
            LoadApiKey();
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

        private void LoadApiKey()
        {
            ErrorMessage = null; // Clear previous errors
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
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

        private void OnSave()
        {
            ErrorMessage = null; // Clear previous errors

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                try
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        File.Delete(_settingsFilePath);
                    }
                    CloseDialogRequested?.Invoke(true);
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting settings file: {ex.Message}");
                    ErrorMessage = $"Error clearing API key: {ex.Message}";
                    // Not closing dialog on error here, user can retry or cancel
                    return;
                }
            }

            try
            {
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(ApiKey);
                byte[] encryptedData = ProtectedData.Protect(dataToEncrypt, Entropy, DataProtectionScope.CurrentUser);
                string encryptedBase64 = Convert.ToBase64String(encryptedData);

                var settings = new SettingsData { EncryptedApiKey = encryptedBase64 };
                string json = JsonSerializer.Serialize(settings);

                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
                File.WriteAllText(_settingsFilePath, json);

                CloseDialogRequested?.Invoke(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving API key: {ex.Message}");
                ErrorMessage = $"Error saving API key: {ex.Message}";
                // Not closing dialog on error, user can see the message, retry or cancel.
                // CloseDialogRequested?.Invoke(false); // This would close it, but let's keep it open on save error.
            }
        }

        private bool CanSave()
        {
            return true;
        }

        private void OnCancel()
        {
            CloseDialogRequested?.Invoke(false);
        }
    }
}
