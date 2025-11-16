#!/bin/bash

# Ludiscan Unity API Client - Version Bump Script
# Usage: ./scripts/bump-version.sh [major|minor|patch]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

PACKAGE_JSON="Assets/Matuyuhi/LudiscanApiClient/package.json"

# Check if package.json exists
if [ ! -f "$PACKAGE_JSON" ]; then
    echo -e "${RED}Error: $PACKAGE_JSON not found${NC}"
    exit 1
fi

# Get bump type from argument
BUMP_TYPE=${1:-patch}

if [[ ! "$BUMP_TYPE" =~ ^(major|minor|patch)$ ]]; then
    echo -e "${RED}Error: Invalid bump type. Use 'major', 'minor', or 'patch'${NC}"
    echo "Usage: $0 [major|minor|patch]"
    exit 1
fi

# Extract current version from package.json
CURRENT_VERSION=$(grep -oP '(?<="version": ")[^"]*' "$PACKAGE_JSON")

if [ -z "$CURRENT_VERSION" ]; then
    echo -e "${RED}Error: Could not extract version from $PACKAGE_JSON${NC}"
    exit 1
fi

echo -e "${YELLOW}Current version: $CURRENT_VERSION${NC}"

# Split version into major, minor, patch
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"

# Bump version based on type
case $BUMP_TYPE in
    major)
        MAJOR=$((MAJOR + 1))
        MINOR=0
        PATCH=0
        ;;
    minor)
        MINOR=$((MINOR + 1))
        PATCH=0
        ;;
    patch)
        PATCH=$((PATCH + 1))
        ;;
esac

NEW_VERSION="$MAJOR.$MINOR.$PATCH"

echo -e "${GREEN}New version: $NEW_VERSION${NC}"

# Ask for confirmation
read -p "Do you want to bump version from $CURRENT_VERSION to $NEW_VERSION? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 0
fi

# Update package.json
echo "Updating $PACKAGE_JSON..."
sed -i "s/\"version\": \"$CURRENT_VERSION\"/\"version\": \"$NEW_VERSION\"/" "$PACKAGE_JSON"

# Verify the change
UPDATED_VERSION=$(grep -oP '(?<="version": ")[^"]*' "$PACKAGE_JSON")
if [ "$UPDATED_VERSION" != "$NEW_VERSION" ]; then
    echo -e "${RED}Error: Failed to update version in $PACKAGE_JSON${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Updated $PACKAGE_JSON${NC}"

# Check if there are changes to commit
if [ -n "$(git status --porcelain)" ]; then
    # Stage the changes
    git add "$PACKAGE_JSON"

    # Commit the changes
    echo "Committing version bump..."
    git commit -m "chore: bump version to $NEW_VERSION"
    echo -e "${GREEN}✓ Committed version bump${NC}"

    # Create git tag
    TAG_NAME="v$NEW_VERSION"
    echo "Creating git tag $TAG_NAME..."
    git tag -a "$TAG_NAME" -m "Release $NEW_VERSION"
    echo -e "${GREEN}✓ Created tag $TAG_NAME${NC}"

    echo ""
    echo -e "${GREEN}Version bump complete!${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Review the changes: git show"
    echo "2. Push the commit: git push origin <branch>"
    echo "3. Push the tag: git push origin $TAG_NAME"
    echo ""
    echo "Or push both at once:"
    echo "  git push origin <branch> --follow-tags"
else
    echo -e "${YELLOW}No changes detected. Version already updated?${NC}"
fi
