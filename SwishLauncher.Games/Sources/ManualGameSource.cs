using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;

namespace SwishLauncher.Games.Sources;

/// <summary>
/// Placeholder source for manually added games.
/// Returns nothing on scan — manual entries are added by the user
/// via the Add Game UI and already exist in the database.
/// </summary>
public class ManualGameSource : IGameSource
{
    public string SourceName => "Manual";

    public Task<IEnumerable<GameEntry>> ScanAsync(CancellationToken ct = default)
        => Task.FromResult(Enumerable.Empty<GameEntry>());
}
