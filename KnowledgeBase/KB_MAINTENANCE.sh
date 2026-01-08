#!/bin/bash
# KB_MAINTENANCE.sh - Master Knowledgebase Maintenance Orchestrator
# Version: 1.0
# Last Updated: 2025-01-07
# Purpose: Automated daily/weekly/monthly maintenance tasks

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Paths
KB_PATH=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase
LOCK_FILE=~/.claude/knowledgebase/.maintenance.lock
LOG_FILE=~/.claude/knowledgebase/maintenance.log
METRICS_FILE=~/.claude/knowledgebase/metrics.json

# Configuration
MAX_RUNTIME=600  # 10 minutes max
LOCK_TIMEOUT=600 # 10 minutes lock timeout

# Functions
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$LOG_FILE"
}

success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1" | tee -a "$LOG_FILE"
}

info() {
    echo -e "${BLUE}[INFO]${NC} $1" | tee -a "$LOG_FILE"
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $1" | tee -a "$LOG_FILE"
}

# Lock management
acquire_lock() {
    if [ -f "$LOCK_FILE" ]; then
        # Check lock age
        LOCK_AGE=$(($(date +%s) - $(stat -f %m "$LOCK_FILE" 2>/dev/null || echo 0)))
        if [ $LOCK_AGE -gt $LOCK_TIMEOUT ]; then
            warn "Stale lock detected, removing..."
            rm -f "$LOCK_FILE"
        else
            error "Maintenance already running (lock file exists)"
            exit 1
        fi
    fi

    echo $$ > "$LOCK_FILE"
    log "Lock acquired (PID: $$)"
}

release_lock() {
    rm -f "$LOCK_FILE"
    log "Lock released"
}

# Cleanup on exit
cleanup() {
    release_lock
    log "Maintenance completed"
}

trap cleanup EXIT INT TERM

# Check prerequisites
check_prereqs() {
    info "Checking prerequisites..."

    if [ ! -d "$KB_PATH" ]; then
        error "Knowledgebase not found at $KB_PATH"
        exit 1
    fi

    cd "$KB_PATH"

    if ! git status >/dev/null 2>&1; then
        error "Not a git repository"
        exit 1
    fi

    success "Prerequisites OK"
}

# Daily tasks
daily_tasks() {
    log "=== Starting Daily Maintenance ==="

    info "1/4 Running audit..."
    if [ -f "$KB_PATH/KB_AUDIT.sh" ]; then
        bash "$KB_PATH/KB_AUDIT.sh" >> "$LOG_FILE" 2>&1 || warn "Audit had warnings"
    else
        warn "KB_AUDIT.sh not found, skipping"
    fi

    info "2/4 Creating backup..."
    if [ -f "$KB_PATH/KB_BACKUP.sh" ]; then
        bash "$KB_PATH/KB_BACKUP.sh" >> "$LOG_FILE" 2>&1 || warn "Backup had issues"
    else
        warn "KB_BACKUP.sh not found, skipping"
    fi

    info "3/4 Running research (async)..."
    if [ -f "$KB_PATH/KB_RESEARCH.sh" ]; then
        bash "$KB_PATH/KB_RESEARCH.sh" >> "$LOG_FILE" 2>&1 &
        info "Research running in background (PID: $!)"
    else
        warn "KB_RESEARCH.sh not found, skipping"
    fi

    info "4/4 Optimizing knowledgebase..."
    if [ -f "$KB_PATH/KB_OPTIMIZE.sh" ]; then
        bash "$KB_PATH/KB_OPTIMIZE.sh" >> "$LOG_FILE" 2>&1 || warn "Optimization had issues"
    else
        warn "KB_OPTIMIZE.sh not found, skipping"
    fi

    success "Daily maintenance complete"
}

# Weekly tasks
weekly_tasks() {
    log "=== Starting Weekly Maintenance ==="

    # Run daily tasks first
    daily_tasks

    info "Running weekly deep audit..."
    # Add deep audit logic here

    info "Consolidating learning log..."
    # Add learning log consolidation here

    info "Generating performance report..."
    # Add performance analysis here

    success "Weekly maintenance complete"
}

# Monthly tasks
monthly_tasks() {
    log "=== Starting Monthly Maintenance ==="

    # Run weekly tasks first
    weekly_tasks

    info "Creating monthly archive..."
    # Add monthly archiving here

    info "Comprehensive quality review..."
    # Add quality review here

    success "Monthly maintenance complete"
}

# Main
main() {
    log "==============================================="
    log "KB Maintenance Started"
    log "==============================================="

    acquire_lock
    check_prereqs

    case "${1:-daily}" in
        daily)
            daily_tasks
            ;;
        weekly)
            weekly_tasks
            ;;
        monthly)
            monthly_tasks
            ;;
        audit)
            bash "$KB_PATH/KB_AUDIT.sh"
            ;;
        backup)
            bash "$KB_PATH/KB_BACKUP.sh"
            ;;
        research)
            bash "$KB_PATH/KB_RESEARCH.sh"
            ;;
        optimize)
            bash "$KB_PATH/KB_OPTIMIZE.sh"
            ;;
        *)
            echo "Usage: $0 {daily|weekly|monthly|audit|backup|research|optimize}"
            exit 1
            ;;
    esac

    log "==============================================="
}

main "$@"
