using System;

namespace MAAUnified.Application.Models;

public sealed record VersionUpdateCheckResult(
    string Channel,
    string CurrentVersion,
    string TargetVersion,
    string ReleaseName,
    string Summary,
    string Body,
    string? PackageName,
    Uri? PackageDownloadUrl,
    long? PackageSize,
    bool IsNewVersion,
    bool HasPackage,
    string? PreparedPackagePath = null);
