using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SigmaNotificationBackend.Models
{
    [Table("SVN_target")]
    [Keyless] // Bảng/VIEW không có khóa chính

    public class SVN_target
    {
        // nvarchar(100), Allow Nulls = true
        public string? Operation { get; set; }

        // decimal(18,2), Allow Nulls = true
        public decimal? Daily_plan { get; set; }

        // decimal(18,2), Allow Nulls = true
        public decimal? UPH { get; set; }

        // decimal(18,2), Allow Nulls = true
        public decimal? UPPH { get; set; }

        // int, Allow Nulls = true
        public int? Labor { get; set; }

        // decimal(18,2), Allow Nulls = true
        public decimal? Defect { get; set; }

        // varchar(8), Allow Nulls = true
        public string? Date_time { get; set; }

        // decimal(18,2), Allow Nulls = true
        public decimal? Workingtime { get; set; }

        // decimal(18,2), Allow Nulls = true
        public decimal? Total_Qty { get; set; }

        // int, Allow Nulls = true
        public int? MaxLabor { get; set; }

        // nvarchar(50), Allow Nulls = true
        public string? WC { get; set; }

        // decimal(18,2), Allow Nulls = true
        public decimal? Current_UPH { get; set; }

        // decimal(18,2), Allow Nulls = true
        public decimal? Current_UPPH { get; set; }

        // decimal(18,2), Allow Nulls = false
        public decimal? Total_NG_Qty { get; set; }
    }
}
