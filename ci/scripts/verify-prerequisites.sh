#!/bin/bash
set -e

echo "=========================================="
echo "Phase 0: Tooling Readiness Verification"
echo "=========================================="
echo ""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

ERRORS=0
WARNINGS=0

# Function to check command exists and version
check_version() {
    local tool=$1
    local version_cmd=$2
    local expected_pattern=$3
    local severity=$4  # ERROR or WARNING

    echo -n "Checking $tool... "

    if ! command -v $tool &> /dev/null; then
        echo -e "${RED}NOT FOUND${NC}"
        if [ "$severity" = "ERROR" ]; then
            ((ERRORS++))
        else
            ((WARNINGS++))
        fi
        return 1
    fi

    version_output=$(eval $version_cmd 2>&1)
    echo "$version_output" | grep -q "$expected_pattern" || {
        echo -e "${YELLOW}VERSION MISMATCH${NC}"
        echo "  Found: $version_output"
        echo "  Expected pattern: $expected_pattern"
        if [ "$severity" = "ERROR" ]; then
            ((ERRORS++))
        else
            ((WARNINGS++))
        fi
        return 1
    }

    echo -e "${GREEN}OK${NC} ($version_output)"
    return 0
}

echo "1. .NET SDK & Tools"
echo "-------------------"
check_version "dotnet" "dotnet --version" "^10\." "ERROR"
check_version "upgrade-assistant" "upgrade-assistant --version" "." "ERROR"
dotnet tool list -g 2>/dev/null | grep -q "microsoft.dotnet.apicompat.tool" && echo -e "  ApiCompat: ${GREEN}OK${NC}" || { echo -e "  ApiCompat: ${RED}NOT FOUND${NC}"; ((ERRORS++)); }

echo ""
echo "2. Kubernetes Tools"
echo "-------------------"
check_version "kind" "kind version" "v0\.(30|3[1-9]|[4-9][0-9])" "WARNING"
check_version "kubectl" "kubectl version --client --short 2>&1 | head -1" "v1\.(3[4-9]|[4-9][0-9])" "WARNING"
check_version "helm" "helm version --short" "v3\.(1[8-9]|[2-9][0-9])" "WARNING"

echo ""
echo "3. Dapr"
echo "-------"
check_version "dapr" "dapr version 2>&1 | grep 'CLI version' | awk '{print \$3}'" "1\.16" "ERROR"

echo ""
echo "4. Language Runtimes"
echo "--------------------"
check_version "go" "go version | awk '{print \$3}'" "go1\.23" "WARNING"
check_version "python3" "python3 --version | awk '{print \$2}'" "3\.12" "WARNING"
check_version "node" "node --version" "v24\." "ERROR"
check_version "npm" "npm --version" "(10|11)\." "WARNING"

echo ""
echo "5. Artifact Directories"
echo "-----------------------"
for dir in artifacts/upgrade-assistant artifacts/api-analyzer artifacts/dependencies artifacts/performance; do
    if [ -d "$dir" ]; then
        echo -e "  $dir: ${GREEN}EXISTS${NC}"
    else
        echo -e "  $dir: ${RED}MISSING${NC}"
        ((ERRORS++))
    fi
done

echo ""
echo "6. Workspace Paths"
echo "------------------"
if [ "$PWD" = "/workspaces/reddog-code" ]; then
    echo -e "  Working directory: ${GREEN}CORRECT${NC} ($PWD)"
else
    echo -e "  Working directory: ${YELLOW}UNEXPECTED${NC} ($PWD, expected /workspaces/reddog-code)"
    ((WARNINGS++))
fi

if [ -f "RedDog.sln" ]; then
    echo -e "  RedDog.sln: ${GREEN}FOUND${NC}"
else
    echo -e "  RedDog.sln: ${RED}NOT FOUND${NC}"
    ((ERRORS++))
fi

echo ""
echo "=========================================="
echo "Verification Summary"
echo "=========================================="
echo -e "Errors: ${RED}$ERRORS${NC}"
echo -e "Warnings: ${YELLOW}$WARNINGS${NC}"
echo ""

if [ $ERRORS -eq 0 ]; then
    echo -e "${GREEN}✅ Phase 0 Prerequisites: PASSED${NC}"
    echo ""
    echo "Next steps:"
    echo "  1. Review warnings above (if any)"
    echo "  2. Proceed to Phase 1: Baseline Establishment"
    echo "  3. See: plan/testing-validation-strategy.md"
    exit 0
else
    echo -e "${RED}❌ Phase 0 Prerequisites: FAILED${NC}"
    echo ""
    echo "Fix errors above before proceeding to Phase 1."
    echo "See: plan/testing-validation-strategy.md"
    exit 1
fi
