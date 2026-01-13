#!/bin/bash

# One-line installer for AI Knowledge Base System
# Run: curl -sSL [URL] | bash

cat << 'BANNER'
╔═══════════════════════════════════════════════════════╗
║        AI Knowledge Base Auto-Collection System       ║
║              Automatic Link List Manager              ║
╚═══════════════════════════════════════════════════════╝
BANNER

echo -e "\n🤖 Setting up AI Knowledge Base System...\n"

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Detect OS
if [[ "$OSTYPE" == "darwin"* ]]; then
    OS="macOS"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OS="Linux"
else
    echo "❌ Unsupported OS: $OSTYPE"
    exit 1
fi

echo "✓ Detected OS: $OS"

# Install dependencies
echo -e "\n📦 Installing dependencies..."

if [ "$OS" == "macOS" ]; then
    # Check for Homebrew
    if ! command_exists brew; then
        echo "Installing Homebrew..."
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    fi
    
    # Install required tools
    brew install jq fswatch pandoc
    
elif [ "$OS" == "Linux" ]; then
    # Update package manager
    sudo apt-get update
    
    # Install required tools
    sudo apt-get install -y jq fswatch pandoc xclip
fi

echo "✓ Dependencies installed"

# Run the main setup
if [ -f ~/Desktop/AI_Knowledge_Base_System/setup_knowledge_base.sh ]; then
    echo -e "\n🚀 Running setup script..."
    chmod +x ~/Desktop/AI_Knowledge_Base_System/setup_knowledge_base.sh
    ~/Desktop/AI_Knowledge_Base_System/setup_knowledge_base.sh
else
    echo "❌ Setup script not found. Please ensure all files are in ~/Desktop/AI_Knowledge_Base_System/"
    exit 1
fi

# Success message
cat << 'SUCCESS'

✨ ════════════════════════════════════════════════════ ✨
   
   🎉 AI Knowledge Base System Successfully Installed! 🎉
   
   The system is now actively monitoring your clipboard
   for link lists and will auto-save them in multiple
   formats to both local storage and Google Drive.
   
   Quick Commands:
   • kb-save "Title"  - Save links manually
   • kb-sync          - Check sync status  
   • kb-log           - View activity log
   
   Try it now: Copy any list of URLs!
   
✨ ════════════════════════════════════════════════════ ✨

SUCCESS

# Source aliases in current shell
source ~/Desktop/AI_Knowledge_Base_System/aliases.sh

# Show example
echo -e "\n📋 Example saved: AR VFX GitHub Projects"
echo "   Check: ~/Desktop/AI_Knowledge_Base/Claude/links/"
echo -e "\n🔄 System is running in the background"
