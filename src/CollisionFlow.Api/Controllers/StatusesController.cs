using CollisionFlow.Api.Contracts;
using CollisionFlow.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CollisionFlow.Api.Controllers;

/// <summary>The approved statuses and the moves the workflow permits between them.</summary>
/// <remarks>
/// The client builds its status pickers from this endpoint rather than shipping its
/// own copy of the rules. That is what keeps an illegal option from ever being
/// rendered, without the rules living in two places.
/// </remarks>
[ApiController]
[Route("api/statuses")]
[Produces("application/json")]
public sealed class StatusesController : ControllerBase
{
    private readonly IStatusTransitionPolicy _policy;

    public StatusesController(IStatusTransitionPolicy policy)
    {
        _policy = policy;
    }

    /// <summary>Lists the approved statuses in workflow order.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RepairStatusResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<RepairStatusResponse>> GetAll() =>
        Ok(RepairStatusInfo.InWorkflowOrder
            .Select(s => s.ToResponse(_policy))
            .ToArray());
}
