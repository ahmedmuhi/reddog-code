using Microsoft.AspNetCore.Mvc;
using RedDog.LoyaltyService.Models;
using RedDog.LoyaltyService.Services;

namespace RedDog.LoyaltyService.Controllers;

[ApiController]
[Route("[controller]")]
public class LoyaltyController(ILoyaltyStateService loyaltyStateService, ILogger<LoyaltyController> logger) : ControllerBase
{
    private readonly ILoyaltyStateService _loyaltyStateService = loyaltyStateService ?? throw new ArgumentNullException(nameof(loyaltyStateService));
    private readonly ILogger<LoyaltyController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpPost("orders")]
    public async Task<IActionResult> UpdateLoyalty(OrderSummary orderSummary, CancellationToken cancellationToken)
    {
        if (orderSummary is null)
        {
            return BadRequest("OrderSummary cannot be null.");
        }

        var summary = await _loyaltyStateService.UpdateAsync(orderSummary, cancellationToken);
        _logger.LogInformation("Updated loyalty points for {LoyaltyId}. Points earned: {PointsEarned}, total: {PointTotal}",
            orderSummary.LoyaltyId,
            summary.PointsEarned,
            summary.PointTotal);

        return Ok(summary);
    }
}
