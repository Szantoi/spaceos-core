---
name: nuget-manager
description: 'Manage NuGet packages in .NET projects/solutions. Use this skill when adding, removing, or updating NuGet package versions. It enforces using `dotnet` CLI for package management and provides strict procedures for direct file edits only when updating versions.'
---

# NuGet Manager

Ensures consistent and safe management of NuGet packages across .NET projects.

## Core Rules
1. **NEVER** edit files to **add** or **remove** packages. Use `dotnet add/remove package`.
2. **DIRECT EDITING** is ONLY for **changing versions**.
3. **MANDATORY WORKFLOW** for updates: Verify version -> Identify file -> Apply -> `dotnet restore`.

## Workflows
- **Add**: `dotnet add <PROJECT> package <NAME>`
- **Remove**: `dotnet remove <PROJECT> package <NAME>`
- **Update**: Verify existence with `dotnet package search --format json`, edit file, run `dotnet restore`.
