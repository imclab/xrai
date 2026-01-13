#!/bin/bash
# KB_AUDIT.sh - Knowledgebase Health & Integrity Audit
# Version: 2.0 - Added self-healing + history tracking
# Last Updated: 2026-01-12

set -eo pipefail
# Note: Using || true after ((var++)) to prevent exit on increment from 0

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

KB_PATH=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase

PASS=0
FAIL=0
WARN=0

check() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓${NC} $1"
        ((PASS++)) || true
    else
        echo -e "${RED}✗${NC} $1"
        ((FAIL++)) || true
    fi
}

check_warn() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓${NC} $1"
        ((PASS++)) || true
    else
        echo -e "${YELLOW}⚠${NC} $1"
        ((WARN++)) || true
    fi
}

echo "🔍 Knowledgebase Audit - $(date '+%Y-%m-%d %H:%M:%S')"
echo "=================================================="

# 1. File Structure
echo ""
echo "1. File Structure"
echo "-----------------"
test -d "$KB_PATH"
check "Knowledgebase directory exists"

test -f "$KB_PATH/_MASTER_KNOWLEDGEBASE_INDEX.md"
check "Master Index exists"

test -f "$KB_PATH/_MASTER_AI_TOOLS_REGISTRY.md"
check "AI Tools Registry exists"

test -f "$KB_PATH/_SELF_IMPROVING_MEMORY_ARCHITECTURE.md"
check "Memory Architecture exists"

test -f "$KB_PATH/LEARNING_LOG.md"
check "Learning Log exists"

# 2. Symlinks
echo ""
echo "2. Symlink Integrity"
echo "--------------------"
test -L ~/.claude/knowledgebase
check "Claude Code symlink exists"

test -L ~/.windsurf/knowledgebase
check_warn "Windsurf symlink exists"

test -L ~/.cursor/knowledgebase
check_warn "Cursor symlink exists"

# Verify symlink targets
TARGET=$(readlink ~/.claude/knowledgebase 2>/dev/null || echo "")
if [ "$TARGET" = "$KB_PATH" ]; then
    echo -e "${GREEN}✓${NC} Symlinks point to correct location"
    ((PASS++)) || true
else
    echo -e "${YELLOW}⚠${NC} Symlink target mismatch: $TARGET"
    ((WARN++)) || true
fi

# 3. Git Status
echo ""
echo "3. Git Repository"
echo "-----------------"
cd "$KB_PATH"

git status >/dev/null 2>&1
check "Repository is valid"

# Check for uncommitted changes
if git diff-index --quiet HEAD -- 2>/dev/null; then
    echo -e "${GREEN}✓${NC} No uncommitted changes"
    ((PASS++)) || true
else
    echo -e "${YELLOW}⚠${NC} Uncommitted changes detected"
    ((WARN++)) || true
fi

# 4. File Permissions
echo ""
echo "4. File Accessibility"
echo "---------------------"
test -r "$KB_PATH/_MASTER_KNOWLEDGEBASE_INDEX.md"
check "Can read Master Index"

test -w "$KB_PATH/LEARNING_LOG.md"
check "Can write to Learning Log"

# 5. Duplicate Detection
echo ""
echo "5. Duplicate Content Check"
echo "--------------------------"
DUPES=$(find "$KB_PATH" -name "*.md" -type f -exec md5 {} \; 2>/dev/null | sort | uniq -d | wc -l | tr -d ' ')
if [ "$DUPES" -eq 0 ]; then
    echo -e "${GREEN}✓${NC} No duplicate files detected"
    ((PASS++)) || true
else
    echo -e "${YELLOW}⚠${NC} $DUPES duplicate files found"
    ((WARN++)) || true
fi

# 6. Token Usage Measurement
echo ""
echo "6. Token Usage Analysis"
echo "-----------------------"
TOTAL_WORDS=$(find "$KB_PATH" -name "*.md" -type f -exec cat {} \; 2>/dev/null | wc -w | tr -d ' ')
ESTIMATED_TOKENS=$((TOTAL_WORDS * 4 / 3))  # Rough estimate
echo "   Total words: $TOTAL_WORDS"
echo "   Estimated tokens: ~${ESTIMATED_TOKENS}"

if [ $ESTIMATED_TOKENS -lt 60000 ]; then
    echo -e "${GREEN}✓${NC} Token usage within target (<60K)"
    ((PASS++)) || true
else
    echo -e "${YELLOW}⚠${NC} Token usage high (consider optimization)"
    ((WARN++)) || true
fi

# 7. File Size Check
echo ""
echo "7. File Size Check"
echo "------------------"
KB_SIZE=$(du -sh "$KB_PATH" | awk '{print $1}')
echo "   Knowledgebase size: $KB_SIZE"

LARGE_FILES=$(find "$KB_PATH" -name "*.md" -type f -size +100k 2>/dev/null | wc -l | tr -d ' ')
if [ "$LARGE_FILES" -eq 0 ]; then
    echo -e "${GREEN}✓${NC} No files exceed 100KB"
    ((PASS++)) || true
else
    echo -e "${YELLOW}⚠${NC} $LARGE_FILES files exceed 100KB"
    echo "   Consider splitting large files"
    ((WARN++)) || true
fi

# 8. Markdown Syntax Check
echo ""
echo "8. Markdown Syntax (Quick Check)"
echo "--------------------------------"
SYNTAX_ERRORS=0
for file in "$KB_PATH"/*.md; do
    # Basic checks: unclosed code blocks
    OPEN_BLOCKS=$(grep -c '^```' "$file" 2>/dev/null | tr -d '[:space:]' || echo "0")
    OPEN_BLOCKS=${OPEN_BLOCKS:-0}
    if [[ "$OPEN_BLOCKS" =~ ^[0-9]+$ ]] && [ $((OPEN_BLOCKS % 2)) -ne 0 ]; then
        echo -e "${RED}✗${NC} Unclosed code block in $(basename "$file")"
        ((SYNTAX_ERRORS++)) || true
        ((FAIL++)) || true
    fi
done

if [ $SYNTAX_ERRORS -eq 0 ]; then
    echo -e "${GREEN}✓${NC} No obvious syntax errors"
    ((PASS++)) || true
fi

# 9. External Links (Sample Check)
echo ""
echo "9. External Links Check (Sample)"
echo "---------------------------------"
# Check a few critical links
if curl -s -o /dev/null -w "%{http_code}" "https://docs.unity3d.com" | grep -q "200"; then
    echo -e "${GREEN}✓${NC} Unity docs accessible"
    ((PASS++)) || true
else
    echo -e "${YELLOW}⚠${NC} Unity docs may be down"
    ((WARN++)) || true
fi

if curl -s -o /dev/null -w "%{http_code}" "https://threejs.org" | grep -q "200"; then
    echo -e "${GREEN}✓${NC} Three.js site accessible"
    ((PASS++)) || true
else
    echo -e "${YELLOW}⚠${NC} Three.js site may be down"
    ((WARN++)) || true
fi

# 10. Self-Healing (Auto-Repair)
echo ""
echo "10. Self-Healing"
echo "----------------"
HEALED=0

# Auto-repair broken symlinks
for tool in claude windsurf cursor; do
    LINK="$HOME/.$tool/knowledgebase"
    if [ -L "$LINK" ] && [ ! -e "$LINK" ]; then
        # Broken symlink - repair
        rm -f "$LINK"
        ln -sf "$KB_PATH" "$LINK"
        echo -e "${GREEN}✓${NC} Repaired broken symlink: $tool"
        ((HEALED++)) || true
    elif [ ! -L "$LINK" ] && [ -d "$HOME/.$tool" ]; then
        # Missing symlink - create
        ln -sf "$KB_PATH" "$LINK"
        echo -e "${GREEN}✓${NC} Created missing symlink: $tool"
        ((HEALED++)) || true
    fi
done

if [ $HEALED -eq 0 ]; then
    echo -e "${GREEN}✓${NC} No repairs needed"
else
    echo "   Healed $HEALED issues"
fi

# 11. Summary
echo ""
echo "=================================================="
echo "📊 Audit Summary"
echo "=================================================="
echo -e "${GREEN}Passed:${NC} $PASS"
echo -e "${YELLOW}Warnings:${NC} $WARN"
echo -e "${RED}Failed:${NC} $FAIL"
echo -e "${BLUE}Healed:${NC} $HEALED"
echo ""

# Generate metrics JSON
METRICS_FILE=~/.claude/knowledgebase/metrics.json
HISTORY_FILE=~/.claude/knowledgebase/audit-history.jsonl
cat > "$METRICS_FILE" << EOF
{
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "passed": $PASS,
  "warnings": $WARN,
  "failed": $FAIL,
  "healed": $HEALED,
  "estimated_tokens": $ESTIMATED_TOKENS,
  "size": "$KB_SIZE",
  "file_count": $(find "$KB_PATH" -name "*.md" -type f | wc -l | tr -d ' ')
}
EOF

# Append to history log (tracks what works/doesn't over time)
echo "{\"ts\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"pass\":$PASS,\"warn\":$WARN,\"fail\":$FAIL,\"heal\":$HEALED}" >> "$HISTORY_FILE"

if [ $FAIL -eq 0 ]; then
    echo -e "${GREEN}✅ Knowledgebase health: GOOD${NC}"
    echo ""
    echo "Metrics saved to: $METRICS_FILE"
    exit 0
else
    echo -e "${RED}❌ Knowledgebase health: NEEDS ATTENTION${NC}"
    echo ""
    echo "Please review failures above."
    echo "Metrics saved to: $METRICS_FILE"
    exit 1
fi
