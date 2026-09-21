using BatchCraft.App.Models;

namespace BatchCraft.App.Services;

public interface IBatchService
{
    Task<int> RunAsync(BatchJob job, string workingDirectory, Action<string> output, CancellationToken cancellationToken);
}
