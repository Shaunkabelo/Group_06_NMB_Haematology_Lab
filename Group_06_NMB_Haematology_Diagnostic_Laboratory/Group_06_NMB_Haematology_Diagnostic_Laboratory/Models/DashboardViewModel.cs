using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Models;

public class DashboardViewModel
{
    public int TestTypeCount { get; set; }
    public int CategoryCount { get; set; }
    public int DoctorCount { get; set; }
    public int TechnicianCount { get; set; }
    public int ManagerCount { get; set; }
    public int ConsumableCount { get; set; }
    public List<Consumable> LowStockConsumables { get; set; } = new();
    public List<StockOrder> RecentOrders { get; set; } = new();
}
