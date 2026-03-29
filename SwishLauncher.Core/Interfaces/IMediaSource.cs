using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SwishLauncher.Core.Models;

namespace SwishLauncher.Core.Interfaces;

public interface IMediaSource
{
    string SourceName { get; }
    Task<IEnumerable<MediaEntry>> ScanAsync(CancellationToken ct = default);
}
