# Publishing Guide

This document explains how to build and publish the Cocoar.Capabilities package.

## Prerequisites

- .NET 9.0 SDK or later
- Git (for GitVersion)

## Building

### Development Build
```bash
dotnet build
```

### Release Build
```bash
dotnet build -c Release
```

### Running Tests
```bash
dotnet test
```

## Packaging

### Create Package
```bash
dotnet pack -c Release
```

This will create the package in `src/Cocoar.Capabilities/bin/Release/`.

### Package Contents

The NuGet package includes:
- **Main Library**: `Cocoar.Capabilities.dll` (.NET 9.0)
- **XML Documentation**: For IntelliSense support
- **Symbols**: Debug symbols for better debugging experience
- **Documentation**: README.md, LICENSE, and CHANGELOG.md
- **Source Link**: Links to source code for debugging

## Publishing

### To NuGet.org (Production)

1. **Get API Key**: Obtain API key from [nuget.org](https://www.nuget.org/account/apikeys)

2. **Push Package**:
   ```bash
   dotnet nuget push src/Cocoar.Capabilities/bin/Release/Cocoar.Capabilities.*.nupkg --api-key [YOUR-API-KEY] --source https://api.nuget.org/v3/index.json
   ```

### To GitHub Packages (Private)

1. **Create GitHub Token**: With `write:packages` permission

2. **Configure Source** (if not already in nuget.config):
   ```bash
   dotnet nuget add source --username cocoar-dev --password [GITHUB-TOKEN] --store-password-in-clear-text --name github "https://nuget.pkg.github.com/cocoar-dev/index.json"
   ```

3. **Push Package**:
   ```bash
   dotnet nuget push src/Cocoar.Capabilities/bin/Release/Cocoar.Capabilities.*.nupkg --source github
   ```

## Versioning

This project uses **GitVersion** for automatic semantic versioning:

### Version Strategy
- **main branch**: Patch versions (1.0.x)  
- **develop branch**: Minor pre-release versions (1.1.0-beta.x)
- **feature branches**: Feature pre-release versions (1.0.0-feature.x)

### Manual Version Override

To override version manually, create `GitVersion.yml` configuration or use:

```bash
dotnet pack -p:Version=1.2.3
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Publish

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Required for GitVersion
        
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build -c Release --no-restore
      
    - name: Test
      run: dotnet test -c Release --no-build
      
    - name: Pack
      run: dotnet pack -c Release --no-build
      
    - name: Publish to NuGet (main branch only)
      if: github.ref == 'refs/heads/main'
      run: dotnet nuget push src/Cocoar.Capabilities/bin/Release/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

## Quality Checks

Before publishing, ensure:

- ✅ All tests pass (`dotnet test`)
- ✅ No build warnings in Release mode
- ✅ Package builds successfully (`dotnet pack`)
- ✅ Documentation is up to date
- ✅ CHANGELOG.md is updated
- ✅ Version number is appropriate

## Package Validation

Use NuGet Package Explorer or dotnet commands to validate:

```bash
# List package contents
dotnet nuget locals all --list

# Verify package dependencies
dotnet list package --include-transitive
```

## Troubleshooting

### Common Issues

1. **GitVersion Error**: Ensure git repository is initialized and has commits
2. **Missing Dependencies**: Run `dotnet restore` first  
3. **Permission Denied**: Check API keys and package source permissions
4. **Version Conflicts**: Clear NuGet cache: `dotnet nuget locals all --clear`