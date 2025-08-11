using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ScrapingDataApi.Models;

namespace ScrapingDataApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMongoCollection<ProductScrapingRecord> _collection;

    public ProductsController(IConfiguration configuration)
    {
        var mongoConn = configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var mongoDb = configuration["MongoDB:Database"] ?? "ScrapingDb";
        var mongoCollection = configuration["MongoDB:Collection"] ?? "ProductScrapingRecords";
        var client = new MongoClient(mongoConn);
        var db = client.GetDatabase(mongoDb);
        _collection = db.GetCollection<ProductScrapingRecord>(mongoCollection);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _collection.Find(_ => true).ToListAsync();
        return Ok(products);
    }
}
