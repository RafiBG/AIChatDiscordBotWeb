# Latest version AIChatDiscordBotWeb v1.0.1
The AI chat bot can now see images. The AI model must be multimodel with vision or images in him. From Ollama model, Gemma3 has vision and other models. Same way as you give him file there will be second optional named image.
# Used libraries (Nugets)
Made with C# on .NET 9.0 and libraries: DSharpPlus 4.5.1, DsharpPlus.Interactivity 4.5.1, DSharpPlus.SlashCommands 4.5.1, OllamaSharp 5.4.4, DocumentFormat.OpenXml 3.3.0, PdfPig 0.1.11
# Steps to make it work
Go to Discord developer portal link: https://discord.com/developers/applications. Make your own discord bot and get the token that will be used in configOllama. Before you leave the website go to Bot and turn on the ones that you see on the image. ![privilage](https://github.com/user-attachments/assets/f6a7ae67-acf6-4d11-a479-7b55df3fab02)

Now the Bot Permissions are shown in the image that you must check to work. ![bot](https://github.com/user-attachments/assets/61b00634-6aee-474f-99e3-549f142380e4)

### Ollama
Install Ollama from this link: https://ollama.com/
From the official ollama website you can download AI model for your discord bot: https://ollama.com/search
Open your console and enter: ollama serve
It will show you the error and the last 5 numbers that you need to put in configOllama to connect the AI.
![comm](https://github.com/user-attachments/assets/c8af5b48-042d-4a74-a9d5-e5a5b798a010)

Ollama must be running in the background to be able to connect to your local AI chat bot for discord.
Run the command in the console: ollama list
Copy the full name of the model you downloaded. We will need it for configOllama
Example of what you will be looking for the name of the model in the console.
![ModelName](https://github.com/user-attachments/assets/cb687521-ea53-44fe-8a0c-28db29f85d5e)

# Slash commands
/ask (your message) (optional file to read like pdf,txt,docx) (optional image to see) <br/>
/forgetme - Start a fresh conversation with the AI only for you. <br/>
/reset - Resets all the user's chats and starts a whole new conversation for everyone. <br/>
/help - Show all the commands for the AI chat bot.

## Images
<img width="788" height="631" alt="image" src="https://github.com/user-attachments/assets/a485b027-522d-492f-b7d8-ee4596c5ba7f" /> <br>

<img width="733" height="817" alt="image" src="https://github.com/user-attachments/assets/6e1c9848-3055-4eaa-ab03-08aca79097ba" /> <br>

<img width="574" height="330" alt="image" src="https://github.com/user-attachments/assets/4c763117-20cb-4547-9b53-0bc17443fec2" /> <br>
<img width="587" height="679" alt="image" src="https://github.com/user-attachments/assets/2d224286-15e1-4890-bfa6-6701405b654e" />

