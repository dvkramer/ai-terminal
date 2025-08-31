import customtkinter as ctk
from google import genai
from google.genai import types
import subprocess
import threading
import os
from dotenv import load_dotenv

# --- Configuration ---
load_dotenv()
# Try to get API key from .env file
api_key = os.getenv("GEMINI_API_KEY")

# --- Core PowerShell Function & AI Tool Definition ---

def run_powershell_script(script: str) -> dict:
    """
    Executes a PowerShell script on the local Windows machine and returns its output.
    This function executes scripts directly without user confirmation.
    """
    try:
        # Using -NoProfile makes execution faster and cleaner for automation
        # Using -Command ensures better handling of complex scripts and quotes
        result = subprocess.run(
            ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script],
            capture_output=True,
            text=True,
            check=False,
            timeout=60  # 60-second timeout to prevent runaway scripts
        )
        
        # Consolidate output for the model. It's crucial for the AI to see errors.
        if result.stderr:
            output = f"STDOUT:\n{result.stdout}\n\nSTDERR:\n{result.stderr}"
        else:
            output = result.stdout

        # The function response must be a dictionary as per the API docs
        return {"output": output.strip()}

    except subprocess.TimeoutExpired:
        return {"output": "ERROR: Script execution timed out after 60 seconds."}
    except Exception as e:
        return {"output": f"ERROR: A Python exception occurred while trying to run the script: {str(e)}"}

# Define the function declaration for the model (following new API format)
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

        # Store API key and initialize client if available
        self.api_key = api_key
        self.client = None
        self.tools = None
        self.config = None
        
        if self.api_key:
            self.initialize_ai()
        else:
            self.add_to_chat("System", "No API key found. Use '/api YOUR_KEY' to set your Gemini API key.")

        # Keep track of conversation history
        self.conversation_history = []

        # --- UI Layout ---
        self.grid_columnconfigure(0, weight=1)
        self.grid_rowconfigure(0, weight=1)

        self.textbox = ctk.CTkTextbox(self, state="disabled", wrap="word", font=("Consolas", 12))
        self.textbox.grid(row=0, column=0, padx=10, pady=10, sticky="nsew")

        self.entry = ctk.CTkEntry(self, placeholder_text="Ask me to do anything on this system...")
        self.entry.grid(row=1, column=0, padx=10, pady=10, sticky="ew")
        self.entry.bind("<Return>", self.send_message_event)
    
    def initialize_ai(self):
        """Initialize the AI client and tools with the API key."""
        try:
            self.client = genai.Client(api_key=self.api_key)
            self.tools = types.Tool(function_declarations=[powershell_function_declaration])
            self.config = types.GenerateContentConfig(
                tools=[self.tools],
                system_instruction="You are a powerful, autonomous Windows assistant. Your purpose is to directly help the user by executing PowerShell commands to accomplish their goals. When a user's request requires OS interaction, you must call the `run_powershell_script` function with the appropriate script. Be efficient and act directly. After executing a script, summarize the result for the user."
            )
            self.conversation_history = []
            return True
        except Exception as e:
            self.add_to_chat("System Error", f"Failed to initialize AI: {str(e)}")
            return False
    
    def update_api_key(self, new_key: str):
        """Update the API key and save it to .env file."""
        self.api_key = new_key
        
        # Save to .env file
        env_path = ".env"
        env_content = f"GEMINI_API_KEY={new_key}\n"
        
        # If .env exists, update it; otherwise create it
        if os.path.exists(env_path):
            with open(env_path, 'r') as file:
                lines = file.readlines()
            
            # Replace existing key or add new one
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
        
        # Initialize AI with new key
        if self.initialize_ai():
            self.add_to_chat("System", "API key updated successfully!")
        else:
            self.add_to_chat("System Error", "Failed to initialize AI with new key.")
    
    def handle_api_command(self, command: str) -> bool:
        """Handle /api command to set API key. Returns True if handled."""
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
        """Helper to add text to the chat window safely from any thread."""
        self.textbox.configure(state="normal")
        self.textbox.insert("end", f"{role}:\n{text}\n\n")
        self.textbox.configure(state="disabled")
        self.textbox.see("end")

    def send_message_event(self, event):
        """Handle the send event from the UI."""
        prompt = self.entry.get()
        if not prompt:
            return
            
        self.add_to_chat("You", prompt)
        self.entry.delete(0, "end")
        
        # Check for /api command
        if self.handle_api_command(prompt):
            return
        
        # Check if AI is initialized
        if not self.client:
            self.add_to_chat("System Error", "No API key set. Use '/api YOUR_KEY' to set your Gemini API key.")
            return
        
        # Run the AI interaction in a separate thread to keep the UI responsive
        thread = threading.Thread(target=self.process_ai_interaction, args=(prompt,))
        thread.daemon = True
        thread.start()

    def process_ai_interaction(self, prompt: str):
        """The core logic for a single turn of conversation."""
        try:
            # Add user message to conversation history
            self.conversation_history.append(types.Content(
                role="user", 
                parts=[types.Part(text=prompt)]
            ))

            # Send the request with function declarations
            response = self.client.models.generate_content(
                model="gemini-2.5-flash",
                contents=self.conversation_history,
                config=self.config,
            )
            
            # Add model response to conversation history
            self.conversation_history.append(response.candidates[0].content)
            
            # Loop as long as the model wants to call a function
            while (response.candidates[0].content.parts and 
                   hasattr(response.candidates[0].content.parts[0], 'function_call') and 
                   response.candidates[0].content.parts[0].function_call):
                
                function_call = response.candidates[0].content.parts[0].function_call
                
                if function_call.name == "run_powershell_script":
                    script_to_run = function_call.args['script']
                    self.add_to_chat("Agent (Action)", f"Executing PowerShell:\n---\n{script_to_run}\n---")
                    
                    # Execute the script and get the result dictionary
                    function_response_dict = run_powershell_script(script_to_run)
                    
                    # Create function response part following the new API
                    function_response_part = types.Part.from_function_response(
                        name=function_call.name,
                        response={"result": function_response_dict},
                    )
                    
                    # Add function response to conversation history
                    self.conversation_history.append(types.Content(
                        role="user", 
                        parts=[function_response_part]
                    ))
                    
                    # Send the updated conversation back to the model
                    response = self.client.models.generate_content(
                        model="gemini-2.5-flash",
                        contents=self.conversation_history,
                        config=self.config,
                    )
                    
                    # Add the new response to conversation history
                    self.conversation_history.append(response.candidates[0].content)

                else:
                    self.add_to_chat("System Error", f"Model tried to call an unknown function: {function_call.name}")
                    break # Exit loop if we get an unexpected function call
            
            # Once the loop finishes, the final response is the model's text summary
            if response.text:
                self.add_to_chat("Agent", response.text)
            else:
                self.add_to_chat("System Error", "No text response received from model")

        except Exception as e:
            error_msg = str(e)
            
            # Clean up common API errors
            if "400 INVALID_ARGUMENT" in error_msg and "API key not valid" in error_msg:
                self.add_to_chat("System Error", "Invalid API key. Use '/api YOUR_KEY' to set a valid Gemini API key.")
            elif "403" in error_msg and "quota" in error_msg.lower():
                self.add_to_chat("System Error", "API quota exceeded. Check your Gemini API usage limits.")
            elif "401" in error_msg or "unauthorized" in error_msg.lower():
                self.add_to_chat("System Error", "Authentication failed. Check your API key with '/api YOUR_KEY'.")
            elif "429" in error_msg or "rate limit" in error_msg.lower():
                self.add_to_chat("System Error", "Rate limited. Please wait a moment before trying again.")
            elif "network" in error_msg.lower() or "connection" in error_msg.lower():
                self.add_to_chat("System Error", "Network error. Check your internet connection.")
            else:
                # Show full error for debugging
                self.add_to_chat("System Error", f"An unexpected error occurred: {error_msg}")

# --- Application Entry Point ---
if __name__ == "__main__":
    app = PowerShellAgentApp()
    app.mainloop()