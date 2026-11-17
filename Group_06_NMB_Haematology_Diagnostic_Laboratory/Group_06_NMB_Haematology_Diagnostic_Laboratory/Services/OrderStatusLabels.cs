namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
public static class OrderStatusLabels
{
    public static string Display(string? status) => status?.Trim().ToLowerInvariant() switch {
        "complete" or "received" => "Received",
        "partially complete" or "partially received" => "Partially Received",
        "ordered" => "Ordered", "cancelled" => "Cancelled", _ => status ?? "Unknown"
    };
}