#!/bin/bash
# KB_RESEARCH.sh - Automated Research & Discovery
# Version: 1.0
# Last Updated: 2025-01-07

set -e

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

KB_PATH=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase
RESEARCH_QUEUE="$KB_PATH/RESEARCH_QUEUE.md"

echo -e "${BLUE}🔬 Automated Research & Discovery${NC}"
echo "========================================"

# Initialize research queue if needed
if [ ! -f "$RESEARCH_QUEUE" ]; then
    cat > "$RESEARCH_QUEUE" << 'EOF'
# Research Queue - Automated Discoveries

**Purpose**: Automated research findings for manual review
**Last Updated**: Auto-generated

## Review Queue

> **Action Required**: Review discoveries below and update knowledgebase if valuable

---

EOF
fi

# 1. GitHub Trending Repos
echo ""
echo "1. GitHub Trending Repositories"
echo "--------------------------------"

# Check Unity trending
echo "Checking Unity trending..."
if command -v gh &> /dev/null; then
    # Using GitHub CLI if available
    gh repo list Unity-Technologies --limit 5 --json name,description,url,stargazerCount 2>/dev/null | \
    jq -r '.[] | "- [\(.name)](\(.url)) - \(.description) (⭐ \(.stargazerCount))"' >> "$RESEARCH_QUEUE.tmp" || true

    if [ -f "$RESEARCH_QUEUE.tmp" ]; then
        echo -e "\n## Unity-Technologies New Repos ($(date +%Y-%m-%d))\n" >> "$RESEARCH_QUEUE"
        cat "$RESEARCH_QUEUE.tmp" >> "$RESEARCH_QUEUE"
        rm "$RESEARCH_QUEUE.tmp"
        echo -e "${GREEN}✓${NC} Added Unity repos to research queue"
    fi
else
    echo -e "${YELLOW}⚠${NC} GitHub CLI not installed, skipping repo discovery"
    echo "   Install with: brew install gh"
fi

# 2. Search for New XR Patterns
echo ""
echo "2. XR Development Patterns"
echo "--------------------------"
echo "Searching GitHub for recent XR repos..."

# Manual web search approach (requires manual review)
echo -e "\n## Manual Research Tasks ($(date +%Y-%m-%d))\n" >> "$RESEARCH_QUEUE"
cat >> "$RESEARCH_QUEUE" << 'EOF'
### Recommended Searches:
1. GitHub: "Unity AR Foundation" created:>2025-01-01
2. GitHub: "Three.js WebXR" created:>2025-01-01
3. GitHub: "Gaussian Splatting Unity" created:>2024-12-01
4. ArXiv: cs.CV (Computer Vision) recent papers
5. ArXiv: cs.GR (Graphics) recent papers

### Blogs to Check:
- [ ] blog.unity.com
- [ ] threejs.org/blog
- [ ] keijiro.github.io
- [ ] dilmerv.medium.com

EOF

echo -e "${GREEN}✓${NC} Added research tasks"

# 3. Check for Tool Updates
echo ""
echo "3. AI Tool Updates"
echo "------------------"
echo "Checking for tool updates..."

# Check Claude Code version (if API available)
# Check Windsurf updates
# Check Cursor updates
echo -e "${YELLOW}⚠${NC} Manual check required for tool updates"

cat >> "$RESEARCH_QUEUE" << 'EOF'

### Tool Update Checks:
- [ ] Claude Code: Check for new features
- [ ] Windsurf: Check releases
- [ ] Cursor: Check updates
- [ ] MCP Servers: Check npm updates

EOF

# 4. Unity Asset Store (Manual Check Required)
echo ""
echo "4. Unity Asset Store"
echo "--------------------"
cat >> "$RESEARCH_QUEUE" << 'EOF'

### Unity Asset Store New Releases:
- [ ] XR Interaction Toolkit updates
- [ ] New VFX Graph assets
- [ ] AR Foundation tools
- [ ] Performance optimization tools

EOF

echo -e "${YELLOW}⚠${NC} Manual check required"

# 5. ArXiv Papers (Automated Search)
echo ""
echo "5. Academic Papers (ArXiv)"
echo "--------------------------"
echo "Searching recent papers..."

# Use arXiv API if curl available
if command -v curl &> /dev/null; then
    # Search Computer Vision papers
    ARXIV_RESULTS=$(curl -s "http://export.arxiv.org/api/query?search_query=cat:cs.CV&sortBy=lastUpdatedDate&sortOrder=descending&max_results=5" | \
    grep -o '<title>.*</title>' | sed 's/<[^>]*>//g' | tail -n +2 | head -5 2>/dev/null || echo "")

    if [ ! -z "$ARXIV_RESULTS" ]; then
        echo -e "\n## Recent ArXiv Papers - Computer Vision ($(date +%Y-%m-%d))\n" >> "$RESEARCH_QUEUE"
        echo "$ARXIV_RESULTS" | while read line; do
            echo "- $line" >> "$RESEARCH_QUEUE"
        done
        echo -e "${GREEN}✓${NC} Added ArXiv papers to queue"
    else
        echo -e "${YELLOW}⚠${NC} No ArXiv results"
    fi
else
    echo -e "${YELLOW}⚠${NC} curl not available"
fi

# 6. Summary
echo ""
echo "========================================"
echo -e "${GREEN}✅ Research Queue Updated${NC}"
echo "========================================"
echo ""
echo "Research queue: $RESEARCH_QUEUE"
echo ""
echo "Next Steps:"
echo "  1. Review research queue: code $RESEARCH_QUEUE"
echo "  2. Investigate interesting findings"
echo "  3. Add valuable discoveries to knowledgebase"
echo "  4. Update LEARNING_LOG.md with new patterns"
echo ""

# Add timestamp to queue
echo -e "\n---\n**Last Run**: $(date)\n" >> "$RESEARCH_QUEUE"

echo "Run again with: $KB_PATH/KB_RESEARCH.sh"
