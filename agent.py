import customtkinter as ctk
from google import genai
from google.genai import types
import subprocess
import threading
import os
from dotenv import load_dotenv

# --- Configuration ---
load_dotenv()
api_key = os.getenv("GEMINI_API_KEY")

# --- Core PowerShell Function & AI Tool Definition ---

def run_powershell_script(script: str) -> dict:
    """Executes a PowerShell script and returns its output."""
    try:
        result = subprocess.run(
            ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script],
            capture_output=True,
            text=True,
            check=False,
            timeout=60
        )
        if result.stderr:
            output = f"STDOUT:\n{result.stdout}\n\nSTDERR:\n{result.stderr}"
        else:
            output = result.stdout
        return {"output": output.strip()}
    except subprocess.TimeoutExpired:
        return {"output": "ERROR: Script execution timed out after 60 seconds."}
    except Exception as e:
        return {"output": f"ERROR: A Python exception occurred: {str(e)}"}

powershell_function_declaration = {
    "name": "run_powershell_script",
    "description": (
        "Executes a PowerShell script on the local Windows OS to manage files, "
        "processes, services, network, or system state. Use this for any task "
        "requiring direct OS interaction. The script runs autonomously."
    ),
    "parameters": {
        "type": "object",
        "properties": {
            "script": {
                "type": "string",
                "description": "A valid, self-contained PowerShell script to execute."
            },
        },
        "required": ["script"],
    },
}

# --- Main Application Class ---

class PowerShellAgentApp(ctk.CTk):
    def __init__(self):
        super().__init__()

        self.title("PowerShell Agent")
        self.geometry("800x600")
        ctk.set_appearance_mode("dark")
        ctk.set_default_color_theme("blue")

        # Initialize attributes
        self.api_key = api_key
        self.client = None
        self.tools = None
        self.config = None
        self.conversation_history = []

        # --- UI Layout --- (MOVED UP)
        # Create all UI widgets first before any logic uses them.
        self.grid_columnconfigure(0, weight=1)
        self.grid_rowconfigure(0, weight=1)

        self.textbox = ctk.CTkTextbox(self, state="disabled", wrap="word", font=("Consolas", 12))
        self.textbox.grid(row=0, column=0, padx=10, pady=10, sticky="nsew")

        self.entry = ctk.CTkEntry(self, placeholder_text="Ask me to do anything on this system...")
        self.entry.grid(row=1, column=0, padx=10, pady=10, sticky="ew")
        self.entry.bind("<Return>", self.send_message_event)
        self.entry.after(10, self.entry.focus_set)

        # --- AI Initialization and Startup Message --- (NOW SAFE TO RUN)
        # Now that self.textbox exists, we can safely call self.add_to_chat().
        if self.api_key:
            self.initialize_ai()
        else:
            self.add_to_chat("System", "No API key found. Use '/api YOUR_KEY' to set your Gemini API key.")


    def initialize_ai(self):
        """Initialize the AI client and tools with the API key."""
        try:
            genai.configure(api_key=self.api_key) # Recommended way to configure
            self.model = genai.GenerativeModel(
                model_name="gemini-1.5-flash", # Updated model name for better tool use
                tools=[run_powershell_script],
                system_instruction=(
                    "You are a powerful, autonomous Windows assistant. "
                    "Your purpose is to directly help the user by executing PowerShell commands. "
                    "When a user's request requires OS interaction, you must call the "
                    "`run_powershell_script` function. Be efficient and act directly. "
                    "After executing a script, summarize the result for the user. "
                    "This application does not support markdown or LaTeX formatting. "
                    "Do NOT attempt to use **bold**, *italics*, or other forms of markdown/LaTeX formatting. "
                    "Your turn is not complete until you output the phrase 'END OF TURN.' at the end of your final message."
                )
            )
            # Start a chat session to maintain history
            self.chat = self.model.start_chat(enable_automatic_function_calling=True)
            return True
        except Exception as e:
            self.add_to_chat("System Error", f"Failed to initialize AI: {str(e)}")
            return False

    def update_api_key(self, new_key: str):
        """Update the API key and save it to .env file."""
        self.api_key = new_key
        env_path = ".env"
        env_content = f"GEMINI_API_KEY={new_key}\n"

        if os.path.exists(env_path):
            with open(env_path, 'r') as file:
                lines = file.readlines()
            updated = False
            for i, line in enumerate(lines):
                if line.startswith("GEMINI_API_KEY="):
                    lines[i] = env_content
                    updated = True
                    break
            if not updated:
                lines.append(env_content)
            with open(env_path, 'w') as file:
                file.writelines(lines)
        else:
            with open(env_path, 'w') as file:
                file.write(env_content)

        if self.initialize_ai():
            self.add_to_chat("System", "API key updated successfully!")
        else:
            self.add_to_chat("System Error", "Failed to initialize AI with new key.")

    def handle_api_command(self, command: str) -> bool:
        """Handle /api command to set API key."""
        if command.startswith("/api "):
            new_key = command[5:].strip()
            if new_key:
                self.update_api_key(new_key)
                return True
            else:
                self.add_to_chat("System Error", "Please provide an API key: /api YOUR_KEY")
                return True
        return False

    def add_to_chat(self, role: str, text: str):
        """Helper to add text to the chat window safely."""
        # Remove 'END OF TURN.' from the visible text
        visible_text = text
        if visible_text.endswith("END OF TURN."):
            visible_text = visible_text[:-len("END OF TURN.")].rstrip()

        self.textbox.configure(state="normal")
        self.textbox.insert("end", f"{role}:\n{visible_text}\n\n")
        self.textbox.configure(state="disabled")
        self.textbox.see("end")

    def send_message_event(self, event):
        """Handle the send event from the UI."""
        prompt = self.entry.get()
        if not prompt:
            return

        self.add_to_chat("You", prompt)
        self.entry.delete(0, "end")

        if self.handle_api_command(prompt):
            return

        if not self.model: # Check if the model was initialized
            self.add_to_chat("System Error", "AI model not initialized. Use '/api YOUR_KEY' to set your API key.")
            return

        thread = threading.Thread(target=self.process_ai_interaction, args=(prompt,))
        thread.daemon = True
        thread.start()

    def process_ai_interaction(self, prompt: str):
        """Sends a message to the chat session and handles the response."""
        try:
            # Automatic function calling will handle the loop
            response = self.chat.send_message(prompt)

            # Check for tool calls in the history to display them
            last_message = self.chat.history[-1]
            if last_message.role == 'model' and last_message.parts:
                for part in last_message.parts:
                    if part.function_call:
                        fc = part.function_call
                        script_to_run = fc.args['script']
                        self.add_to_chat("Agent (Action)", f"Executing PowerShell:\n---\n{script_to_run}\n---")

            # The final response text after function calls are handled
            final_text = response.text.strip()
            if final_text:
                 self.add_to_chat("Agent", final_text)

        except Exception as e:
            self.add_to_chat("System Error", f"An unexpected error occurred: {str(e)}")


# --- Application Entry Point ---
if __name__ == "__main__":
    app = PowerShellAgentApp()
    app.mainloop()
