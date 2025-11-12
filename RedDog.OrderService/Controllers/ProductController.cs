using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RedDog.OrderService.Models;

namespace RedDog.OrderService.Controllers;

[ApiController]
[EnableCors]
[Route("[controller]")]
public class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> _logger;

    public ProductController(ILogger<ProductController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<List<Product>> Get()
    {
        _logger.LogInformation("Retrieving all products");
        var products = await Product.GetAllAsync();
        _logger.LogInformation("Retrieved {ProductCount} products", products.Count);
        return products;
    }
}
