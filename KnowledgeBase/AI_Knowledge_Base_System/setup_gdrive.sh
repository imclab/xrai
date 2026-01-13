#!/bin/bash

# Google Drive Setup for AI Knowledge Base System

echo "🔗 Setting up Google Drive integration..."

# Check if Google Drive is mounted
GDRIVE_PATH="$HOME/Google Drive/My Drive"
if [ ! -d "$GDRIVE_PATH" ]; then
    GDRIVE_PATH="$HOME/Google Drive"
fi

if [ ! -d "$GDRIVE_PATH" ]; then
    echo "❌ Google Drive not found. Please ensure Google Drive desktop app is installed."
    echo "   Download from: https://www.google.com/drive/download/"
    exit 1
fi

echo "✓ Google Drive found at: $GDRIVE_PATH"

# Create folder structure in Google Drive
echo "📁 Creating Google Drive folders..."

AI_ASSISTANTS=("Claude" "ChatGPT" "Gemini" "Copilot" "Perplexity" "Bard")

mkdir -p "$GDRIVE_PATH/AI_Knowledge_Base"

for ai in "${AI_ASSISTANTS[@]}"; do
    mkdir -p "$GDRIVE_PATH/AI_Knowledge_Base/$ai"/{links,documents,exports}
    echo "  ✓ Created $ai folders"
done

# Create sync configuration
cat > "$HOME/Desktop/AI_Knowledge_Base_System/gdrive_sync.conf" << EOF
# Google Drive Sync Configuration
GDRIVE_PATH="$GDRIVE_PATH"
SYNC_ENABLED=true
SYNC_INTERVAL=300  # 5 minutes
AUTO_SYNC_ON_SAVE=true

# Sync folders
SYNC_FOLDERS=(
    "Claude/links"
    "ChatGPT/links"
    "Gemini/links"
    "Copilot/links"
    "Perplexity/links"
    "Bard/links"
)
EOF

echo "✓ Google Drive integration configured"
echo "📍 Google Drive Knowledge Base: $GDRIVE_PATH/AI_Knowledge_Base"
