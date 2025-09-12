using System.Text.Json.Serialization;

public class ZaloQuotaResponse
{
    [JsonPropertyName("data")]
    public List<ZaloQuotaItem> Data { get; set; }

    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}

public class ZaloQuotaItem
{
    [JsonPropertyName("product_type")]
    public string ProductType { get; set; }

    [JsonPropertyName("quota_type")]
    public string QuotaType { get; set; }

    [JsonPropertyName("asset_id")]
    public string AssetId { get; set; }

    [JsonPropertyName("valid_through")]
    public string ValidThrough { get; set; }

    [JsonPropertyName("auto_renew")]
    public bool AutoRenew { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("used_id")]
    public string UsedId { get; set; }
}
