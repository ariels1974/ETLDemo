using System.Text.Json.Serialization;

namespace SiteTransformers.Transformers;

public partial class ALMTransformer
{
    public class GraphQLResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("errors")]
        public GraphQLError[] Errors { get; set; }
    }

    public class GraphQLError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("locations")]
        public GraphQLLocation[] Locations { get; set; }

        [JsonPropertyName("path")]
        public string[] Path { get; set; }
    }
    public class GraphQLLocation
    {
        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("column")]
        public int Column { get; set; }
    }
    // Main data structure
    public class CategoryProductData
    {
        [JsonPropertyName("products")]
        public Products Products { get; set; }
    }

    public class Products
    {
        [JsonPropertyName("items")]
        public ProductItem[] Items { get; set; }

        [JsonPropertyName("page_info")]
        public PageInfo PageInfo { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }

    public class ProductItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("product_brand")]
        public ProductBrand ProductBrand { get; set; }

        [JsonPropertyName("price_range")]
        public PriceRange PriceRange { get; set; }

        [JsonPropertyName("sku")]
        public string Sku { get; set; }

        [JsonPropertyName("stock_status")]
        public string StockStatus { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }

    }

    public class ProductBrand
    {
        [JsonPropertyName("brand_image_url")]
        public string BrandImageUrl { get; set; }

        [JsonPropertyName("brand_link")]
        public string BrandLink { get; set; }

        [JsonPropertyName("item_brand")]
        public string ItemBrand { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }

    public class PriceRange
    {
        [JsonPropertyName("maximum_price")]
        public ProductPrice MaximumPrice { get; set; }

        [JsonPropertyName("minimum_price")]
        public ProductPrice MinimumPrice { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }

    public class ProductPrice
    {
        [JsonPropertyName("final_price")]
        public Money FinalPrice { get; set; }

        [JsonPropertyName("regular_price")]
        public Money RegularPrice { get; set; }

        [JsonPropertyName("discount")]
        public ProductDiscount Discount { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }

    public class Money
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }

    public class ProductDiscount
    {
        [JsonPropertyName("amount_off")]
        public decimal AmountOff { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }

    public class PageInfo
    {
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }
}