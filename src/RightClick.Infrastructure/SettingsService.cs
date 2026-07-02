using System.Text.Json;
using Microsoft.Extensions.Logging;
using RightClick.Core.Models;
using RightClick.Core.Services;

namespace RightClick.Infrastructure;

/// <summary>
/// JSON-backed settings store written to the portable data folder.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly ILogger<SettingsService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SettingsService(ILogger<SettingsService> logger) => _logger = logger;

    public AppSettings Current { get; private set; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = PortablePaths.SettingsFile;
            if (!File.Exists(path))
            {
                _logger.LogInformation("No settings file found; using defaults at {Path}.", path);
                Current = new AppSettings();
                return;
            }

            await using var stream = File.OpenRead(path);
            Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
                          .ConfigureAwait(false)
                      ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings; falling back to defaults.");
            Current = new AppSettings();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = PortablePaths.SettingsFile;
            var tmp = path + ".tmp";
            await using (var stream = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(stream, Current, JsonOptions, cancellationToken).ConfigureAwait(false);
            }
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }
}
