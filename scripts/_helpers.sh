#!/usr/bin/env bash
# Shared color variables and print functions for Red Dog scripts.

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

print_status()  { echo -e "${GREEN}✓${NC} $*"; }
print_warning() { echo -e "${YELLOW}⚠${NC} $*"; }
print_error()   { echo -e "${RED}✗${NC} $*"; }
