// SpaceOS.Kernel.Application/Common/TaskExtensions.cs
using Microsoft.Extensions.Logging;

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Extension methods for fire-and-forget task execution with guaranteed error logging.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Executes a <see cref="Task"/> in a fire-and-forget manner while guaranteeing
    /// that any exception is logged. Prevents silent audit failures (BE-P2-03).
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="logger">Logger used if the task faults.</param>
    /// <param name="context">Context string for the log entry (e.g. handler + operation name).</param>
    public static void FireAndForget(this Task task, ILogger logger, string context)
        => task.ContinueWith(
            t => logger.LogError(t.Exception,
                "Fire-and-forget task failed. Context: {Context}", context),
            TaskContinuationOptions.OnlyOnFaulted |
            TaskContinuationOptions.ExecuteSynchronously);
}
