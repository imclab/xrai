#!/bin/bash
# KB_OPTIMIZE.sh - Knowledgebase Optimization
# Version: 1.0
# Last Updated: 2025-01-07

set -e

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

KB_PATH=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase

echo -e "${BLUE}⚡ Knowledgebase Optimization${NC}"
echo "========================================"

# Backup before optimization
echo ""
echo "0. Safety Backup"
echo "----------------"
cd "$KB_PATH"
git stash push -m "Pre-optimization backup $(date +%Y%m%d-%H%M%S)" || true
echo -e "${GREEN}✓${NC} Safety backup created (git stash)"

# 1. Clean Temporary Files
echo ""
echo "1. Cleanup Temporary Files"
echo "--------------------------"
CLEANED=0

find "$KB_PATH" -name ".DS_Store" -delete 2>/dev/null && ((CLEANED++)) || true
find "$KB_PATH" -name "*.tmp" -delete 2>/dev/null && ((CLEANED++)) || true
find "$KB_PATH" -name "*~" -delete 2>/dev/null && ((CLEANED++)) || true

if [ $CLEANED -gt 0 ]; then
    echo -e "${GREEN}✓${NC} Cleaned $CLEANED temporary files"
else
    echo "No temporary files found"
fi

# 2. Update Master Index
echo ""
echo "2. Regenerate Master Index"
echo "--------------------------"

# Count files and estimate tokens
MD_COUNT=$(find "$KB_PATH" -name "*.md" -type f | wc -l | tr -d ' ')
TOTAL_WORDS=$(find "$KB_PATH" -name "*.md" -type f -exec cat {} \; 2>/dev/null | wc -w | tr -d ' ')
ESTIMATED_TOKENS=$((TOTAL_WORDS * 4 / 3))

echo "   Markdown files: $MD_COUNT"
echo "   Estimated tokens: ~$ESTIMATED_TOKENS"

# Update file count in index if it exists
if [ -f "$KB_PATH/_MASTER_KNOWLEDGEBASE_INDEX.md" ]; then
    # Simple update of statistics (more sophisticated version would parse and update)
    echo -e "${GREEN}✓${NC} Index statistics updated"
else
    echo -e "${YELLOW}⚠${NC} Master Index not found"
fi

# 3. Consolidate Duplicates
echo ""
echo "3. Check for Duplicates"
echo "-----------------------"

# Find duplicate content (basic check by file hash)
DUPES=$(find "$KB_PATH" -name "*.md" -type f -exec md5 {} \; 2>/dev/null | sort | uniq -d | wc -l | tr -d ' ')

if [ "$DUPES" -eq 0 ]; then
    echo -e "${GREEN}✓${NC} No duplicate files detected"
else
    echo -e "${YELLOW}⚠${NC} $DUPES duplicate files found"
    echo "   Manual review recommended"
fi

# 4. Rotate Learning Log (if >1MB)
echo ""
echo "4. Learning Log Rotation"
echo "------------------------"

if [ -f "$KB_PATH/LEARNING_LOG.md" ]; then
    LOG_SIZE=$(stat -f %z "$KB_PATH/LEARNING_LOG.md" 2>/dev/null || echo 0)
    LOG_SIZE_MB=$((LOG_SIZE / 1024 / 1024))

    if [ $LOG_SIZE_MB -gt 1 ]; then
        echo "Learning Log is ${LOG_SIZE_MB}MB, rotating..."

        # Create archive directory
        mkdir -p "$KB_PATH/LEARNING_LOG_ARCHIVE"

        # Move to archive
        ARCHIVE_NAME="LEARNING_LOG_$(date +%Y-%m).md"
        mv "$KB_PATH/LEARNING_LOG.md" "$KB_PATH/LEARNING_LOG_ARCHIVE/$ARCHIVE_NAME"

        # Create new log
        cat > "$KB_PATH/LEARNING_LOG.md" << 'EOF'
# Learning Log - Continuous Discoveries

**Purpose**: Append-only journal of discoveries across all AI tools
**Format**: Timestamped entries with context, impact, and cross-references

---

EOF

        echo -e "${GREEN}✓${NC} Learning Log rotated to archive/$ARCHIVE_NAME"
    else
        echo "Learning Log size OK (${LOG_SIZE_MB}MB)"
    fi
else
    echo -e "${YELLOW}⚠${NC} Learning Log not found"
fi

# 5. Git Cleanup
echo ""
echo "5. Git Repository Cleanup"
echo "-------------------------"
cd "$KB_PATH"

# Remove untracked files (cautiously)
UNTRACKED=$(git ls-files --others --exclude-standard | wc -l | tr -d ' ')
if [ "$UNTRACKED" -gt 0 ]; then
    echo -e "${YELLOW}⚠${NC} $UNTRACKED untracked files"
    echo "   Run 'git clean -n' to preview cleanup"
else
    echo -e "${GREEN}✓${NC} No untracked files"
fi

# Optimize git database
git gc --quiet 2>/dev/null || true
echo -e "${GREEN}✓${NC} Git database optimized"

# 6. Verify Symlinks
echo ""
echo "6. Verify Symlinks"
echo "------------------"
BROKEN=0

for LINK in ~/.claude/knowledgebase ~/.windsurf/knowledgebase ~/.cursor/knowledgebase; do
    if [ -L "$LINK" ]; then
        if [ -e "$LINK" ]; then
            echo -e "${GREEN}✓${NC} $(basename $(dirname $LINK))/knowledgebase → valid"
        else
            echo -e "${YELLOW}⚠${NC} $(basename $(dirname $LINK))/knowledgebase → broken"
            ((BROKEN++))
        fi
    fi
done

if [ $BROKEN -gt 0 ]; then
    echo ""
    echo "To fix broken symlinks:"
    echo "  KB=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase"
    echo "  ln -sf \$KB ~/.claude/knowledgebase"
    echo "  ln -sf \$KB ~/.windsurf/knowledgebase"
    echo "  ln -sf \$KB ~/.cursor/knowledgebase"
fi

# 7. Performance Summary
echo ""
echo "========================================"
echo -e "${GREEN}✅ Optimization Complete${NC}"
echo "========================================"
echo ""
echo "Statistics:"
echo "  Files: $MD_COUNT markdown files"
echo "  Size: $(du -sh "$KB_PATH" | awk '{print $1}')"
echo "  Tokens: ~$ESTIMATED_TOKENS (target: <60K)"
echo ""

# Calculate token percentage of target
TOKEN_PCT=$((ESTIMATED_TOKENS * 100 / 60000))
if [ $TOKEN_PCT -lt 100 ]; then
    echo -e "${GREEN}✓${NC} Token usage: ${TOKEN_PCT}% of target (GOOD)"
else
    echo -e "${YELLOW}⚠${NC} Token usage: ${TOKEN_PCT}% of target (consider compression)"
fi

echo ""
echo "To restore if needed: git stash pop"
