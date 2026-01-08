#!/bin/bash
# KB_BACKUP.sh - Automated Backup System with Versioning
# Version: 1.0
# Last Updated: 2025-01-07

set -e

GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

KB_PATH=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase
BACKUP_DIR=~/Documents/GitHub/code-backups
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_NAME="KB-$TIMESTAMP"

echo -e "${BLUE}🔒 Knowledgebase Backup${NC}"
echo "========================================"

# Create backup directory if needed
mkdir -p "$BACKUP_DIR"

# 1. Git Commit (Primary Backup)
echo ""
echo "1. Git Backup"
echo "-------------"
cd "$KB_PATH"

if git diff-index --quiet HEAD -- 2>/dev/null; then
    echo "No changes to commit"
else
    git add .
    git commit -m "Auto-backup: $TIMESTAMP" || echo "Commit failed (may be empty)"
    echo -e "${GREEN}✓${NC} Git commit created"
fi

git log -1 --oneline
echo -e "${GREEN}✓${NC} Git history preserved"

# 2. Local Backup (Secondary)
echo ""
echo "2. Local Backup"
echo "---------------"
BACKUP_PATH="$BACKUP_DIR/$BACKUP_NAME"
mkdir -p "$BACKUP_PATH"

# Copy knowledgebase
cp -R "$KB_PATH"/* "$BACKUP_PATH/" 2>/dev/null || true

# Verify backup
if [ -f "$BACKUP_PATH/_MASTER_KNOWLEDGEBASE_INDEX.md" ]; then
    echo -e "${GREEN}✓${NC} Backup created at: $BACKUP_PATH"
    BACKUP_SIZE=$(du -sh "$BACKUP_PATH" | awk '{print $1}')
    echo "   Size: $BACKUP_SIZE"
else
    echo "❌ Backup verification failed"
    exit 1
fi

# 3. Cleanup Old Backups (Retention: 30 days)
echo ""
echo "3. Cleanup Old Backups"
echo "----------------------"
RETENTION_DAYS=30
DELETED=0

find "$BACKUP_DIR" -name "KB-*" -type d -mtime +$RETENTION_DAYS 2>/dev/null | while read OLD_BACKUP; do
    rm -rf "$OLD_BACKUP"
    ((DELETED++))
    echo "Deleted: $(basename "$OLD_BACKUP")"
done || true

if [ $DELETED -eq 0 ]; then
    echo "No old backups to delete"
else
    echo -e "${GREEN}✓${NC} Deleted $DELETED old backups"
fi

# 4. Optional: Cloud Sync (Uncomment to enable)
# echo ""
# echo "4. Cloud Sync (Optional)"
# echo "------------------------"
# if [ -d ~/Dropbox ]; then
#     rsync -av --delete "$KB_PATH/" ~/Dropbox/KB-Backups/latest/ || true
#     echo -e "${GREEN}✓${NC} Synced to Dropbox"
# fi

# 5. Summary
echo ""
echo "========================================"
echo -e "${GREEN}✅ Backup Complete${NC}"
echo "========================================"
echo ""
echo "Backup Locations:"
echo "  1. Git: $(git log -1 --format=%H)"
echo "  2. Local: $BACKUP_PATH"
echo ""
echo "Recent Backups:"
ls -lth "$BACKUP_DIR" | grep "^d" | grep "KB-" | head -5 | awk '{print "  "$9, "("$6, $7, $8")"}'
echo ""

# Restore Instructions
cat << 'EOF'
To restore from backup:
  # From Git (recommended):
  cd ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase
  git log --oneline  # Find commit
  git reset --hard <commit-hash>

  # From local backup:
  cp -R ~/Documents/GitHub/code-backups/KB-YYYYMMDD-HHMMSS/* \
        ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
EOF
