using CollisionFlow.Api.Contracts;
using CollisionFlow.Domain;
using CollisionFlow.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CollisionFlow.Api.Controllers;

/// <summary>Read repair orders and move them through the workflow.</summary>
[ApiController]
[Route("api/repair-jobs")]
[Produces("application/json")]
public sealed class RepairJobsController : ControllerBase
{
    private readonly IRepairJobRepository _repository;
    private readonly IStatusTransitionPolicy _policy;

    public RepairJobsController(IRepairJobRepository repository, IStatusTransitionPolicy policy)
    {
        _repository = repository;
        _policy = policy;
    }

    /// <summary>Lists every repair order, most recently updated first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RepairJobResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RepairJobResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var jobs = await _repository.GetAllAsync(cancellationToken);

        return Ok(jobs.Select(j => j.ToResponse(_policy)).ToArray());
    }

    /// <summary>Fetches a single repair order.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType<RepairJobResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepairJobResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(id, cancellationToken);

        return job is null
            ? NotFoundProblem(id)
            : Ok(job.ToResponse(_policy));
    }

    /// <summary>
    /// Moves a repair order to a new status.
    /// </summary>
    /// <remarks>
    /// Modeled as a PUT against the job's <c>status</c> sub-resource rather than an
    /// RPC-style "advance" call. The status is a thing that has a value, so setting
    /// it is naturally idempotent: sending the status a job is already in succeeds
    /// and changes nothing, which means a retried request after a dropped connection
    /// is safe.
    /// </remarks>
    /// <response code="200">The repair order, with its new status and newly available transitions.</response>
    /// <response code="400">The body was missing, or the status was not a recognized name.</response>
    /// <response code="404">No repair order with that id.</response>
    /// <response code="422">The status exists, but the workflow does not allow this move.</response>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType<RepairJobResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RepairJobResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        // [ApiController] runs model validation before this point, so [Required]
        // has already guaranteed a value here.
        var requested = request.Status!.Value;

        var job = await _repository.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFoundProblem(id);
        }

        if (job.Status != requested && !_policy.IsAllowed(job.Status, requested))
        {
            return InvalidTransitionProblem(job.Status, requested);
        }

        var updated = await _repository.UpdateStatusAsync(id, requested, cancellationToken);

        return updated is null
            ? NotFoundProblem(id)
            : Ok(updated.ToResponse(_policy));
    }

    private ActionResult NotFoundProblem(Guid id) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Repair order not found.",
            detail: $"No repair order exists with id '{id}'.");

    /// <summary>
    /// Builds a 422 that tells the caller what they <i>can</i> do next.
    /// </summary>
    /// <remarks>
    /// An error that only says "no" forces the client to guess. Returning the legal
    /// transitions alongside the rejection means a client can recover without
    /// hard-coding a copy of the workflow.
    /// </remarks>
    private ActionResult InvalidTransitionProblem(RepairStatus current, RepairStatus requested)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Invalid status transition.",
            Detail = $"A repair order in '{RepairStatusInfo.DisplayName(current)}' " +
                     $"cannot move to '{RepairStatusInfo.DisplayName(requested)}'.",
            Instance = HttpContext.Request.Path,
        };

        problem.Extensions["currentStatus"] = current.ToString();
        problem.Extensions["requestedStatus"] = requested.ToString();
        problem.Extensions["allowedTransitions"] = _policy
            .AllowedNextFrom(current)
            .Select(s => s.ToString())
            .ToArray();
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return new ObjectResult(problem) { StatusCode = StatusCodes.Status422UnprocessableEntity };
    }
}
