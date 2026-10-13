#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

# Determine repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

CURSOR_RULES_DIR="${CURSOR_RULES_DIR:-$REPO_ROOT/.cursor/rules}"
AGENTS_FILE="${AGENTS_FILE:-$REPO_ROOT/AGENTS.md}"

if [[ ! -d "$CURSOR_RULES_DIR" ]]; then
    echo "ERROR: Directory not found: $CURSOR_RULES_DIR" >&2
    exit 1
fi

if [[ ! -f "$AGENTS_FILE" ]]; then
    echo "ERROR: File not found: $AGENTS_FILE" >&2
    exit 1
fi

# Helper to feed stream into command/pipe without generating empty newline when empty
to_stream() {
    local val="$1"
    if [[ -n "$val" ]]; then
        printf "%s\n" "$val"
    fi
}

# Helper to get line count of non-empty variable
count_lines() {
    local val="$1"
    if [[ -z "$val" ]]; then
        echo 0
    else
        echo "$val" | wc -l | tr -d ' '
    fi
}

# 1. Get sorted list of rule files in .cursor/rules (*.mdc)
cursor_rules=$(find "$CURSOR_RULES_DIR" -maxdepth 1 -name "*.mdc" -exec basename {} \; | sort)

# 2. Extract referenced rule files from AGENTS.md (*.mdc)
raw_agents_rules=$( (grep -oE '\.cursor/rules/[^)]+\.mdc' "$AGENTS_FILE" 2>/dev/null || true) | sed -E 's|.*/||' | sort)
agents_rules=$(to_stream "$raw_agents_rules" | sort -u)
duplicate_agents=$(to_stream "$raw_agents_rules" | uniq -d)

has_error=0

cursor_count=$(count_lines "$cursor_rules")
agents_count=$(count_lines "$agents_rules")

# Check for zero AI rules
if [[ "$cursor_count" -eq 0 ]]; then
    echo "ERROR: No rule files (*.mdc) found in $CURSOR_RULES_DIR." >&2
    has_error=1
fi

if [[ "$agents_count" -eq 0 ]]; then
    echo "ERROR: No rule files referenced in $AGENTS_FILE." >&2
    has_error=1
fi

if [[ -n "$duplicate_agents" ]]; then
    echo "ERROR: The following rules are referenced more than once in AGENTS.md:" >&2
    echo "$duplicate_agents" | sed 's/^/  - /' >&2
    has_error=1
fi

# 3. Check for rules present in .cursor/rules but missing in AGENTS.md
missing_in_agents=$(comm -23 <(to_stream "$cursor_rules") <(to_stream "$agents_rules"))

# 4. Check for rules referenced in AGENTS.md but missing in .cursor/rules
missing_in_cursor=$(comm -13 <(to_stream "$cursor_rules") <(to_stream "$agents_rules"))

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

echo "SUCCESS: All $cursor_count rules in .cursor/rules/ and AGENTS.md are fully synchronized."
exit 0

