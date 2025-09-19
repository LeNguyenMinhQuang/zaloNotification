// using System;
// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;
// using Microsoft.EntityFrameworkCore;

// namespace SigmaNotificationBackend.Models
// {
//     [Table("SVN_daily_target")]
//     // [Keyless] 

//     public class DailyTarget
//     {
//         public string Operation { get; set; }

//         public decimal? Daily_plan { get; set; }

//         public decimal? UPH { get; set; }

//         public decimal? UPPH { get; set; }

//         public decimal? Labor { get; set; }

//         public decimal? Defect { get; set; }

//         public string? Date_time { get; set; }

//         public decimal? Workingtime { get; set; }

//         public decimal? Total_Qty { get; set; }

//         public double? MaxLabor { get; set; }

//         public string? WC { get; set; }

//         public decimal? Current_UPH { get; set; }

//         public decimal? Current_UPPH { get; set; }

//         public int? Total_NG_Qty { get; set; }
//     }
// }

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SigmaNotificationBackend.Models
{
    [Table("SVN_daily_target")]
    [Keyless] // Bảng/VIEW không có khóa chính

    public class DailyTarget
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
