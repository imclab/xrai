#!/bin/bash
# System-Wide Auto-Improvement
# Runs HOURLY via LaunchAgent - ZERO TOKENS
# Covers: MCP, Agents, KB, Hooks, Sessions, Memory

set -e

# === CONFIGURATION ===
LOG_DIR="$HOME/.claude/logs"
REPORT_DIR="$HOME/.claude/reports"
KB_DIR="$HOME/Documents/GitHub/Unity-XR-AI/KnowledgeBase"
AGENT_DIR="$HOME/.claude/agents"
HOOK_DIR="$HOME/.claude/hooks"
SESSION_DIR="$HOME/.claude/session-state"

mkdir -p "$LOG_DIR" "$REPORT_DIR" "$SESSION_DIR"

TIMESTAMP=$(date '+%Y-%m-%d %H:%M')
REPORT_FILE="$REPORT_DIR/health-$(date +%Y%m%d-%H%M).md"
LATEST_REPORT="$REPORT_DIR/LATEST_HEALTH.md"

# === LOGGING ===
log() {
    echo "[$(date '+%H:%M:%S')] $1"
}

report() {
    echo "$1" >> "$REPORT_FILE"
}

# === START REPORT ===
cat > "$REPORT_FILE" << HEADER
# System Health Report
**Generated**: $TIMESTAMP
**Type**: Hourly Auto-Improvement

---

HEADER

log "=== Hourly Auto-Improvement Started ==="

# === 1. MCP HEALTH ===
report "## 1. MCP Health"
report ""

mcp_unity=$(ps aux | grep -i "mcp-for-unity" | grep -v grep | wc -l | tr -d ' ')
mcp_github=$(ps aux | grep "mcp-server-github" | grep -v grep | wc -l | tr -d ' ')
mcp_chroma=$(ps aux | grep "chroma-mcp" | grep -v grep | wc -l | tr -d ' ')
mcp_total=$((mcp_unity + mcp_github + mcp_chroma))

# Kill duplicates if needed
duplicates_killed=0
if [ "$mcp_unity" -gt 1 ]; then
    pids=$(ps -eo pid,lstart,command | grep "mcp-for-unity" | grep -v grep | sort -k2,5 | awk '{print $1}' | head -n $((mcp_unity - 1)))
    echo "$pids" | xargs kill 2>/dev/null || true
    duplicates_killed=$((duplicates_killed + mcp_unity - 1))
fi

if [ "$mcp_unity" -le 1 ]; then status="✅"; else status="⚠️ (killed duplicates)"; fi
report "| Component | Count | Status |"
report "|-----------|-------|--------|"
report "| Unity MCP | $mcp_unity | $status |"
report "| GitHub MCP | $mcp_github | ✅ |"
report "| Chroma MCP | $mcp_chroma | ✅ |"
report "| **Duplicates Killed** | $duplicates_killed | — |"
report ""

log "1. MCP: $mcp_total processes, killed $duplicates_killed duplicates"

# === 2. AGENT HEALTH ===
report "## 2. Agent Health"
report ""

agent_total=$(ls "$AGENT_DIR"/*.md 2>/dev/null | wc -l | tr -d ' ')
agent_haiku=$(grep -l "model: haiku" "$AGENT_DIR"/*.md 2>/dev/null | wc -l | tr -d ' ')
agent_opus=$(grep -l "model: opus" "$AGENT_DIR"/*.md 2>/dev/null | wc -l | tr -d ' ')
agent_sonnet=$((agent_total - agent_haiku - agent_opus))

# Check for agents without model specification
agent_no_model=$(grep -L "model:" "$AGENT_DIR"/*.md 2>/dev/null | wc -l | tr -d ' ')

report "| Tier | Count | Cost |"
report "|------|-------|------|"
report "| Haiku (fast) | $agent_haiku | 0.3x |"
report "| Sonnet (default) | $agent_sonnet | 1x |"
report "| Opus (deep) | $agent_opus | 3-5x |"
report "| **Total** | $agent_total | — |"
if [ "$agent_no_model" -gt 0 ]; then
    report ""
    report "⚠️ **$agent_no_model agents missing model specification** (will use inherited)"
fi
report ""

log "2. Agents: $agent_total total ($agent_haiku haiku, $agent_opus opus)"

# === 3. HOOK HEALTH ===
report "## 3. Hook Health"
report ""

hook_count=$(ls "$HOOK_DIR"/*.sh 2>/dev/null | wc -l | tr -d ' ')
hooks_executable=$(find "$HOOK_DIR" -name "*.sh" -perm +111 | wc -l | tr -d ' ')

report "| Hook | Executable | Status |"
report "|------|------------|--------|"
for hook in "$HOOK_DIR"/*.sh; do
    name=$(basename "$hook")
    if [ -x "$hook" ]; then
        report "| $name | ✅ | Active |"
    else
        report "| $name | ❌ | **Not executable** |"
        chmod +x "$hook" 2>/dev/null && report "| ^ | Fixed | Auto-fixed |"
    fi
done
report ""

log "3. Hooks: $hook_count total, $hooks_executable executable"

# === 4. KNOWLEDGEBASE HEALTH ===
report "## 4. Knowledgebase Health"
report ""

kb_files=$(ls "$KB_DIR"/*.md 2>/dev/null | wc -l | tr -d ' ')
kb_size=$(du -sh "$KB_DIR" 2>/dev/null | cut -f1)

# Check largest files
report "| Metric | Value |"
report "|--------|-------|"
report "| Total files | $kb_files |"
report "| Total size | $kb_size |"
report ""

report "### Largest Files (potential bloat)"
report ""
report "| File | Size |"
report "|------|------|"
ls -lhS "$KB_DIR"/*.md 2>/dev/null | head -5 | while read line; do
    size=$(echo "$line" | awk '{print $5}')
    file=$(echo "$line" | awk '{print $NF}' | xargs basename)
    report "| $file | $size |"
done
report ""

# LEARNING_LOG check
if [ -f "$KB_DIR/LEARNING_LOG.md" ]; then
    ll_lines=$(wc -l < "$KB_DIR/LEARNING_LOG.md")
    if [ "$ll_lines" -gt 3000 ]; then
        report "⚠️ **LEARNING_LOG.md is $ll_lines lines** - consider archiving"
    fi
fi

log "4. KB: $kb_files files, $kb_size total"

# === 5. SESSION STATE ===
report "## 5. Session State"
report ""

session_files=$(ls "$SESSION_DIR"/*.json 2>/dev/null | wc -l | tr -d ' ')
old_sessions=$(find "$SESSION_DIR" -name "*.json" -mtime +1 2>/dev/null | wc -l | tr -d ' ')

# Clean old sessions (>24 hours)
if [ "$old_sessions" -gt 0 ]; then
    find "$SESSION_DIR" -name "*.json" -mtime +1 -delete 2>/dev/null || true
    report "| Action | Count |"
    report "|--------|-------|"
    report "| Sessions cleaned | $old_sessions |"
    report "| Sessions remaining | $((session_files - old_sessions)) |"
else
    report "✅ Session state clean ($session_files active sessions)"
fi
report ""

log "5. Sessions: $session_files total, cleaned $old_sessions old"

# === 6. FAILURE TRACKING ===
report "## 6. Circuit Breaker Status"
report ""

if [ -f "$KB_DIR/FAILURE_LOG.md" ]; then
    total_failures=$(grep -c "Circuit Breaker" "$KB_DIR/FAILURE_LOG.md" 2>/dev/null || echo 0)
    recent_failures=$(grep "$(date +%Y-%m-%d)" "$KB_DIR/FAILURE_LOG.md" 2>/dev/null | grep -c "Circuit Breaker" || echo 0)
    
    report "| Metric | Count |"
    report "|--------|-------|"
    report "| Total triggers (all time) | $total_failures |"
    report "| Today's triggers | $recent_failures |"
    
    if [ "$recent_failures" -gt 5 ]; then
        report ""
        report "⚠️ **High failure rate today** - review FAILURE_LOG.md"
    fi
else
    report "✅ No failure log (good - no circuit breaker events)"
fi
report ""

log "6. Failures: $recent_failures today, $total_failures total"

# === 7. SYSTEM RESOURCES ===
report "## 7. System Resources"
report ""

cpu_usage=$(top -l 1 | head -4 | grep "CPU" | awk '{print $3}' | tr -d '%')
mem_pressure=$(memory_pressure 2>/dev/null | grep "System-wide" | awk '{print $4}' || echo "unknown")

report "| Resource | Value | Status |"
report "|----------|-------|--------|"
if [ -n "$cpu_usage" ]; then
    if [ "${cpu_usage%.*}" -lt 70 ]; then cpu_status="✅"; else cpu_status="⚠️"; fi
    report "| CPU Usage | ${cpu_usage}% | $cpu_status |"
fi
report "| Memory Pressure | $mem_pressure | — |"
report ""

log "7. Resources: CPU ${cpu_usage:-?}%, Memory $mem_pressure"

# === 8. SUMMARY ===
report "## Summary"
report ""

issues=0
[ "$duplicates_killed" -gt 0 ] && issues=$((issues + 1))
[ "$agent_no_model" -gt 0 ] && issues=$((issues + 1))
[ "$hooks_executable" -lt "$hook_count" ] && issues=$((issues + 1))
[ "${recent_failures:-0}" -gt 5 ] && issues=$((issues + 1))

if [ "$issues" -eq 0 ]; then
    report "### ✅ System Healthy"
    report ""
    report "No issues detected. All systems operating normally."
else
    report "### ⚠️ $issues Issue(s) Found"
    report ""
    report "Review sections above for details."
fi

report ""
report "---"
report "*Next check: $(date -v+1H '+%Y-%m-%d %H:%M')*"

# Copy to latest
cp "$REPORT_FILE" "$LATEST_REPORT"

# Clean old reports (keep last 24)
ls -t "$REPORT_DIR"/health-*.md 2>/dev/null | tail -n +25 | xargs rm -f 2>/dev/null || true

log "=== Complete. Report: $LATEST_REPORT ==="

# Output summary to stdout (visible in launchctl logs)
echo ""
echo "=== HEALTH SUMMARY ==="
echo "MCP: $mcp_total processes ($duplicates_killed duplicates killed)"
echo "Agents: $agent_total ($agent_haiku haiku, $agent_opus opus)"
echo "Hooks: $hook_count active"
echo "KB: $kb_files files"
echo "Failures today: ${recent_failures:-0}"
echo "Status: $([ "$issues" -eq 0 ] && echo "✅ Healthy" || echo "⚠️ $issues issues")"
echo "Report: $LATEST_REPORT"
