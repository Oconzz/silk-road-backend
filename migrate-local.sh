#!/bin/bash

# Script to run Entity Framework migrations on the local database (Docker)
# Usage: ./migrate-local.sh [command]
#
# Available commands:
#   update          - Apply all pending migrations (default)
#   add <name>      - Create a new migration
#   remove          - Remove the last migration
#   list            - List all migrations
#   script          - Generate SQL script from migrations
#   status          - Show pending migrations

set -e

# Output colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Project directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/APIServer"

# Configuration for local Docker database
export ConnectionString=LocalConnectionString

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}  🐳 Local Migrations (Docker SQL Server)${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Check if Docker is running
if ! docker ps | grep -q "nmj-sqlserver"; then
    echo -e "${RED}❌ Error: The nmj-sqlserver container is not running${NC}"
    echo -e "${YELLOW}   Run: cd backend && docker-compose up -d${NC}"
    exit 1
fi

echo -e "${GREEN}✓ SQL Server Docker is running${NC}"
echo ""

cd "$PROJECT_DIR"

COMMAND=${1:-update}

case $COMMAND in
    update)
        echo -e "${YELLOW}📦 Applying pending migrations...${NC}"
        dotnet ef database update --context DBContext
        echo -e "${GREEN}✓ Migrations applied successfully${NC}"
        ;;
    add)
        if [ -z "$2" ]; then
            echo -e "${RED}❌ Error: You must specify a migration name${NC}"
            echo -e "${YELLOW}   Usage: ./migrate-local.sh add MigrationName${NC}"
            exit 1
        fi
        echo -e "${YELLOW}📝 Creating new migration: $2${NC}"
        dotnet ef migrations add "$2" --context DBContext
        echo -e "${GREEN}✓ Migration '$2' created successfully${NC}"
        ;;
    remove)
        echo -e "${YELLOW}🗑️  Removing last migration...${NC}"
        dotnet ef migrations remove --context DBContext
        echo -e "${GREEN}✓ Last migration removed${NC}"
        ;;
    list)
        echo -e "${YELLOW}📋 Migration list:${NC}"
        dotnet ef migrations list --context DBContext
        ;;
    script)
        OUTPUT_FILE="$SCRIPT_DIR/migrations-$(date +%Y%m%d-%H%M%S).sql"
        echo -e "${YELLOW}📄 Generating SQL script...${NC}"
        dotnet ef migrations script --context DBContext -o "$OUTPUT_FILE"
        echo -e "${GREEN}✓ Script generated: $OUTPUT_FILE${NC}"
        ;;
    status)
        echo -e "${YELLOW}📊 Migration status:${NC}"
        dotnet ef migrations list --context DBContext --pending
        ;;
    *)
        echo -e "${RED}❌ Unknown command: $COMMAND${NC}"
        echo ""
        echo "Available commands:"
        echo "  update          - Apply all pending migrations (default)"
        echo "  add <name>      - Create a new migration"
        echo "  remove          - Remove the last migration"
        echo "  list            - List all migrations"
        echo "  script          - Generate SQL script from migrations"
        echo "  status          - Show pending migrations"
        exit 1
        ;;
esac

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
