namespace Business.Response;

public class InventoryUsageHistoryRM
{
    public Guid id { get; set; }
    public Guid inventoryId { get; set; }
    public Guid? jobId { get; set; }
    public string? jobTitle { get; set; }
    public string actionType { get; set; } = string.Empty;
    public decimal quantity { get; set; }
    public decimal previousQuantity { get; set; }
    public decimal newQuantity { get; set; }
    public string? notes { get; set; }
    public string? performedByUserName { get; set; }
    public DateTime createDatetime { get; set; }
}
