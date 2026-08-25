using Aiursoft.GitMirrorServer.Entities;

namespace Aiursoft.GitMirrorServer.Services;

public static class RepositoryMirrorRetryRunner
{
    public const int MaximumAttempts = 4;

    public static async Task RunAsync(
        string repositoryName,
        MirrorJobExecution jobExecution,
        MirrorRepoExecution repoExecution,
        Func<Task> mirrorRepositoryAsync,
        Action cleanupRepository,
        Action<string> log,
        Action<int, Exception> logError,
        Func<TimeSpan, Task>? delayAsync = null)
    {
        delayAsync ??= Task.Delay;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            log($"Attempt {attempt}/{MaximumAttempts} for repository {repositoryName}");

            try
            {
                await mirrorRepositoryAsync();
                repoExecution.IsSuccess = true;
                jobExecution.SuccessCount++;
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                log($"Attempt {attempt}/{MaximumAttempts} failed for repository {repositoryName}: {ex}");
                logError(attempt, ex);

                try
                {
                    cleanupRepository();
                }
                catch (Exception cleanupException)
                {
                    log($"Failed to clean local repository {repositoryName} after attempt " +
                        $"{attempt}/{MaximumAttempts}: {cleanupException}");
                    logError(attempt, cleanupException);
                }

                if (attempt < MaximumAttempts)
                {
                    var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    log($"Retrying repository {repositoryName} in {retryDelay.TotalSeconds:0} seconds");
                    await delayAsync(retryDelay);
                }
            }
        }

        var finalError = $"Failed to mirror repository {repositoryName} after {MaximumAttempts} attempts. " +
            $"Last error: {lastException!.Message}";
        log(finalError);
        repoExecution.ErrorMessage = finalError;
        repoExecution.IsSuccess = false;
        jobExecution.FailureCount++;
        jobExecution.IsSuccess = false;
    }
}
