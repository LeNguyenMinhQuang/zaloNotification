using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SigmaNotificationBackend.Models
{
    public class DailyTarget
    {
        public string Operation { get; set; }

        public decimal? Daily_plan { get; set; }

        public decimal? UPH { get; set; }

        public decimal? UPPH { get; set; }

        public decimal? Labor { get; set; }

        public decimal? Defect { get; set; }

        public string? Date_time { get; set; }

        public decimal? Workingtime { get; set; }

        public decimal? Total_Qty { get; set; }

        public double? MaxLabor { get; set; }

        public string? WC { get; set; }

        public decimal? Current_UPH { get; set; }

        public decimal? Current_UPPH { get; set; }

        public int? Total_NG_Qty { get; set; }
    }
}

