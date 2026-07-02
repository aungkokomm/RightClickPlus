using RightClick.Core.Models;

namespace RightClick.Core.Services;

/// <summary>
/// Loads and persists <see cref="AppSettings"/> to a portable JSON file.
/// </summary>
public interface ISettingsService
{
    /// <summary>The current in-memory settings.</summary>
    AppSettings Current { get; }

    /// <summary>Loads settings from disk (creating defaults if none exist).</summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the current settings to disk.</summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
