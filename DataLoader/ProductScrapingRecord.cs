using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataLoader;

public record ProductScrapingRecord(
    [property: BsonElement("category")] string Category = "",
    [property: BsonElement("price")] string Price = "",
    [property: BsonElement("serialNumber")] string SerialNumber = "",
    [property: BsonElement("siteName")] string SiteName = "",
    [property: BsonElement("description")] string Description = "",
    [property: BsonElement("subCategory")] string SubCategory = "",
    [property: BsonElement("dateTime")] DateTime DateTime = default
);
