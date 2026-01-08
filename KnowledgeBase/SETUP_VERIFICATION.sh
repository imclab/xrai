#!/bin/bash
# Knowledge Base Setup Verification Script
# Version: 1.0
# Last Updated: 2025-01-07
# Purpose: Verify all AI tools have proper access to unified knowledgebase

set -e

echo "🔍 Verifying Unified Knowledgebase Setup..."
echo "=============================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Counters
PASS=0
FAIL=0
WARN=0

# Check function
check() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓${NC} $1"
        ((PASS++))
    else
        echo -e "${RED}✗${NC} $1"
        ((FAIL++))
    fi
}

check_warn() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓${NC} $1"
        ((PASS++))
    else
        echo -e "${YELLOW}⚠${NC} $1 (optional)"
        ((WARN++))
    fi
}

echo "1. Checking Knowledgebase Location"
echo "-----------------------------------"
KB_PATH=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase
test -d "$KB_PATH"
check "Main knowledgebase exists at $KB_PATH"

echo ""
echo "2. Checking Essential Files"
echo "---------------------------"
test -f "$KB_PATH/_MASTER_KNOWLEDGEBASE_INDEX.md"
check "Master Index exists"

test -f "$KB_PATH/_MASTER_AI_TOOLS_REGISTRY.md"
check "AI Tools Registry exists"

test -f "$KB_PATH/_SELF_IMPROVING_MEMORY_ARCHITECTURE.md"
check "Memory Architecture guide exists"

test -f "$KB_PATH/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md"
check "GitHub Repos knowledgebase exists (530+ repos)"

test -f "$KB_PATH/_WEBGL_THREEJS_COMPREHENSIVE_GUIDE.md"
check "WebGL/Three.js guide exists"

test -f "$KB_PATH/_PERFORMANCE_PATTERNS_REFERENCE.md"
check "Performance Patterns reference exists"

test -f "$KB_PATH/LEARNING_LOG.md"
check "Learning Log exists"

echo ""
echo "3. Checking Global Configuration"
echo "---------------------------------"
test -f ~/CLAUDE.md
check "Global CLAUDE.md exists"

test -f ~/.claude/CLAUDE.md
check "Claude Code CLAUDE.md exists"

echo ""
echo "4. Checking Symlinks - Claude Code"
echo "-----------------------------------"
test -L ~/.claude/knowledgebase
check "Knowledgebase symlink exists"

TARGET=$(readlink ~/.claude/knowledgebase 2>/dev/null)
if [ "$TARGET" = "$KB_PATH" ]; then
    echo -e "${GREEN}✓${NC} Symlink points to correct location: $TARGET"
    ((PASS++))
else
    echo -e "${RED}✗${NC} Symlink points to wrong location: $TARGET"
    ((FAIL++))
fi

echo ""
echo "5. Checking Symlinks - Windsurf"
echo "--------------------------------"
test -d ~/.windsurf
check_warn "Windsurf directory exists"

if [ -d ~/.windsurf ]; then
    test -L ~/.windsurf/knowledgebase
    check "Windsurf knowledgebase symlink exists"

    test -L ~/.windsurf/CLAUDE.md
    check "Windsurf CLAUDE.md symlink exists"
fi

echo ""
echo "6. Checking Symlinks - Cursor"
echo "------------------------------"
test -d ~/.cursor
check_warn "Cursor directory exists"

if [ -d ~/.cursor ]; then
    test -L ~/.cursor/knowledgebase
    check "Cursor knowledgebase symlink exists"

    test -L ~/.cursor/CLAUDE.md
    check "Cursor CLAUDE.md symlink exists"
fi

echo ""
echo "7. Checking File Accessibility"
echo "-------------------------------"
test -r "$KB_PATH/_MASTER_KNOWLEDGEBASE_INDEX.md"
check "Can read Master Index"

test -r "$KB_PATH/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md"
check "Can read GitHub KB"

test -r "$KB_PATH/LEARNING_LOG.md"
check "Can read Learning Log"

test -w "$KB_PATH/LEARNING_LOG.md"
check "Can write to Learning Log"

echo ""
echo "8. Checking MCP Configurations (Optional)"
echo "------------------------------------------"
test -f ~/.claude/settings.json
check_warn "Claude Code settings.json exists"

test -f ~/.windsurf/mcp.json 2>/dev/null
check_warn "Windsurf mcp.json exists"

test -f ~/.cursor/mcp.json 2>/dev/null
check_warn "Cursor mcp.json exists"

echo ""
echo "9. Checking Git Status"
echo "----------------------"
cd "$KB_PATH"
git status >/dev/null 2>&1
check "Knowledgebase is in Git repository"

git log -1 --oneline >/dev/null 2>&1
check "Git has commit history"

echo ""
echo "10. File Count Statistics"
echo "-------------------------"
MD_COUNT=$(find "$KB_PATH" -name "*.md" -type f | wc -l | tr -d ' ')
echo "   Markdown files: $MD_COUNT"

SIZE=$(du -sh "$KB_PATH" | awk '{print $1}')
echo "   Total size: $SIZE"

echo ""
echo "=============================================="
echo "📊 Verification Results"
echo "=============================================="
echo -e "${GREEN}Passed:${NC} $PASS"
echo -e "${YELLOW}Warnings:${NC} $WARN (optional features)"
echo -e "${RED}Failed:${NC} $FAIL"
echo ""

if [ $FAIL -eq 0 ]; then
    echo -e "${GREEN}✅ All critical checks passed!${NC}"
    echo ""
    echo "Your unified knowledgebase is properly configured."
    echo "All AI tools have shared access via symlinks."
    echo ""
    echo "Quick Access Commands:"
    echo "  View Master Index: cat ~/.claude/knowledgebase/_MASTER_KNOWLEDGEBASE_INDEX.md"
    echo "  View Learning Log: cat ~/.claude/knowledgebase/LEARNING_LOG.md"
    echo "  List all files: ls -la ~/.claude/knowledgebase/"
    echo ""
    echo "Next Steps:"
    echo "  1. Start using AI tools - they now share knowledge automatically"
    echo "  2. Add discoveries to LEARNING_LOG.md"
    echo "  3. Watch intelligence grow with each interaction!"
    exit 0
else
    echo -e "${RED}❌ Some checks failed. Please review the errors above.${NC}"
    echo ""
    echo "To fix symlinks, run:"
    echo "  KB=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase"
    echo "  ln -sf \$KB ~/.claude/knowledgebase"
    echo "  ln -sf \$KB ~/.windsurf/knowledgebase"
    echo "  ln -sf \$KB ~/.cursor/knowledgebase"
    echo "  ln -sf ~/CLAUDE.md ~/.windsurf/CLAUDE.md"
    echo "  ln -sf ~/CLAUDE.md ~/.cursor/CLAUDE.md"
    exit 1
fi
