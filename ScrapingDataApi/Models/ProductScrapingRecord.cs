using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrapingDataApi.Models;

public record ProductScrapingRecord(
    [property: BsonId][property: BsonRepresentation(BsonType.ObjectId)] string? Id = null,
    [property: BsonElement("category")] string Category = "",
    [property: BsonElement("price")] string Price = "",
    [property: BsonElement("serialNumber")] string SerialNumber = "",
    [property: BsonElement("siteName")] string SiteName = "",
    [property: BsonElement("description")] string Description = "",
    [property: BsonElement("subCategory")] string SubCategory = "",
    [property: BsonElement("dateTime")] DateTime DateTime = default
);
