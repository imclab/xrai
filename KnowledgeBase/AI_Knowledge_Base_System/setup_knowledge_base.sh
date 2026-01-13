#!/bin/bash

# AI Knowledge Base Auto-Collection System
# This script sets up automatic link list detection and saving for AI assistants

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
LOCAL_BASE="$HOME/Desktop/AI_Knowledge_Base"
GDRIVE_BASE="$HOME/Google Drive/My Drive/AI_Knowledge_Base"
SYSTEM_DIR="$HOME/Desktop/AI_Knowledge_Base_System"
LOG_FILE="$SYSTEM_DIR/collection.log"

echo -e "${BLUE}=== AI Knowledge Base Auto-Collection Setup ===${NC}"

# Function to log messages
log_message() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" >> "$LOG_FILE"
}

# Create directory structure
create_directories() {
    echo -e "${YELLOW}Creating directory structure...${NC}"
    
    # AI Assistant directories
    AI_ASSISTANTS=("Claude" "ChatGPT" "Gemini" "Copilot" "Perplexity" "Bard")
    
    for ai in "${AI_ASSISTANTS[@]}"; do
        mkdir -p "$LOCAL_BASE/$ai"/{links,documents,exports}
        mkdir -p "$GDRIVE_BASE/$ai"/{links,documents,exports}
    done
    
    # System directories
    mkdir -p "$SYSTEM_DIR"/{scripts,templates,watchers,temp}
    
    echo -e "${GREEN}✓ Directory structure created${NC}"
}

# Install dependencies
install_dependencies() {
    echo -e "${YELLOW}Checking dependencies...${NC}"
    
    # Check for jq
    if ! command -v jq &> /dev/null; then
        echo "Installing jq..."
        if [[ "$OSTYPE" == "darwin"* ]]; then
            brew install jq
        else
            sudo apt-get update && sudo apt-get install -y jq
        fi
    fi
    
    # Check for fswatch (for file monitoring)
    if ! command -v fswatch &> /dev/null; then
        echo "Installing fswatch..."
        if [[ "$OSTYPE" == "darwin"* ]]; then
            brew install fswatch
        else
            sudo apt-get install -y fswatch
        fi
    fi
    
    # Check for pandoc (for format conversion)
    if ! command -v pandoc &> /dev/null; then
        echo "Installing pandoc..."
        if [[ "$OSTYPE" == "darwin"* ]]; then
            brew install pandoc
        else
            sudo apt-get install -y pandoc
        fi
    fi
    
    echo -e "${GREEN}✓ Dependencies installed${NC}"
}

# Create the enhanced link saving script
create_save_script() {
    cat > "$SYSTEM_DIR/scripts/save_links.sh" << 'EOF'
#!/bin/bash

# Enhanced link saving script with automatic format conversion

# Configuration
LOCAL_BASE="$HOME/Desktop/AI_Knowledge_Base"
GDRIVE_BASE="$HOME/Google Drive/My Drive/AI_Knowledge_Base"
SYSTEM_DIR="$HOME/Desktop/AI_Knowledge_Base_System"

# Function to extract links from text
extract_links() {
    local content="$1"
    echo "$content" | grep -Eo 'https?://[^[:space:]]+' | sort -u
}

# Function to detect AI assistant from content
detect_assistant() {
    local content="$1"
    if echo "$content" | grep -qi "claude"; then
        echo "Claude"
    elif echo "$content" | grep -qi "chatgpt\|gpt"; then
        echo "ChatGPT"
    elif echo "$content" | grep -qi "gemini"; then
        echo "Gemini"
    elif echo "$content" | grep -qi "copilot"; then
        echo "Copilot"
    else
        echo "Claude"  # Default
    fi
}

# Function to generate filename
generate_filename() {
    local title="$1"
    local timestamp=$(date +%Y%m%d_%H%M%S)
    local safe_title=$(echo "$title" | tr ' ' '_' | tr -cd '[:alnum:]_-')
    echo "${timestamp}_${safe_title}"
}

# Main function
main() {
    local title="${1:-Link_Collection}"
    local content
    
    # Read from stdin or clipboard
    if [ -t 0 ]; then
        # No stdin, try clipboard
        if command -v pbpaste &> /dev/null; then
            content=$(pbpaste)
        else
            content=$(xclip -selection clipboard -o)
        fi
    else
        content=$(cat)
    fi
    
    # Detect AI assistant
    local assistant=$(detect_assistant "$content")
    local filename=$(generate_filename "$title")
    
    # Extract links
    local links=$(extract_links "$content")
    local link_count=$(echo "$links" | wc -l | tr -d ' ')
    
    # Save in multiple formats
    local base_path="$LOCAL_BASE/$assistant/links/$filename"
    local gdrive_path="$GDRIVE_BASE/$assistant/links/$filename"
    
    # Markdown format
    cat > "${base_path}.md" << MD_EOF
# $title

**Date:** $(date '+%Y-%m-%d %H:%M:%S')  
**AI Assistant:** $assistant  
**Total Links:** $link_count

## Content

$content

## Extracted Links

$(echo "$links" | awk '{print "- " $0}')

---
*Auto-collected by AI Knowledge Base System*
MD_EOF

    # JSON format with metadata
    jq -n \
        --arg title "$title" \
        --arg assistant "$assistant" \
        --arg date "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
        --arg content "$content" \
        --argjson links "$(echo "$links" | jq -R . | jq -s .)" \
        '{
            title: $title,
            assistant: $assistant,
            date: $date,
            link_count: ($links | length),
            content: $content,
            links: $links,
            metadata: {
                version: "1.0",
                collector: "AI Knowledge Base System",
                format: ["md", "json", "html"]
            }
        }' > "${base_path}.json"
    
    # HTML format with styling
    cat > "${base_path}.html" << HTML_EOF
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>$title - AI Knowledge Base</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
        }
        .container {
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        h1 { color: #333; border-bottom: 3px solid #007bff; padding-bottom: 10px; }
        .metadata {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
        }
        .metadata p { margin: 5px 0; }
        .content {
            background: #f8f9fa;
            padding: 20px;
            border-left: 4px solid #007bff;
            margin: 20px 0;
        }
        .links {
            list-style: none;
            padding: 0;
        }
        .links li {
            margin: 10px 0;
            padding: 10px;
            background: #e9ecef;
            border-radius: 5px;
            word-break: break-all;
        }
        .links a {
            color: #007bff;
            text-decoration: none;
        }
        .links a:hover {
            text-decoration: underline;
        }
        .assistant-badge {
            display: inline-block;
            padding: 5px 15px;
            background: #007bff;
            color: white;
            border-radius: 20px;
            font-size: 14px;
            font-weight: bold;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>$title</h1>
        <div class="metadata">
            <p><strong>Date:</strong> $(date '+%Y-%m-%d %H:%M:%S')</p>
            <p><strong>AI Assistant:</strong> <span class="assistant-badge">$assistant</span></p>
            <p><strong>Total Links:</strong> $link_count</p>
        </div>
        
        <h2>Content</h2>
        <div class="content">
            <pre>$content</pre>
        </div>
        
        <h2>Extracted Links</h2>
        <ul class="links">
$(echo "$links" | while read -r link; do
    echo "            <li><a href=\"$link\" target=\"_blank\">$link</a></li>"
done)
        </ul>
        
        <hr>
        <p style="text-align: center; color: #666; font-size: 14px;">
            Auto-collected by AI Knowledge Base System
        </p>
    </div>
</body>
</html>
HTML_EOF
    
    # Copy to Google Drive
    cp "${base_path}.md" "${gdrive_path}.md"
    cp "${base_path}.json" "${gdrive_path}.json"
    cp "${base_path}.html" "${gdrive_path}.html"
    
    # Log the collection
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Saved: $title ($assistant) - $link_count links" >> "$SYSTEM_DIR/collection.log"
    
    echo "✓ Saved to $assistant folder: $filename (MD, JSON, HTML)"
    echo "  Local: $LOCAL_BASE/$assistant/links/"
    echo "  GDrive: $GDRIVE_BASE/$assistant/links/"
}

main "$@"
EOF

    chmod +x "$SYSTEM_DIR/scripts/save_links.sh"
}

# Create automatic link detector
create_link_detector() {
    cat > "$SYSTEM_DIR/scripts/link_detector.sh" << 'EOF'
#!/bin/bash

# Automatic link detector for clipboard monitoring

SYSTEM_DIR="$HOME/Desktop/AI_Knowledge_Base_System"
LAST_CLIPBOARD_FILE="$SYSTEM_DIR/temp/last_clipboard.txt"

# Function to get clipboard content
get_clipboard() {
    if command -v pbpaste &> /dev/null; then
        pbpaste
    else
        xclip -selection clipboard -o
    fi
}

# Function to check if content has links
has_links() {
    echo "$1" | grep -Eq 'https?://[^[:space:]]+'
}

# Function to detect if content is a link list
is_link_list() {
    local content="$1"
    local link_count=$(echo "$content" | grep -Eo 'https?://[^[:space:]]+' | wc -l | tr -d ' ')
    
    # Consider it a link list if it has 3+ links or if links make up >30% of lines
    if [ "$link_count" -ge 3 ]; then
        return 0
    fi
    
    local total_lines=$(echo "$content" | wc -l | tr -d ' ')
    if [ "$total_lines" -gt 0 ] && [ "$link_count" -gt 0 ]; then
        local ratio=$((link_count * 100 / total_lines))
        [ "$ratio" -ge 30 ]
    else
        return 1
    fi
}

# Main monitoring loop
monitor_clipboard() {
    echo "Starting clipboard monitor for link lists..."
    
    # Create temp directory if needed
    mkdir -p "$SYSTEM_DIR/temp"
    touch "$LAST_CLIPBOARD_FILE"
    
    while true; do
        current_clipboard=$(get_clipboard)
        last_clipboard=$(cat "$LAST_CLIPBOARD_FILE" 2>/dev/null || echo "")
        
        if [ "$current_clipboard" != "$last_clipboard" ]; then
            if is_link_list "$current_clipboard"; then
                echo "Link list detected! Auto-saving..."
                
                # Generate title from first line or use timestamp
                title=$(echo "$current_clipboard" | head -1 | cut -c1-50)
                if [ -z "$title" ] || has_links "$title"; then
                    title="Auto-collected Links $(date +%Y%m%d_%H%M%S)"
                fi
                
                echo "$current_clipboard" | "$SYSTEM_DIR/scripts/save_links.sh" "$title"
            fi
            
            echo "$current_clipboard" > "$LAST_CLIPBOARD_FILE"
        fi
        
        sleep 5  # Check every 5 seconds
    done
}

monitor_clipboard
EOF

    chmod +x "$SYSTEM_DIR/scripts/link_detector.sh"
}

# Create sync verification script
create_sync_script() {
    cat > "$SYSTEM_DIR/scripts/verify_sync.sh" << 'EOF'
#!/bin/bash

# Google Drive sync verification script

LOCAL_BASE="$HOME/Desktop/AI_Knowledge_Base"
GDRIVE_BASE="$HOME/Google Drive/My Drive/AI_Knowledge_Base"
SYSTEM_DIR="$HOME/Desktop/AI_Knowledge_Base_System"
SYNC_LOG="$SYSTEM_DIR/sync_log.txt"

echo "=== Google Drive Sync Verification ===" | tee -a "$SYNC_LOG"
echo "Date: $(date)" | tee -a "$SYNC_LOG"

# Function to count files
count_files() {
    find "$1" -type f \( -name "*.md" -o -name "*.json" -o -name "*.html" \) 2>/dev/null | wc -l | tr -d ' '
}

# Check each AI assistant folder
AI_ASSISTANTS=("Claude" "ChatGPT" "Gemini" "Copilot" "Perplexity" "Bard")

total_local=0
total_gdrive=0
issues=0

for ai in "${AI_ASSISTANTS[@]}"; do
    local_count=$(count_files "$LOCAL_BASE/$ai")
    gdrive_count=$(count_files "$GDRIVE_BASE/$ai")
    
    total_local=$((total_local + local_count))
    total_gdrive=$((total_gdrive + gdrive_count))
    
    echo "$ai: Local=$local_count, GDrive=$gdrive_count" | tee -a "$SYNC_LOG"
    
    if [ "$local_count" -ne "$gdrive_count" ]; then
        echo "  ⚠️  Sync issue detected!" | tee -a "$SYNC_LOG"
        issues=$((issues + 1))
        
        # Attempt to fix by copying missing files
        rsync -av --ignore-existing "$LOCAL_BASE/$ai/" "$GDRIVE_BASE/$ai/" 2>/dev/null
    else
        echo "  ✓ In sync" | tee -a "$SYNC_LOG"
    fi
done

echo "---" | tee -a "$SYNC_LOG"
echo "Total: Local=$total_local, GDrive=$total_gdrive" | tee -a "$SYNC_LOG"

if [ "$issues" -eq 0 ]; then
    echo "✓ All folders are in sync!" | tee -a "$SYNC_LOG"
else
    echo "⚠️  Found $issues sync issues. Attempted auto-fix." | tee -a "$SYNC_LOG"
fi

echo "" | tee -a "$SYNC_LOG"
EOF

    chmod +x "$SYSTEM_DIR/scripts/verify_sync.sh"
}

# Create migration script for existing data
create_migration_script() {
    cat > "$SYSTEM_DIR/scripts/migrate_existing.sh" << 'EOF'
#!/bin/bash

# Script to migrate existing link collections

echo "=== Migrating Existing Link Collections ==="

# Common locations to check for existing data
SEARCH_PATHS=(
    "$HOME/Documents"
    "$HOME/Desktop"
    "$HOME/Downloads"
    "$HOME/Google Drive"
)

# Find potential link files
echo "Searching for existing link collections..."

for path in "${SEARCH_PATHS[@]}"; do
    if [ -d "$path" ]; then
        echo "Checking $path..."
        find "$path" -type f \( -name "*link*" -o -name "*url*" -o -name "*bookmark*" \) \
            -a \( -name "*.md" -o -name "*.txt" -o -name "*.json" \) \
            -mtime -365 2>/dev/null | while read -r file; do
            
            # Check if file contains links
            if grep -q 'https\?://' "$file"; then
                echo "Found: $file"
                
                # Detect content type
                filename=$(basename "$file")
                echo "$(<"$file")" | "$SYSTEM_DIR/scripts/save_links.sh" "Migrated: $filename"
            fi
        done
    fi
done

echo "Migration complete!"
EOF

    chmod +x "$SYSTEM_DIR/scripts/migrate_existing.sh"
}

# Create LaunchAgent for macOS or systemd service for Linux
create_autostart() {
    echo -e "${YELLOW}Setting up autostart...${NC}"
    
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS LaunchAgent
        PLIST_FILE="$HOME/Library/LaunchAgents/com.ai.knowledgebase.plist"
        
        cat > "$PLIST_FILE" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.ai.knowledgebase</string>
    <key>ProgramArguments</key>
    <array>
        <string>$SYSTEM_DIR/scripts/link_detector.sh</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>$SYSTEM_DIR/detector.log</string>
    <key>StandardErrorPath</key>
    <string>$SYSTEM_DIR/detector_error.log</string>
</dict>
</plist>
EOF
        
        launchctl load "$PLIST_FILE"
        echo -e "${GREEN}✓ LaunchAgent created and loaded${NC}"
        
    else
        # Linux systemd service
        SERVICE_FILE="$HOME/.config/systemd/user/ai-knowledgebase.service"
        
        mkdir -p "$HOME/.config/systemd/user"
        
        cat > "$SERVICE_FILE" << EOF
[Unit]
Description=AI Knowledge Base Link Detector
After=graphical-session.target

[Service]
Type=simple
ExecStart=$SYSTEM_DIR/scripts/link_detector.sh
Restart=always
RestartSec=10

[Install]
WantedBy=default.target
EOF
        
        systemctl --user daemon-reload
        systemctl --user enable ai-knowledgebase.service
        systemctl --user start ai-knowledgebase.service
        echo -e "${GREEN}✓ Systemd service created and started${NC}"
    fi
}

# Create cron job for weekly sync verification
setup_cron() {
    echo -e "${YELLOW}Setting up weekly sync verification...${NC}"
    
    # Add cron job
    (crontab -l 2>/dev/null; echo "0 2 * * 0 $SYSTEM_DIR/scripts/verify_sync.sh") | crontab -
    
    echo -e "${GREEN}✓ Cron job created (Sundays at 2 AM)${NC}"
}

# Save example AR/VFX list
save_example_list() {
    echo -e "${YELLOW}Saving example AR/VFX link list...${NC}"
    
    cat << 'EXAMPLE_EOF' | "$SYSTEM_DIR/scripts/save_links.sh" "AR VFX GitHub Projects"
# AR/VFX GitHub Projects Collection

## AR Development
- https://github.com/google-ar/arcore-android-sdk
- https://github.com/googlevr/arcore-unity-sdk
- https://github.com/AR-js-org/AR.js
- https://github.com/jeromeetienne/AR.js
- https://github.com/hiukim/mind-ar-js

## VFX Tools
- https://github.com/CesiumGS/cesium
- https://github.com/BabylonJS/Babylon.js
- https://github.com/mrdoob/three.js
- https://github.com/playcanvas/engine
- https://github.com/aframevr/aframe

## Computer Vision
- https://github.com/opencv/opencv
- https://github.com/CMU-Perceptual-Computing-Lab/openpose
- https://github.com/facebookresearch/detectron2

## Shaders & Graphics
- https://github.com/patriciogonzalezvivo/thebookofshaders
- https://github.com/greggman/webgl-fundamentals
- https://github.com/spite/codevember-2021

## AR Applications
- https://github.com/viromedia/viro
- https://github.com/MaxstARjs/MaxstARSDK
- https://github.com/artoolkitx/artoolkitx
EXAMPLE_EOF
    
    echo -e "${GREEN}✓ Example saved${NC}"
}

# Create quick command aliases
create_aliases() {
    echo -e "${YELLOW}Creating command aliases...${NC}"
    
    ALIAS_FILE="$SYSTEM_DIR/aliases.sh"
    
    cat > "$ALIAS_FILE" << 'EOF'
#!/bin/bash

# AI Knowledge Base aliases

alias kb-save='$HOME/Desktop/AI_Knowledge_Base_System/scripts/save_links.sh'
alias kb-sync='$HOME/Desktop/AI_Knowledge_Base_System/scripts/verify_sync.sh'
alias kb-migrate='$HOME/Desktop/AI_Knowledge_Base_System/scripts/migrate_existing.sh'
alias kb-log='tail -f $HOME/Desktop/AI_Knowledge_Base_System/collection.log'
alias kb-status='ps aux | grep link_detector'
alias kb-restart='launchctl stop com.ai.knowledgebase && launchctl start com.ai.knowledgebase'

echo "AI Knowledge Base commands:"
echo "  kb-save <title>     - Save links from clipboard"
echo "  kb-sync            - Verify Google Drive sync"
echo "  kb-migrate         - Migrate existing collections"
echo "  kb-log             - View collection log"
echo "  kb-status          - Check detector status"
echo "  kb-restart         - Restart link detector"
EOF
    
    # Add to shell profile
    SHELL_PROFILE="$HOME/.zshrc"
    if [ -f "$HOME/.bashrc" ]; then
        SHELL_PROFILE="$HOME/.bashrc"
    fi
    
    echo "source $ALIAS_FILE" >> "$SHELL_PROFILE"
    
    echo -e "${GREEN}✓ Aliases created${NC}"
}

# Main execution
main() {
    create_directories
    install_dependencies
    create_save_script
    create_link_detector
    create_sync_script
    create_migration_script
    create_autostart
    setup_cron
    save_example_list
    create_aliases
    
    # Initialize log
    log_message "AI Knowledge Base System initialized"
    
    echo -e "\n${GREEN}=== Setup Complete! ===${NC}"
    echo -e "\nYour AI Knowledge Base System is now active with:"
    echo -e "  ${BLUE}•${NC} Automatic clipboard monitoring for link lists"
    echo -e "  ${BLUE}•${NC} Multi-format saving (MD, JSON, HTML)"
    echo -e "  ${BLUE}•${NC} Local and Google Drive storage"
    echo -e "  ${BLUE}•${NC} Weekly sync verification"
    echo -e "  ${BLUE}•${NC} Migration tools for existing data"
    
    echo -e "\n${YELLOW}Quick Start:${NC}"
    echo -e "1. Run migration: ${GREEN}kb-migrate${NC}"
    echo -e "2. Save links manually: ${GREEN}kb-save \"My Links\"${NC}"
    echo -e "3. Check sync status: ${GREEN}kb-sync${NC}"
    echo -e "4. View activity log: ${GREEN}kb-log${NC}"
    
    echo -e "\n${YELLOW}Next Steps:${NC}"
    echo -e "1. Source your shell profile: ${GREEN}source $SHELL_PROFILE${NC}"
    echo -e "2. The system is monitoring your clipboard automatically"
    echo -e "3. Copy any link list and it will be auto-saved"
    
    # Run initial sync check
    "$SYSTEM_DIR/scripts/verify_sync.sh"
}

# Run main function
main

echo -e "\n${GREEN}✨ AI Knowledge Base System is running!${NC}"
