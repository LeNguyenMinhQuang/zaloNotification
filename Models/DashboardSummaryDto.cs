using Microsoft.EntityFrameworkCore;

public class DashBoardSummaryDto
{
    public string Operation { get; set; }

    [Precision(10, 2)]
    public decimal? Daily_plan { get; set; }

    [Precision(10, 2)]
    public decimal? UPH { get; set; }

    [Precision(10, 2)]
    public decimal? UPPH { get; set; }

    [Precision(10, 2)]
    public decimal? Labor { get; set; }

    [Precision(10, 2)]
    public decimal? Defect { get; set; }

    public string? Date_time { get; set; }

    [Precision(10, 2)]
    public decimal? Workingtime { get; set; }

    [Precision(10, 3)]
    public decimal? Total_Qty { get; set; }

    public double? MaxLabor { get; set; }

    public string? WC { get; set; }

    [Precision(10, 3)]
    public decimal? Current_UPH { get; set; }

    [Precision(10, 3)]
    public decimal? Current_UPPH { get; set; }

    public int? Total_NG_Qty { get; set; }
}