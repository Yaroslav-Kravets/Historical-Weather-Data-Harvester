#!/usr/bin/env bash
set -euo pipefail

# Determine repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

CURSOR_RULES_DIR="$REPO_ROOT/.cursor/rules"
AGENTS_FILE="$REPO_ROOT/AGENTS.md"

if [[ ! -d "$CURSOR_RULES_DIR" ]]; then
    echo "ERROR: Directory not found: $CURSOR_RULES_DIR" >&2
    exit 1
fi

if [[ ! -f "$AGENTS_FILE" ]]; then
    echo "ERROR: File not found: $AGENTS_FILE" >&2
    exit 1
fi

# 1. Get sorted list of rule files in .cursor/rules (*.mdc)
cursor_rules=$(find "$CURSOR_RULES_DIR" -maxdepth 1 -name "*.mdc" -exec basename {} \; | sort)

# 2. Extract referenced rule files from AGENTS.md (*.mdc)
agents_rules=$(grep -oE '\.cursor/rules/[^)]+\.mdc' "$AGENTS_FILE" | while read -r path; do basename "$path"; done | sort -u)

# 3. Check for rules present in .cursor/rules but missing in AGENTS.md
missing_in_agents=$(comm -23 <(echo "$cursor_rules") <(echo "$agents_rules"))

# 4. Check for rules referenced in AGENTS.md but missing in .cursor/rules
missing_in_cursor=$(comm -13 <(echo "$cursor_rules") <(echo "$agents_rules"))

has_error=0

if [[ -n "$missing_in_agents" ]]; then
    echo "ERROR: The following rules exist in .cursor/rules/ but are missing from AGENTS.md:" >&2
    echo "$missing_in_agents" | sed 's/^/  - /' >&2
    has_error=1
fi

if [[ -n "$missing_in_cursor" ]]; then
    echo "ERROR: The following rules are referenced in AGENTS.md but do not exist in .cursor/rules/:" >&2
    echo "$missing_in_cursor" | sed 's/^/  - /' >&2
    has_error=1
fi

if [[ $has_error -ne 0 ]]; then
    exit 1
fi

rule_count=$(echo "$cursor_rules" | wc -l | tr -d ' ')
echo "SUCCESS: All $rule_count rules in .cursor/rules/ and AGENTS.md are fully synchronized."
exit 0
