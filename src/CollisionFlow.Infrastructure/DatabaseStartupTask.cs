using CollisionFlow.Infrastructure.Data;
using CollisionFlow.Infrastructure.Workflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CollisionFlow.Infrastructure;

/// <summary>
/// Applies the schema and adopts the database's workflow at startup.
/// </summary>
/// <remarks>
/// Nothing here is allowed to prevent the application from starting. A collision center's
/// board being readable from cached rules is better than a deployment that will not boot
/// because a serverless database was still waking up. Failures are logged loudly and the
/// application continues on the compiled-in workflow.
/// </remarks>
public sealed class DatabaseStartupTask : IHostedService
{
    private readonly DatabaseOptions _options;
    private readonly SqlScriptRunner _scriptRunner;
    private readonly SqlWorkflowLoader _workflowLoader;
    private readonly MutableStatusTransitionPolicy _policy;
    private readonly ILogger<DatabaseStartupTask> _logger;

    public DatabaseStartupTask(
        IOptions<DatabaseOptions> options,
        SqlScriptRunner scriptRunner,
        SqlWorkflowLoader workflowLoader,
        MutableStatusTransitionPolicy policy,
        ILogger<DatabaseStartupTask> logger)
    {
        _options = options.Value;
        _scriptRunner = scriptRunner;
        _workflowLoader = workflowLoader;
        _policy = policy;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogInformation(
                "No connection string configured; running on the in-memory repository.");
            return;
        }

        try
        {
            if (_options.InitializeSchema)
            {
                await _scriptRunner.ApplyAsync(cancellationToken);
            }

            _policy.ReplaceWith(await _workflowLoader.LoadAsync(cancellationToken));

            _logger.LogInformation("Workflow adopted from the database.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Database startup failed. Continuing on the workflow compiled into the domain.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
