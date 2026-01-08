#!/bin/bash
# KB Research and Update Automation
# Periodically research latest best practices and update all configs
#
# Usage:
#   ./KB_RESEARCH_AND_UPDATE.sh [--quick|--full]
#
# Modes:
#   --quick: Fast check (Unity MCP, critical updates only) ~5 min
#   --full:  Deep research (all tools, best practices) ~30 min

set -e

KB_ROOT="$HOME/Documents/GitHub/Unity-XR-AI/KnowledgeBase"
LOG_FILE="$KB_ROOT/.claude/research_log_$(date +%Y%m%d-%H%M%S).txt"
MODE="${1:-quick}"

echo "════════════════════════════════════════════════════════════" | tee -a "$LOG_FILE"
echo "  KB RESEARCH AND UPDATE AUTOMATION" | tee -a "$LOG_FILE"
echo "  Mode: $MODE" | tee -a "$LOG_FILE"
echo "  Started: $(date)" | tee -a "$LOG_FILE"
echo "════════════════════════════════════════════════════════════" | tee -a "$LOG_FILE"
echo "" | tee -a "$LOG_FILE"

# Function to log with timestamp
log() {
    echo "[$(date +%H:%M:%S)] $1" | tee -a "$LOG_FILE"
}

# Function to check for command
check_command() {
    if ! command -v "$1" &> /dev/null; then
        log "⚠️  Warning: $1 not installed"
        return 1
    fi
    return 0
}

# ============================================
# 1. SYSTEM HEALTH CHECK
# ============================================
log "1️⃣  System Health Check"

# Check for critical commands
for cmd in rg jq git; do
    check_command "$cmd" || log "   Install with: brew install $cmd"
done

# Check for Unity MCP port
if lsof -i :6400 &> /dev/null; then
    log "   ✅ Unity MCP active on port 6400"
else
    log "   ⚠️  Unity MCP not running on port 6400"
fi

# Check IDE processes
for ide in Windsurf Cursor Unity; do
    if pgrep -x "$ide" > /dev/null; then
        log "   ✅ $ide running"
    else
        log "   ℹ️  $ide not running"
    fi
done

echo "" | tee -a "$LOG_FILE"

# ============================================
# 2. MCP VERSION CHECK
# ============================================
log "2️⃣  MCP Version Check"

# Check Unity MCP version in configs
for config in ~/.windsurf/mcp.json ~/.cursor/mcp.json ~/.gemini/antigravity/mcp_config.json; do
    if [ -f "$config" ]; then
        ide=$(basename $(dirname "$config"))
        version=$(grep -o "unity-mcp@v[0-9.]*" "$config" | head -1 || echo "unknown")
        log "   $ide: $version"
    fi
done

log "   Latest check: https://github.com/CoplayDev/unity-mcp/releases"
echo "" | tee -a "$LOG_FILE"

# ============================================
# 3. KB INDEX FRESHNESS
# ============================================
log "3️⃣  KB Index Freshness"

KB_INDEX="$KB_ROOT/.claude/KB_MASTER_INDEX.md"
if [ -f "$KB_INDEX" ]; then
    age_seconds=$(( $(date +%s) - $(stat -f %m "$KB_INDEX" 2>/dev/null || stat -c %Y "$KB_INDEX") ))
    age_days=$(( age_seconds / 86400 ))

    if [ "$age_days" -gt 7 ]; then
        log "   ⚠️  Index is $age_days days old (regenerating...)"
        "$KB_ROOT/scripts/generate-kb-index.sh" >> "$LOG_FILE" 2>&1
        log "   ✅ Index regenerated"
    else
        log "   ✅ Index fresh ($age_days days old)"
    fi
else
    log "   ❌ Index missing! Generating..."
    "$KB_ROOT/scripts/generate-kb-index.sh" >> "$LOG_FILE" 2>&1
fi

echo "" | tee -a "$LOG_FILE"

# ============================================
# 4. CONFIG SYNC CHECK
# ============================================
log "4️⃣  Config Sync Check"

# Check if Windsurf and Cursor have same MCP config
if diff ~/.windsurf/mcp.json ~/.cursor/mcp.json &> /dev/null; then
    log "   ✅ Windsurf and Cursor configs in sync"
else
    log "   ⚠️  Windsurf and Cursor configs differ!"
    log "      Run: mcp-unity to sync"
fi

echo "" | tee -a "$LOG_FILE"

# ============================================
# 5. RESEARCH LATEST BEST PRACTICES (FULL MODE ONLY)
# ============================================
if [ "$MODE" = "--full" ]; then
    log "5️⃣  Research Latest Best Practices (this may take 30+ min)"
    log ""
    log "   📖 Recommended Research Topics:"
    log "   - Unity XR Interaction Toolkit 2026 best practices"
    log "   - AR Foundation 6.2 performance optimization"
    log "   - Unity MCP latest features and changelog"
    log "   - Meta Quest 3 development guidelines 2026"
    log "   - Windsurf Fast Context updates"
    log "   - ugrep vs ripgrep benchmarks 2026"
    log "   - MCP ecosystem new servers"
    log ""
    log "   ⚠️  Manual Action Required:"
    log "   1. Open Claude Code, Windsurf, or AntiGravity"
    log "   2. Research each topic above"
    log "   3. Update KB files with findings"
    log "   4. Commit changes to KB repo"
    log ""
else
    log "5️⃣  Research Skipped (use --full for deep research)"
fi

echo "" | tee -a "$LOG_FILE"

# ============================================
# 6. UPDATE RECOMMENDATIONS
# ============================================
log "6️⃣  Update Recommendations"

# Check for outdated search tools
if ! command -v ugrep &> /dev/null; then
    log "   💡 Optional: Install ugrep for 2x search speed"
    log "      brew install ugrep"
fi

# Check Unity log for warnings
UNITY_LOG="$HOME/Library/Logs/Unity/Editor.log"
if [ -f "$UNITY_LOG" ]; then
    error_count=$(tail -1000 "$UNITY_LOG" | grep -c "error CS" || echo "0")
    warning_count=$(tail -1000 "$UNITY_LOG" | grep -c "warning CS" || echo "0")

    if [ "$error_count" -gt 0 ]; then
        log "   ⚠️  Unity has $error_count errors in last 1000 lines"
    fi

    if [ "$warning_count" -gt 10 ]; then
        log "   ℹ️  Unity has $warning_count warnings in last 1000 lines"
    fi

    if [ "$error_count" -eq 0 ] && [ "$warning_count" -lt 10 ]; then
        log "   ✅ Unity console clean"
    fi
fi

echo "" | tee -a "$LOG_FILE"

# ============================================
# 7. BACKUP VERIFICATION
# ============================================
log "7️⃣  Backup Verification"

backup_count=$(find ~/.windsurf ~/.cursor ~/.gemini -name "*.backup*" 2>/dev/null | wc -l | tr -d ' ')
log "   Found $backup_count config backups"

if [ "$backup_count" -gt 0 ]; then
    log "   ✅ Config backups exist"
else
    log "   ⚠️  No config backups found!"
fi

echo "" | tee -a "$LOG_FILE"

# ============================================
# 8. SUMMARY & NEXT STEPS
# ============================================
log "════════════════════════════════════════════════════════════"
log "  SUMMARY"
log "════════════════════════════════════════════════════════════"

log ""
log "✅ Checks Complete"
log ""
log "📖 Full research log: $LOG_FILE"
log ""

if [ "$MODE" = "--quick" ]; then
    log "💡 Next Steps:"
    log "   1. Review log above for warnings"
    log "   2. Run weekly: ./KB_RESEARCH_AND_UPDATE.sh --quick"
    log "   3. Run monthly: ./KB_RESEARCH_AND_UPDATE.sh --full"
else
    log "💡 Next Steps:"
    log "   1. Perform manual research (see topic list above)"
    log "   2. Update KB files with findings"
    log "   3. Commit changes: cd $KB_ROOT && git add . && git commit -m 'Update: Latest research findings'"
    log "   4. KB index will auto-regenerate on commit"
fi

log ""
log "Completed: $(date)"
log "════════════════════════════════════════════════════════════"

# ============================================
# 9. OPTIONAL: OPEN LOG IN EDITOR
# ============================================
if [ -n "$EDITOR" ]; then
    read -p "Open log in editor? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        $EDITOR "$LOG_FILE"
    fi
fi
