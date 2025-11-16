# Versioning Guide

This document describes how to manage versions and create releases for the Ludiscan Unity API Client.

## Version Numbering

We follow [Semantic Versioning](https://semver.org/) (SemVer):

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Add functionality in a backwards compatible manner
- **PATCH** version: Backwards compatible bug fixes

Current version: `1.0.0` (see `Assets/Matuyuhi/LudiscanApiClient/package.json`)

## Methods to Create a Release

### Method 1: Using the Shell Script (Recommended for Local Development)

Use the `bump-version.sh` script to bump the version, update package.json, create a commit, and create a git tag:

```bash
# Bump patch version (1.0.0 -> 1.0.1)
./scripts/bump-version.sh patch

# Bump minor version (1.0.0 -> 1.1.0)
./scripts/bump-version.sh minor

# Bump major version (1.0.0 -> 2.0.0)
./scripts/bump-version.sh major
```

The script will:
1. Show the current and new version
2. Ask for confirmation
3. Update `package.json`
4. Create a git commit
5. Create a git tag
6. Provide instructions for pushing

After running the script, push your changes:

```bash
# Push commit and tag
git push origin <branch-name> --follow-tags

# Or push separately
git push origin <branch-name>
git push origin v1.0.1
```

### Method 2: Using GitHub Actions (Recommended for CI/CD)

Use the GitHub Actions workflow to automatically create a release:

1. Go to your repository on GitHub
2. Click on **Actions** tab
3. Select **Create Release** workflow
4. Click **Run workflow**
5. Select the version bump type (major, minor, or patch)
6. Click **Run workflow**

The workflow will:
1. Bump the version in `package.json`
2. Commit the changes
3. Create and push a git tag
4. Create a GitHub Release with release notes

### Method 3: Manual Tag Push

If you manually create a tag and push it, a release will be automatically created:

```bash
# Update package.json manually
vim Assets/Matuyuhi/LudiscanApiClient/package.json

# Commit the change
git add Assets/Matuyuhi/LudiscanApiClient/package.json
git commit -m "chore: bump version to 1.0.1"

# Create and push tag
git tag -a v1.0.1 -m "Release 1.0.1"
git push origin main
git push origin v1.0.1
```

The `tag-release.yml` workflow will automatically create a GitHub Release when a tag matching `v*.*.*` is pushed.

## Release Notes

Release notes are automatically generated from commit messages since the last tag. To ensure good release notes:

- Write clear, descriptive commit messages
- Follow conventional commits format:
  - `feat:` for new features
  - `fix:` for bug fixes
  - `docs:` for documentation changes
  - `chore:` for maintenance tasks
  - `refactor:` for code refactoring

Example:
```bash
git commit -m "feat: add singleton pattern to Logger classes"
git commit -m "fix: correct timestamp calculation in FieldObjectLogger"
```

## Workflow Files

### `.github/workflows/release.yml`
Manual workflow for creating releases via GitHub UI. Allows selecting version bump type.

### `.github/workflows/tag-release.yml`
Automatic workflow triggered when a version tag is pushed. Creates a GitHub Release.

## Best Practices

1. **Test before releasing**: Ensure all tests pass and the package works correctly
2. **Update CHANGELOG**: Keep CHANGELOG.md up to date with changes
3. **Review changes**: Review all changes since the last release
4. **Semantic versioning**: Follow SemVer strictly to avoid breaking user code
5. **Document breaking changes**: Clearly document any breaking changes in release notes

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial release |

## Troubleshooting

### Script fails to update version

Check that `package.json` exists at the correct path:
```bash
ls -la Assets/Matuyuhi/LudiscanApiClient/package.json
```

### Tag already exists

If a tag already exists, delete it and recreate:
```bash
git tag -d v1.0.1
git push origin :refs/tags/v1.0.1
```

### Workflow doesn't trigger

Ensure:
1. Workflows are enabled in repository settings
2. You have write permissions
3. The tag format matches `v*.*.*` (e.g., `v1.0.0`)

## References

- [Semantic Versioning](https://semver.org/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html)
