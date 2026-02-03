# AIChatDiscordBotWeb

 ![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)

**AIChatDiscordBotWeb** is a local AI-powered Discord bot running on ASP.NET Core. It integrates local LLMs (via Ollama), web search, and image generation into a seamless chat experience.

## ✨ Features
**What can this local AI do in Discord?**

* 💬 **Chat:** Natural conversation with context awareness.
* 📄 **File Analysis:** Upload **PDF, TXT, or DOCX** files, and the bot will read and answer questions based on them.
* 👁️ **Vision:** Upload images and ask the bot to describe or analyze them (requires a Vision model).
* 🌐 **Web Search:** The bot can search the internet for the latest news and real-time information (requires Serper web api key).
* 🎨 **Image Generation:** The bot can generate images locally using **ComfyUI** (requires installed ComfyUI).
* 🗣️ **Speaking:** The bot can speak in voice channels (requires installed second Discord bot).
* 🎵 **Music Generation:** Create original audio and music (requires separate Music Gen Python program running).
* 🐍 **Python Execution:** Write and execute Python code snippets (requires separate Python Execution environment/program running).

---

## 🛠️ Tech Stack & Libraries
Built with **C# ASP.NET Core Web App (MVC)** on **.NET 9.0**.

**Nuget Libraries used:**
* `DSharpPlus` (4.5.1)
* `DSharpPlus.Interactivity` (4.5.1)
* `DSharpPlus.SlashCommands` (4.5.1)
* `Microsoft.SemanticKernel` (1.66.0)
* `Microsoft.SemanticKernel.Connectors.Ollama` (1.66.0-alpha)
* `Microsoft.KernelMemory.Core` (0.98.250508.3)
* `Microsoft.KernelMemory.AI.Ollama` (0.98.250508.3)
* `DocumentFormat.OpenXml` (3.3.0)
* `PdfPig` (0.1.11)

---

## ⚙️ Installation & Setup

### 1. Discord Developer Portal
1.  Go to the [Discord Developer Portal](https://discord.com/developers/applications).
2.  Create a new application and go to the **Bot** tab.
3.  **Privileged Gateway Intents:** Scroll down and enable the intents shown below:
    ![privilage](https://github.com/user-attachments/assets/f6a7ae67-acf6-4d11-a479-7b55df3fab02)
4.  **Installation:** Ensure the bot has the following permissions enabled:
    ![bot](https://github.com/user-attachments/assets/61b00634-6aee-474f-99e3-549f142380e4)
5.  Copy your **Bot Token**. You will need this for the config.

### 2. Ollama Setup (The Brain)
1.  Download and install [Ollama](https://ollama.com/).
2.  Open your console/terminal and run:
    ```
    ollama serve
    ```
    *This must be running in the background for the bot to work.*
3.  Note the **port number** (usually the last 5 digits in the error/status message) to put in your config.
    ![comm](https://github.com/user-attachments/assets/c8af5b48-042d-4a74-a9d5-e5a5b798a010)
4.  Download a model (e.g., `ollama pull PetrosStav/gemma3-tools:4b`) via the command line.
5.  Run the list command to get the exact name:
    ```
    ollama list
    ```
    ![ModelName](https://github.com/user-attachments/assets/cb687521-ea53-44fe-8a0c-28db29f85d5e)
6.  Copy the **full model name** into your config page.

---

### 3. Web Search (Optional)
To enable internet access for the bot:
1.  Get a free or paid API key from [Serper.dev](https://serper.dev).
2.  Paste the API Key into the `AIChatDiscordBotWeb` Configuration.

<img width="800" alt="serper config" src="https://github.com/user-attachments/assets/03d80220-4b49-4af9-87b6-05c4f5b7c5e7" />
<img width="600" alt="serper settings" src="https://github.com/user-attachments/assets/faf84549-edd5-4569-afd3-4abdc16c611d" />

---

### 4. ComfyUI - Image Generation (Optional)
To enable image generation:
1.  Download [ComfyUI](https://www.comfy.org/download).
2.  **Recommended Models (Text-to-Image):**
    * `NetaYume Lumina Text to Image` (Search in ComfyUI templates).
    * [Lumina Image 2.0 Repackaged](https://huggingface.co/Comfy-Org/Lumina_Image_2.0_Repackaged/blob/main/all_in_one/lumina_2.safetensors).
3.  Place models in: `C:\Users\YourPCName\Documents\ComfyUI\models\checkpoints`.
4.  **Important:** Go to ComfyUI Settings and enable **Dev Mode**. <br>
    <img width="500" alt="dev mode" src="https://github.com/user-attachments/assets/ef2d3ec2-fa76-40f8-881d-ad576c1dae90" />
5.  Add your ComfyUI image directory path in the `AIChatDiscordBotWeb` Configuration.
    <img width="600" alt="comfy config" src="https://github.com/user-attachments/assets/f83f45ff-a339-40b4-a343-d5c18a14a679" />

---

### 5. Speaking Discord Bot (Optional)
To enable voice capabilities (Voice-to-Text and Text-to-Speech), you must run the secondary Python bot alongside this one.
* **Repository:** [RafiBG/AITalkingDiscordBot](https://github.com/RafiBG/AITalkingDiscordBot)
* **Role:** Handles microphone input and generates voice audio using Whisper STT and Piper TTS.
* **Setup:** Follow the detailed instructions in the link above to install FFmpeg and Piper TTS.

### 6. Music Generation (Optional)
To enable local music and melody generation within Discord.
* **Repository:** [RafiBG/AIMusicGenerator](https://github.com/RafiBG/AIMusicGenerator)
* **Role:** Processes music generation requests and returns audio files to the main bot.
* **Setup:** Follow the detailed instructions in the link above.

### 7. Python Execution (Optional)
To enable the bot to write and run actual Python scripts.
* **Repository:** [RafiBG/AIPythonRun](https://github.com/RafiBG/AIPythonRun)
* **Role:** Acts as a sandbox/runner to execute code safely and return the result to the Discord AI bot.
* **Setup:** Follow the detailed instructions in the link above.
  
---

## 🎮 Slash Commands

| Command | Description |
| :--- | :--- |
| `/ask [message] (file) (image)` | **Main command.** Chat with AI. Optionally attach a file (PDF/TXT/DOCX) for context or an image for vision analysis. |
| `/ask_multi` | Ask three AI models the same question and the main one will summaries the answer. |
| `/forgetme` | Starts a fresh conversation loop only for you (clears short-term user memory). |
| `/reset` | **Global Reset.** Resets all chats and starts a new conversation for everyone. |
| `/help` | Shows the help menu with these commands. |

---

## 📸 Gallery & Examples

**Chat & Vision Capabilities**
<p float="left">
 <img src="https://github.com/user-attachments/assets/4afd4a47-f2e9-4e17-8ab1-98fd21b9ed72" width="30%" />
 <img src="https://github.com/user-attachments/assets/f86a8202-d71f-48fd-b717-30c16f75920d" width="30%"  />
 <img src="https://github.com/user-attachments/assets/d5fcaa3d-9f01-4c59-992c-0e5b699a88e7" width="30%"  />

</p>

**File Analysis & Web Search**
<p float="left">
   <img src="https://github.com/user-attachments/assets/55f35103-7724-4325-862d-fa3ae0c594c3" width="30%" />
   <img src="https://github.com/user-attachments/assets/47e3cae9-1c34-47b5-b49a-0dc1ee158869" width="30%" />
</p>

**Image Generation & Music Generation**
<p float="left">
  <img src="https://github.com/user-attachments/assets/21588bf2-a3dc-47a1-9829-df216a7cfb22" width="30%" />
  <img src="https://github.com/user-attachments/assets/d5ae4524-9397-4763-8d29-c5574a619cae" width="30%" />
  <img src="https://github.com/user-attachments/assets/e891f861-577d-42c7-9e92-0c5e2c399bee" width="30%" />
  <img src="https://github.com/user-attachments/assets/f453b969-6e00-4703-9f78-2fe596f888f0" width="30%" />
</p>

**Long term memory**
<p float="left">
 <img src="https://github.com/user-attachments/assets/51bc1f34-47a3-4cd3-8d48-1de4baef715b" width="30%" />
 <img src="https://github.com/user-attachments/assets/b7a270f2-9411-4e88-9ddf-5c856e3c867b" width="30%" />
</p>

**Python execution**
<p float="left">
 <img src="https://github.com/user-attachments/assets/7149dd7c-6eab-497a-b5be-f88073c951f3" width="30%" />
</p>

