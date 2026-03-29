using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SwishLauncher.Core.Models;

namespace SwishLauncher.Core.Interfaces;

public interface IGameSource
{
    string SourceName { get; }
    Task<IEnumerable<GameEntry>> ScanAsync(CancellationToken ct = default);
}
