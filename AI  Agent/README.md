# AI Agent

**AI assistant for the Unity Editor with integration of OpenAI GPT-4, Anthropic Claude, Llama3, and a simplified chat interface. Includes scene generation, asset search, progress indicators, and result saving.**

## Installation

1. Copy the package folder (`com.yourcompany.aiaagent`) into your project or add it via Unity Package Manager (Add package from disk).
2. Open the window: Window → AI → AI Agent or Tools → AI Agent.
3. Create a settings asset: Right-click in Project → Create → AI Agent → Settings.
4. Enter API keys for your preferred AI services (OpenAI, Anthropic Claude).

## Features

### Simplified Chat Interface
- **Unified chat interface**: Compact and user-friendly chat-style window
- **Quick action buttons**: Scene generation, clarification pause, asset search
- **Easy access to settings**: All API keys and configuration in a separate window
- **Developer-focused workflow**: Improved experience for game creators
- **Save results**: Option to save AI responses to a file
- **Progress indicators**: Animated spinner while waiting for AI response
- **Tooltips**: Short descriptions for all features on hover

### Core Functionality
- **Context-aware chat assistant** (ChatGPT/Claude-like)
- **Generate and insert C# code** directly into your project
- **One-click scene generation** with customizable parameters
- **Clarification pause** during complex tasks
- **Integrated free asset search** in the Unity Asset Store
- **Dedicated settings window** for all configuration and API keys
- **Chat history saving** for continuous workflow
- **AI response analysis** for automatic command execution

## Quick Start

1. Open the AI Agent window: **Window → AI → AI Agent**
2. Click the **Settings** button to configure your API keys
3. Enter your OpenAI or Anthropic Claude API key in the settings window
4. Return to the main window and use the chat to interact with the AI

### Using Main Features

1. **Scene Generation**: 
   - Click the **Generate Scene** button for quick creation of a basic scene
   - A scene with a player, ground, and camera will be created
   - A player movement script will be added
   - The scene is automatically saved in `Assets/Scenes`

2. **Asset Search**:
   - Click the **Find Assets** button
   - The Asset Store will open with resources matching your current game type

3. **Clarification Pause**:
   - If the AI is generating a long response, click **Pause for Clarification**
   - This allows you to add details to your request before completion

## Requirements
- Unity 2021.3+
- API key for at least one service: OpenAI or Anthropic Claude
- Internet connection for API requests, asset search, and code download

## Controls

### Simplified Interface
- **Chat interface**: Messenger-like chat window
- **Three main action buttons**: 
  - **🏗️ Generate Scene**: Quickly create a basic scene with current parameters
  - **⏸️ Pause for Clarification**: Pause generation to add more details
  - **🔍 Find Assets**: Automatically search for relevant assets in the Asset Store
- **Settings**: Access a separate window for all configurations
- **Press Enter** in the input field to send a message

### Usage Examples
- **Ask the AI**: "Create a character movement script with double jump support"
- **Content generation**: "Generate an inventory system with drag-and-drop functionality"
- **Problem analysis**: "Why does my script throw a NullReferenceException in Start?"
- **Learning**: "Explain how Rigidbody works in Unity and provide examples"

## AudioManager

The new AudioManager enables voice features in AI Agent:

### AudioManager Features:
- **Text-to-Speech (TTS)**: Voice playback of AI responses
- **Speech-to-Text (STT)**: Recognize user voice input
- **Sound notifications**: Indicate various interface events

### Integration with external services:
For full functionality, configure:
- Google Cloud Text-to-Speech API
- Google Cloud Speech-to-Text API
- Microsoft Azure Cognitive Services
- Amazon Polly / Amazon Transcribe

### Testing AudioManager:
You can test AudioManager availability via the menu:
`Window > AI Assistant > Test Audio Manager`

## New Features (version 1.4.0)

### User Interface Improvements
- **Syntax highlighting** for code blocks in AI responses
- **Contextual buttons** for quick copy and save to file
- **Animated progress indicator** for requests
- **Tooltips** for all buttons and controls
- **Auto-scrolling** to the latest message in chat history

### Technical Enhancements
- **Update check** on startup
- **Automatic detection and installation** of required Unity packages
- **Improved error handling** for stability
- **Extended command support** for creating folders and scene objects
- **Better code block handling** for various programming languages

---

**© 2024 AI Agent Team**

### Code Formatting

Supports various programming languages in markdown format:

```csharp
// C# code with syntax highlighting
public class MyClass {
    public void MyMethod() {
        Debug.Log("Hello!");
    }
}
```

Special commands are also recognized:

- `#create_object:Cube` - Create a cube in the scene
- `#create_script:PlayerController.cs` - Create a new script
- `#create_folder:Models` - Create a new folder
- `#find_assets:3D Characters` - Find assets in the Asset Store
- `#generate_scene` - Generate a basic scene