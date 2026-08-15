using CollisionFlow.Api.Contracts;
using CollisionFlow.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CollisionFlow.Api.Controllers;

/// <summary>Operational state of the running application.</summary>
/// <remarks>
/// This controller depends on an infrastructure abstraction, which the others deliberately
/// do not. That is the point of the endpoint: it reports on infrastructure, so there is no
/// domain concept for it to be expressed in terms of.
/// </remarks>
[ApiController]
[Route("api/system")]
[Produces("application/json")]
public sealed class SystemController : ControllerBase
{
    private readonly IDataSourceStatus _dataSource;

    public SystemController(IDataSourceStatus dataSource)
    {
        _dataSource = dataSource;
    }

    /// <summary>Reports which store is currently serving repair orders.</summary>
    [HttpGet("status")]
    [ProducesResponseType<SystemStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemStatusResponse> GetStatus() =>
        Ok(new SystemStatusResponse
        {
            DataSource = _dataSource.Current.ToString(),
            IsDegraded = _dataSource.IsDegraded,
            Since = _dataSource.Since,
            Message = _dataSource.IsDegraded
                ? "The database is unavailable or waking up. You are seeing sample data, and changes will not be saved."
                : null,
        });
}
