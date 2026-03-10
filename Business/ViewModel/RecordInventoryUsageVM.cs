namespace Business.ViewModel;

public class RecordInventoryUsageVM
{
    public Guid inventoryId { get; set; }
    public Guid? jobId { get; set; }
    public string actionType { get; set; } = "used"; // "used" or "returned"
    public decimal quantity { get; set; }
    public string? notes { get; set; }
}
