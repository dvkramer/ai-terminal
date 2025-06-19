# AI Command Prompt

AI Command Prompt is a Windows WPF application that provides a chat-based interface to an AI agent. This agent can understand your requests in natural language and execute corresponding PowerShell commands to automate tasks, answer questions, and assist you right from your desktop.

---

## Features

* **Natural Language to PowerShell:** Simply ask the AI what you want to do, and it will figure out the right PowerShell command to run.
* **Conversational Interface:** A simple and intuitive chat window to interact with the AI agent.
* **Context Aware:** The agent remembers the context of your conversation to handle multi-step tasks.
* **Secure API Key Handling:** Your API key is stored securely on your local machine and is never sent in your prompts.
* **Command and Output Logging:** See the exact commands the AI is executing and their full output, including errors.

---

## Getting Started

Follow these instructions to get the application running on your local machine.

### Prerequisites

* **.NET 6.0 Desktop Runtime:** You must have the [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) installed.
* **Gemini API Key:** You need an API key from [Google AI Studio](https://makersuite.google.com/app/apikey).

### Installation & Running

1.  Go to the [Releases page](https://github.com/dvkramer/ai-terminal/releases) of this repository.
2.  Download the latest release zip file.
3.  Unzip the folder to your desired location.
4.  Run `AICommandPrompt.exe`.

### Configuration

Before the AI agent can work, you must provide it with your Gemini API key.

1.  Open the application.
2.  In the chat input box, type the following command, replacing `YOUR_API_KEY_HERE` with your actual key:
    ```
    /api YOUR_API_KEY_HERE
    ```
3.  Press Enter or click "Send". You will see a confirmation message that the key has been updated.

> **Note:** This `/api` command is processed locally on your machine. Your API key is **not** sent to the AI model or exposed in any chat history that the model sees.

---

## How to Use

Once your API key is configured, you can start making requests. Just type what you want to do into the chat box.

**Examples:**
* `list all the text files on my desktop`
* `create a new folder called "My Project" in my documents`
* `what is my current IP address?`

The AI will show you its thought process, the command it's about to execute, and then the output from that command.

---

## Built With

* [.NET 6](https://dotnet.microsoft.com/)
* [WPF (Windows Presentation Foundation)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
* [Prism Framework](https://prismlibrary.com/) for MVVM
* [Google Gemini API](https://ai.google.dev/)
