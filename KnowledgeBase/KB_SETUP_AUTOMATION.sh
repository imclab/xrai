#!/bin/bash
# KB_SETUP_AUTOMATION.sh - One-time setup for automated maintenance
# Version: 1.0
# Last Updated: 2025-01-07

set -e

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}🚀 Setting Up Automated Knowledgebase Maintenance${NC}"
echo "======================================================"

# 1. Create alias shortcuts
echo ""
echo "1. Creating Shell Aliases"
echo "-------------------------"

ALIAS_FILE=~/.zshrc  # or ~/.bashrc for bash

cat >> "$ALIAS_FILE" << 'EOF'

# Knowledgebase Maintenance Aliases (Auto-generated)
alias kb-audit='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_AUDIT.sh'
alias kb-backup='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_BACKUP.sh'
alias kb-research='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_RESEARCH.sh'
alias kb-optimize='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_OPTIMIZE.sh'
alias kb-maintain='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_MAINTENANCE.sh'
alias kb-logs='tail -f ~/.claude/knowledgebase/maintenance.log'

EOF

echo -e "${GREEN}✓${NC} Aliases added to $ALIAS_FILE"
echo "   Run: source $ALIAS_FILE"

# 2. Create log directory
echo ""
echo "2. Creating Log Directory"
echo "-------------------------"
mkdir -p ~/.claude/knowledgebase
touch ~/.claude/knowledgebase/maintenance.log
echo -e "${GREEN}✓${NC} Log directory created"

# 3. Setup cron jobs (macOS)
echo ""
echo "3. Cron Job Setup"
echo "-----------------"
echo ""
echo "To enable automated daily maintenance:"
echo ""
echo "  1. Edit crontab:"
echo "     crontab -e"
echo ""
echo "  2. Add these lines:"
echo ""
cat << 'CRONEOF'
# Knowledgebase Daily Maintenance (5 AM)
0 5 * * * /Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_MAINTENANCE.sh daily >> /Users/jamestunick/.claude/knowledgebase/maintenance.log 2>&1

# Knowledgebase Weekly Maintenance (Sunday 5 AM)
0 5 * * 0 /Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_MAINTENANCE.sh weekly >> /Users/jamestunick/.claude/knowledgebase/maintenance.log 2>&1

CRONEOF
echo ""
echo -e "${YELLOW}⚠${NC} Manual step required - edit crontab"

# 4. Test run
echo ""
echo "4. Test Run"
echo "-----------"
echo "Running audit to verify setup..."
echo ""

~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_AUDIT.sh || true

# 5. Summary
echo ""
echo "======================================================"
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo "======================================================"
echo ""
echo "Available Commands:"
echo "  kb-audit      - Run health check"
echo "  kb-backup     - Create backup"
echo "  kb-research   - Run research automation"
echo "  kb-optimize   - Optimize knowledgebase"
echo "  kb-maintain   - Run full maintenance"
echo "  kb-logs       - View maintenance logs"
echo ""
echo "Next Steps:"
echo "  1. Reload shell: source $ALIAS_FILE"
echo "  2. Setup cron: crontab -e (optional)"
echo "  3. Test: kb-audit"
echo ""
echo "Documentation:"
echo "  ~/.claude/knowledgebase/_AUTOMATED_MAINTENANCE_GUIDE.md"
echo ""
