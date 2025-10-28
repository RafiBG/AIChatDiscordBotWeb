# Latest version AIChatDiscordBotWeb v1.0.1
The AI chat bot can now see images. The AI model must be multimodel with vision or images in him. From Ollama model, Gemma3 has vision and other models. Same way as you give him file there will be second optional named image.
# What can the local AI do in Discord ?
It can chat with you. <br>
Help you with files like PDF,TXT,DOCX <br>
Can see images. (You must put model with vision like Gemma 3) <br>
Search the internet for latest news. (The model must be able to use tools) <br>
Generate images (The model must be able to use tools, ComfyUI)
# Used libraries (Nugets)
Made with C# ASP.Net Core Web App (Model-View-Controller) on .NET 9.0 and libraries: <br> DSharpPlus 4.5.1, <br> DsharpPlus.Interactivity 4.5.1, <br> DSharpPlus.SlashCommands 4.5.1, <br> Microsoft.SemanticKernel 1.66.0, <br> Microsoft.SemanticKernel.Connectors.Ollama 1.66.0-alpha, <br> DocumentFormat.OpenXml 3.3.0, <br> PdfPig 0.1.11
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
### Serper (Web search if you want the AI to look for new information) 
You can get a free or paid api key. Go to this link: 
Get a api key and paste it in the AIChatDiscordBotWeb Configuration
<img width="1850" height="257" alt="image" src="https://github.com/user-attachments/assets/03d80220-4b49-4af9-87b6-05c4f5b7c5e7" />
<img width="974" height="207" alt="image" src="https://github.com/user-attachments/assets/faf84549-edd5-4569-afd3-4abdc16c611d" />

### ComfyUI (If you want to ask it to generate images)
You must download ComfyUI. Here is link to it : https://www.comfy.org/download
The 2 tested and working ones that are Text to Image: Comfy-Org/Lumina_Image_2.0_Repackaged and NetaYume Lumina Text to Image.
Go to templates and search "NetaYume Lumina Text to Image" and download all of it. 
If you want to use Lumina Image 2 go https://huggingface.co/Comfy-Org/Lumina_Image_2.0_Repackaged/blob/main/all_in_one/lumina_2.safetensors and download it. Then go to your PC directory: C:\Users\YourPCName\Documents\ComfyUI\models\checkpoints and add it there.
Go to ComfyUI settings enable Dev mode. 
<img width="792" height="342" alt="image" src="https://github.com/user-attachments/assets/ef2d3ec2-fa76-40f8-881d-ad576c1dae90" /> <br>
Add your ComfyUI image directory in the Configuration of the AIChatDiscordBotWeb.
<img width="971" height="391" alt="image" src="https://github.com/user-attachments/assets/f83f45ff-a339-40b4-a343-d5c18a14a679" />
ComfyUI must be working for the AI model to create images and send them in discord.
# Slash commands
/ask (your message) (optional file to read like pdf,txt,docx) (optional image to see) <br/>
/forgetme - Start a fresh conversation with the AI only for you. <br/>
/reset - Resets all the user's chats and starts a whole new conversation for everyone. <br/>
/help - Show all the commands for the AI chat bot.

## Images
<img width="788" height="631" alt="image" src="https://github.com/user-attachments/assets/a485b027-522d-492f-b7d8-ee4596c5ba7f" /> <br>

<img width="733" height="817" alt="image" src="https://github.com/user-attachments/assets/6e1c9848-3055-4eaa-ab03-08aca79097ba" /> <br>

<img width="727" height="760" alt="image" src="https://github.com/user-attachments/assets/8ea61363-c0db-4c93-9c28-a7fac76f024c" /> <br>

 <img width="707" height="435" alt="file image" src="https://github.com/user-attachments/assets/47e3cae9-1c34-47b5-b49a-0dc1ee158869" />
<br>
<img width="587" height="679" alt="image" src="https://github.com/user-attachments/assets/2d224286-15e1-4890-bfa6-6701405b654e" />
<br>
<img width="749" height="716" alt="image" src="https://github.com/user-attachments/assets/55f35103-7724-4325-862d-fa3ae0c594c3" />
<br> 
<img width="507" height="776" alt="image" src="https://github.com/user-attachments/assets/21588bf2-a3dc-47a1-9829-df216a7cfb22" />
<br>
<img width="521" height="787" alt="image" src="https://github.com/user-attachments/assets/d5ae4524-9397-4763-8d29-c5574a619cae" />
