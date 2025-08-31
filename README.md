# PowerShell Agent
A Python desktop app that lets you control Windows with natural language. Ask the AI what you want to do, and it executes the right PowerShell commands automatically.

## Features
* **Natural Language to PowerShell:** Just ask what you want - the AI figures out the command
* **Modern Dark UI:** Clean interface built with CustomTkinter
* **Autonomous Execution:** AI runs commands directly without asking permission
* **Full Context:** Remembers conversation for multi-step tasks

## Quick Start
1. **Install Python 3.8+** from [python.org](https://www.python.org/downloads/)
2. **Install packages:**
   ```bash
   pip install customtkinter google-genai python-dotenv
   ```
3. **Create `.env` file** with your API key:
   ```
   GEMINI_API_KEY=your_key_here
   ```
4. **Run:**
   ```bash
   python powershell_agent.py
   ```

## Usage Examples
* `what is today's date?`
* `list files on my desktop`
* `create a folder called "My Project"`
* `what's my IP address?`
* `show running processes`

Get your API key from [Google AI Studio](https://makersuite.google.com/app/apikey).

## Built With
Python • CustomTkinter • Google Gemini API • PowerShell