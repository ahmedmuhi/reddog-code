using Microsoft.AspNetCore.Mvc;
using RedDog.VirtualWorker.Services;

namespace RedDog.VirtualWorker.Controllers;

[ApiController]
[Route("")]
public class VirtualWorkerController(IVirtualWorkerService workerService) : ControllerBase
{
    private readonly IVirtualWorkerService _workerService = workerService ?? throw new ArgumentNullException(nameof(workerService));

    [HttpPost("orders")]
    public async Task<IActionResult> ProcessOrders(CancellationToken cancellationToken)
    {
        await _workerService.RunOnceAsync(cancellationToken);
        return Ok(new { Message = "Virtual worker executed." });
    }
}
