#!/bin/bash

# Quick Start Script for AI Knowledge Base System
# Run this to immediately set up and activate the system

echo "🚀 Starting AI Knowledge Base System Setup..."

# Make setup script executable
chmod +x ~/Desktop/AI_Knowledge_Base_System/setup_knowledge_base.sh

# Run the setup
~/Desktop/AI_Knowledge_Base_System/setup_knowledge_base.sh

# After setup completes, run migration for existing files
echo -e "\n📁 Checking for existing link collections to migrate..."
sleep 2

# Source the aliases
source ~/Desktop/AI_Knowledge_Base_System/aliases.sh

# Run migration
if [ -f ~/Desktop/AI_Knowledge_Base_System/scripts/migrate_existing.sh ]; then
    ~/Desktop/AI_Knowledge_Base_System/scripts/migrate_existing.sh
fi

# Show system status
echo -e "\n📊 System Status:"
ps aux | grep link_detector | grep -v grep && echo "✅ Link detector is running" || echo "❌ Link detector not running"

# Show recent activity
echo -e "\n📝 Recent Activity:"
tail -5 ~/Desktop/AI_Knowledge_Base_System/collection.log 2>/dev/null || echo "No activity yet"

echo -e "\n✨ AI Knowledge Base System is now active!"
echo -e "\nThe system is now:"
echo "  • Monitoring your clipboard for link lists"
echo "  • Auto-saving to local and Google Drive"
echo "  • Converting to MD, JSON, and HTML formats"
echo -e "\nTry copying a list of links to test it!"
