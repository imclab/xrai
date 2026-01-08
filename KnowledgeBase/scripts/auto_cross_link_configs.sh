#!/bin/bash
# Auto Cross-Link Config Files and Global Rules
# Ensures all IDE configs reference each other and KB
# Always creates backups before modifications
#
# Usage: ./auto_cross_link_configs.sh

set -e

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_ROOT="$HOME/Documents/GitHub/code-backups/config-backups-$TIMESTAMP"

echo "════════════════════════════════════════════════════════════"
echo "  AUTO CROSS-LINK GLOBAL RULES & CONFIGS"
echo "  Backup: $BACKUP_ROOT"
echo "════════════════════════════════════════════════════════════"
echo ""

# ============================================
# 1. CREATE BACKUPS (ALWAYS!)
# ============================================
echo "1️⃣  Creating backups..."

mkdir -p "$BACKUP_ROOT"

# Backup all config files
for file in \
    ~/.claude/CLAUDE.md \
    ~/Documents/GitHub/portals_v4/CLAUDE.md \
    ~/.gemini/GEMINI.md \
    ~/.windsurf/mcp.json \
    ~/.cursor/mcp.json \
    ~/.gemini/antigravity/mcp_config.json \
    ~/.zshrc
do
    if [ -f "$file" ]; then
        cp "$file" "$BACKUP_ROOT/$(basename $file).backup"
        echo "   ✅ Backed up: $(basename $file)"
    else
        echo "   ⚠️  Not found: $file"
    fi
done

echo "   📁 Backups stored: $BACKUP_ROOT"
echo ""

# ============================================
# 2. VERIFY CROSS-LINKS IN GLOBAL RULES
# ============================================
echo "2️⃣  Verifying cross-links..."

# Function to check if file contains reference to another
check_link() {
    local file="$1"
    local search_term="$2"
    local friendly_name="$3"

    if [ ! -f "$file" ]; then
        echo "   ⚠️  File not found: $file"
        return 1
    fi

    if grep -q "$search_term" "$file"; then
        echo "   ✅ $friendly_name: Links found"
        return 0
    else
        echo "   ⚠️  $friendly_name: Missing links to $search_term"
        return 1
    fi
}

# Check Claude Code global rules
check_link ~/.claude/CLAUDE.md "GEMINI.md" "Claude → AntiGravity" || true
check_link ~/.claude/CLAUDE.md "MASTER_CONFIG_REGISTRY" "Claude → Config Registry" || true

# Check AntiGravity global rules
check_link ~/.gemini/GEMINI.md "CLAUDE.md" "AntiGravity → Claude" || true
check_link ~/.gemini/GEMINI.md "KB_MASTER_INDEX" "AntiGravity → KB" || true

# Check KB master index
check_link ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/KB_MASTER_INDEX.md "MASTER_CONFIG_REGISTRY" "KB → Registry" || true

echo ""

# ============================================
# 3. GENERATE CROSS-REFERENCE FOOTER
# ============================================
echo "3️⃣  Generating cross-reference footer..."

FOOTER_FILE="$HOME/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/CROSS_REFERENCE_FOOTER.md"

cat > "$FOOTER_FILE" << 'EOF'
---

## 🔗 Cross-IDE Configuration Links

**This Document**: Part of unified configuration system across all AI tools

### Global Rules Files
- **Claude Code**: `~/.claude/CLAUDE.md` (this tool)
- **AntiGravity**: `~/.gemini/GEMINI.md`
- **Windsurf**: Built into Cascade (no separate file)
- **Cursor**: `.cursorrules` (project-specific)

### MCP Configurations
- **Claude/Windsurf**: `~/.windsurf/mcp.json`
- **Cursor**: `~/.cursor/mcp.json`
- **AntiGravity**: `~/.gemini/antigravity/mcp_config.json`
- **Profiles**: `~/.claude/mcp-configs/*.json`

### Knowledge Base
- **Master Index**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/KB_MASTER_INDEX.md`
- **Config Registry**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/MASTER_CONFIG_REGISTRY.md`
- **Maintenance**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/PERIODIC_MAINTENANCE.md`
- **530+ Repos**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md`

### Automation & Scripts
```bash
# MCP Profile Switchers
mcp-all-unity          # Update all IDEs to Unity profile
mcp-status             # Check Windsurf/Cursor MCP
mcp-antigravity-status # Check AntiGravity MCP

# KB Maintenance
kb-research            # Weekly health check (5 min)
kb-research-full       # Monthly deep research (30 min)
kb-update-all          # Full system update

# Backups
# All in: ~/Documents/GitHub/code-backups/config-backups-YYYYMMDD-HHMMSS/
```

### Key Documentation
- **Unity XRI 3.1**: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/
- **AR Foundation 6.2**: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.2/
- **Unity MCP**: https://github.com/CoplayDev/unity-mcp
- **MCP Protocol**: https://modelcontextprotocol.io/
- **Windsurf Docs**: https://docs.windsurf.com/
- **AntiGravity Docs**: https://antigravity.google/docs/

### Auto-Update System
- **Daily**: KB index regeneration (git hooks)
- **Weekly**: `kb-research` (health check)
- **Monthly**: `kb-research-full` (deep research)
- **Quarterly**: Full audit + benchmarking

**Last Auto-Updated**: Auto-generated on demand
**Token Savings**: ~90-140K per session across all IDEs
EOF

echo "   ✅ Generated: $FOOTER_FILE"
echo ""

# ============================================
# 4. UPDATE GLOBAL RULES WITH CROSS-LINKS
# ============================================
echo "4️⃣  Updating global rules with cross-links..."

# Add cross-reference to Claude Code global rules if not present
if ! grep -q "CROSS_REFERENCE_FOOTER" ~/.claude/CLAUDE.md; then
    echo "" >> ~/.claude/CLAUDE.md
    cat "$FOOTER_FILE" >> ~/.claude/CLAUDE.md
    echo "   ✅ Updated: ~/.claude/CLAUDE.md"
else
    echo "   ℹ️  Already linked: ~/.claude/CLAUDE.md"
fi

# Add cross-reference to AntiGravity global rules if not present
if ! grep -q "CROSS_REFERENCE_FOOTER" ~/.gemini/GEMINI.md; then
    echo "" >> ~/.gemini/GEMINI.md
    cat "$FOOTER_FILE" >> ~/.gemini/GEMINI.md
    echo "   ✅ Updated: ~/.gemini/GEMINI.md"
else
    echo "   ℹ️  Already linked: ~/.gemini/GEMINI.md"
fi

# Add cross-reference to project-specific Claude rules if not present
if [ -f ~/Documents/GitHub/portals_v4/CLAUDE.md ]; then
    if ! grep -q "CROSS_REFERENCE_FOOTER" ~/Documents/GitHub/portals_v4/CLAUDE.md; then
        echo "" >> ~/Documents/GitHub/portals_v4/CLAUDE.md
        cat "$FOOTER_FILE" >> ~/Documents/GitHub/portals_v4/CLAUDE.md
        echo "   ✅ Updated: ~/Documents/GitHub/portals_v4/CLAUDE.md"
    else
        echo "   ℹ️  Already linked: ~/Documents/GitHub/portals_v4/CLAUDE.md"
    fi
fi

echo ""

# ============================================
# 5. VERIFY MCP CONFIG SYNC
# ============================================
echo "5️⃣  Verifying MCP config sync..."

if diff ~/.windsurf/mcp.json ~/.cursor/mcp.json &> /dev/null; then
    echo "   ✅ Windsurf and Cursor in sync"
else
    echo "   ⚠️  Windsurf and Cursor differ! Run: mcp-unity"
fi

# Check Unity MCP versions
for config in ~/.windsurf/mcp.json ~/.cursor/mcp.json ~/.gemini/antigravity/mcp_config.json; do
    if [ -f "$config" ]; then
        ide=$(basename $(dirname "$config"))
        version=$(grep -o "unity-mcp@v[0-9.]*" "$config" | head -1 || echo "unknown")
        echo "   $ide: $version"
    fi
done

echo ""

# ============================================
# 6. SUMMARY
# ============================================
echo "════════════════════════════════════════════════════════════"
echo "  SUMMARY"
echo "════════════════════════════════════════════════════════════"
echo ""
echo "✅ Backups Created: $BACKUP_ROOT"
echo "✅ Cross-links verified and updated"
echo "✅ MCP configs checked"
echo ""
echo "📖 Cross-reference footer: $FOOTER_FILE"
echo ""
echo "💡 Next Steps:"
echo "   1. Review changes in global rules files"
echo "   2. Restart IDEs to load updated configs"
echo "   3. Run: kb-research to verify system health"
echo ""
echo "🔄 To rollback changes:"
echo "   cp $BACKUP_ROOT/*.backup ~/.claude/"
echo "   cp $BACKUP_ROOT/*.backup ~/.gemini/"
echo ""
echo "════════════════════════════════════════════════════════════"
