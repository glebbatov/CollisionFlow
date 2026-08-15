using CollisionFlow.Infrastructure.Data;
using CollisionFlow.Infrastructure.Workflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CollisionFlow.Infrastructure;

/// <summary>
/// Applies the schema and adopts the database's workflow, retrying until it succeeds.
/// </summary>
/// <remarks>
/// <para>
/// The retry is the entire point. A serverless database that has auto-paused takes up to a
/// minute to resume, and a brand new deployment meets exactly that: the first connection of
/// the application's life arrives at a database that is asleep. A single attempt would fail,
/// log, and give up - leaving the schema unapplied forever and the application permanently
/// on its fallback, with no path back other than a restart nobody knows to perform.
/// </para>
/// <para>
/// It runs as a <see cref="BackgroundService"/> rather than in <c>StartAsync</c> so that
/// waiting for the database never delays the web server from accepting requests. Those early
/// requests are served from the fallback, with the banner explaining why - which is a better
/// first impression than a five-minute startup or a failed health probe.
/// </para>
/// <para>
/// Nothing here can prevent the application from starting. A board readable from cached rules
/// beats a deployment that will not boot.
/// </para>
/// </remarks>
public sealed class DatabaseInitializer : BackgroundService
{
    /// <summary>
    /// Roughly five minutes in total, front-loaded. Comfortably longer than a serverless
    /// resume, short enough that a genuinely bad connection string is reported rather than
    /// retried forever.
    /// </summary>
    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(45),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60),
    ];

    private readonly DatabaseOptions _options;
    private readonly SqlScriptRunner _scriptRunner;
    private readonly SqlWorkflowLoader _workflowLoader;
    private readonly MutableStatusTransitionPolicy _policy;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IOptions<DatabaseOptions> options,
        SqlScriptRunner scriptRunner,
        SqlWorkflowLoader workflowLoader,
        MutableStatusTransitionPolicy policy,
        ILogger<DatabaseInitializer> logger)
    {
        _options = options.Value;
        _scriptRunner = scriptRunner;
        _workflowLoader = workflowLoader;
        _policy = policy;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 0; attempt < Backoff.Length; attempt++)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                if (_options.InitializeSchema)
                {
                    await _scriptRunner.ApplyAsync(stoppingToken);
                }

                _policy.ReplaceWith(await _workflowLoader.LoadAsync(stoppingToken));

                _logger.LogInformation(
                    "Database ready. Workflow adopted from dbo.StatusTransition.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                var delay = Backoff[attempt];

                _logger.LogWarning(
                    ex,
                    "Database not ready (attempt {Attempt} of {Total}). Retrying in {Delay}s.",
                    attempt + 1,
                    Backoff.Length,
                    delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        _logger.LogError(
            "Database could not be initialized. Continuing on the in-memory repository and the " +
            "workflow compiled into the domain. Check the connection string and firewall rules.");
    }
}
