# AI Knowledge Base Auto-Collection System

## 🚀 Quick Start

Run this single command to set up everything:

```bash
chmod +x ~/Desktop/AI_Knowledge_Base_System/quick_start.sh && ~/Desktop/AI_Knowledge_Base_System/quick_start.sh
```

## 📋 What This System Does

1. **Automatic Link Detection**: Monitors your clipboard for link lists and auto-saves them
2. **Multi-Format Storage**: Saves in Markdown, JSON, and HTML formats
3. **Dual Storage**: Keeps copies in both local folders and Google Drive
4. **AI Organization**: Automatically organizes by AI assistant (Claude, ChatGPT, etc.)
5. **Weekly Sync Verification**: Ensures Google Drive stays in sync
6. **Migration Tools**: Finds and imports existing link collections

## 📁 Directory Structure

```
~/Desktop/AI_Knowledge_Base/
├── Claude/
│   ├── links/       # Link collections
│   ├── documents/   # Related documents
│   └── exports/     # Exported data
├── ChatGPT/
├── Gemini/
├── Copilot/
└── ...

~/Google Drive/My Drive/AI_Knowledge_Base/
└── [Same structure as above]
```

## 🛠️ Commands

After setup, use these commands:

- `kb-save "Title"` - Manually save links from clipboard
- `kb-sync` - Verify Google Drive sync
- `kb-migrate` - Find and import existing collections
- `kb-log` - View activity log
- `kb-status` - Check system status
- `kb-restart` - Restart link detector

## 🔧 How It Works

1. **Clipboard Monitoring**: Checks clipboard every 5 seconds
2. **Link Detection**: Identifies content with 3+ links
3. **Auto-Categorization**: Detects which AI assistant based on content
4. **Format Conversion**: Creates MD, JSON, and HTML versions
5. **Sync to Cloud**: Copies to Google Drive automatically

## 📝 Global Rules

The system follows these rules (defined in `global_rules.yaml`):

- Minimum 3 links to trigger auto-save
- Triggers on keywords like "links", "URLs", "resources", "bookmarks"
- Saves in three formats always
- Weekly sync verification
- 365-day backup retention

## 🔍 Example Use Cases

1. **Copy a GitHub project list** → Auto-saved with "GitHub Projects" title
2. **Copy bookmarks from browser** → Detected and organized
3. **Paste research links** → Categorized by AI assistant
4. **Share resource lists** → Available in multiple formats

## ⚙️ Manual Setup (if needed)

If the quick start doesn't work, run these commands:

```bash
# 1. Make scripts executable
chmod +x ~/Desktop/AI_Knowledge_Base_System/*.sh

# 2. Run setup
~/Desktop/AI_Knowledge_Base_System/setup_knowledge_base.sh

# 3. Source aliases (for zsh)
echo "source ~/Desktop/AI_Knowledge_Base_System/aliases.sh" >> ~/.zshrc
source ~/.zshrc

# 4. Start monitoring
launchctl load ~/Library/LaunchAgents/com.ai.knowledgebase.plist
```

## 🐛 Troubleshooting

### Link detector not running?
```bash
# Check status
ps aux | grep link_detector

# Restart
launchctl stop com.ai.knowledgebase
launchctl start com.ai.knowledgebase
```

### Sync issues?
```bash
# Manual sync check
kb-sync

# Force sync
rsync -av ~/Desktop/AI_Knowledge_Base/ ~/Google\ Drive/My\ Drive/AI_Knowledge_Base/
```

### Permission issues?
```bash
# Fix permissions
chmod -R 755 ~/Desktop/AI_Knowledge_Base_System
```

## 📊 Monitoring

View system activity:
- Collection log: `~/Desktop/AI_Knowledge_Base_System/collection.log`
- Sync log: `~/Desktop/AI_Knowledge_Base_System/sync_log.txt`
- Error log: `~/Desktop/AI_Knowledge_Base_System/detector_error.log`

## 🔄 Updates

The system auto-updates through:
- Weekly sync verification
- Automatic error recovery
- Migration of new file formats

## 🎯 Tips

1. **Batch Processing**: Copy multiple link lists at once
2. **Quick Save**: Just copy links - no need to format
3. **Search**: Use Spotlight/grep to search saved collections
4. **Share**: HTML format perfect for sharing

## 📧 Integration Ideas

- Email link collections using HTML format
- Import to note-taking apps via Markdown
- Process with scripts using JSON format
- Create web pages from HTML exports

---

**System Version**: 1.0  
**Last Updated**: $(date)  
**Location**: ~/Desktop/AI_Knowledge_Base_System
