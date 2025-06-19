# AI Command Prompt

AI Command Prompt is a smart terminal application for Windows that translates your natural language requests into executable PowerShell commands. Powered by Google's Gemini API, it acts as an intelligent agent, allowing you to interact with your system in a conversational way.

Instead of remembering complex commands, you can simply ask the application what you want to do (e.g., "list all text files in my documents folder" or "create a new directory called 'my-project' and navigate into it"), and the AI agent will determine the correct PowerShell commands, execute them, and display the results.

## Features

-   **Natural Language to PowerShell:** Converts plain English requests into precise PowerShell commands.
-   **Intelligent Agent:** A core agent orchestrates the conversation, command execution, and error handling. It can perform multi-step tasks by analyzing the output of previous commands.
-   **Interactive Chat UI:** A clean, chat-based interface built with WPF and Prism to display your requests, the AI's reasoning, the commands being executed, and their output.
-   **Error Handling:** Intelligently handles and displays errors from PowerShell execution, allowing the AI to retry, correct the command, or ask for clarification.
-   **Secure API Key Storage:** Your Google Gemini API key is securely stored using the Windows Data Protection API (`ProtectedData`).
-   **Asynchronous Operations:** Ensures the UI remains responsive while the agent processes requests and executes commands in the background.

## How It Works

The application operates in a loop, managed by the `Agent` class:

1.  **User Input:** You enter a request in the `MainWindow`.
2.  **Agent Processing:** The `MainWindowViewModel` sends the message to the `Agent`.
3.  **Gemini API Call:** The `Agent` formats the conversation history and sends it to the `GeminiService`. A detailed system prompt instructs the Gemini model to act as a PowerShell expert and respond with a specific action (`speak`, `execute_powershell`, or `error`).
4.  **Action Decision:** The application parses Gemini's response.
    -   If the action is `execute_powershell`, the `PowerShellService` is invoked to run the command.
    -   If the action is `speak`, the message is displayed directly to the user.
    -   If an `error` occurs, it is logged and displayed.
5.  **Feedback Loop:** The output or error from the PowerShell command is fed back into the agent's conversation history. The agent then calls the Gemini API again with this new context to decide the next step. This allows for complex, multi-step operations.
6.  **UI Update:** All interactions, including AI reasoning, commands, and results, are displayed as messages in the chat window.

## Getting Started

### Prerequisites

-   .NET 6.0 SDK for Windows
-   A Google Gemini API Key. You can get one from [Google AI Studio](https://aistudio.google.com/app/apikey).

### Installation & Running

1.  Clone the repository to your local machine.
2.  Open a terminal or command prompt in the project's root directory.
3.  Build and run the application using the .NET CLI:
    ```bash
    dotnet run --project AICommandPrompt/AICommandPrompt.csproj
    ```
4.  The application window will appear.

## Usage

1.  **Set API Key:** The first time you run the application, click the "Settings" button. Paste your Google Gemini API key into the text field and click "Save".
2.  **Make a Request:** In the main window, type a request in the input box at the bottom (e.g., `What's my current directory?`).
3.  **Send:** Press `Enter` or click the "Send" button.
4.  **Observe:** The AI will process your request. You will see messages indicating its reasoning, the exact PowerShell command it's executing, and finally, the output or result of that command.

## Project Structure

The project follows the Model-View-ViewModel (MVVM) design pattern using the Prism framework.

-   `/AICommandPrompt` - The main project directory.
    -   `/Views` - Contains the WPF windows and user controls (`.xaml` files).
        -   `MainWindow.xaml`: The main chat interface.
        -   `SettingsView.xaml`: The settings dialog for the API key.
    -   `/ViewModels` - Contains the logic and data for the Views.
        -   `MainWindowViewModel.cs`: Handles chat logic, user input, and communication with the agent.
        -   `SettingsViewModel.cs`: Manages loading, saving, and encrypting the API key.
    -   `/Agent` - The core logic for the AI agent.
        -   `Agent.cs`: The "brain" of the application that manages the interaction loop.
    -   `/Services` - Contains services for external and system interactions.
        -   `GeminiService.cs`: Manages all communication with the Google Gemini API.
        -   `PowerShellService.cs`: Executes PowerShell commands and captures their output.
    -   `/Models` - Contains the data structures used throughout the application.
        -   `ChatMessage.cs`: Represents a single message in the chat history.
        -   `AgentActionResponse.cs`: Represents a decision made by the AI agent.
    -   `App.xaml.cs`: The application's entry point and global exception handler.
-   `run_dotnet.sh` & `dotnet-install.sh`: Shell scripts for setting up and running a .NET environment, typically for CI/CD or non-Windows environments.
