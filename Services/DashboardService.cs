// Services/DashboardService.cs
using SigmaNotificationBackend.Models;
using SigmaNotificationBackend.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

public class DashboardService
{
    private readonly DashboardDbContext _context;
    private readonly AppDbContext _dbcontext;

    public DashboardService(DashboardDbContext dashcontext, AppDbContext appDbContext)
    {
        _dbcontext = appDbContext;
        _context = dashcontext;
    }




    // 🔹 1. Gọi stored procedure để lấy dữ liệu dashboard theo ngày
    public async Task<List<DashBoardSummaryDto>> GetDashboardDataByDate(string? date = null)
    {
        var formattedDate = string.IsNullOrEmpty(date)
            ? DateTime.Now.ToString("yyyyMMdd")
            : date;

        return await _context.DashboardSummaries
            .FromSqlInterpolated($"EXEC SVN_Pro_CalTarget_Viindoo @date_time = {formattedDate}")
            .ToListAsync();
    }




    public List<SVN_Messages> GenerateWarningsFromDashboardDto(DashBoardSummaryDto dto)
    {
        var warnings = new List<SVN_Messages>();
        DateTime now = DateTime.Now;


        var contentParts = new List<string>();
        string? operation = dto.Operation;

        // Từng cảnh báo sẽ có màu riêng
        var detailParts = new List<string>();


        // 🔒 H.Plan
        if ((dto.Daily_plan ?? 0) > 0)
        {
            double planRate = ((double)(dto.Total_Qty ?? 0) / (double)(dto.Daily_plan ?? 1)) * 100;
            if (planRate < 75)
                detailParts.Add($"[bg-danger] H.Plan : {planRate:F2}%");
            else if (planRate <= 92)
                detailParts.Add($"[bg-warning] H.Plan : {planRate:F2}%");
        }

        // 🔒 UPH
        if ((dto.UPH ?? 0) > 0)
        {
            double uphRate = ((double)(dto.Current_UPH ?? 0) / (double)(dto.UPH ?? 1)) * 100;
            if (uphRate < 75)
                detailParts.Add($"[bg-danger] UPH : {uphRate:F2}%");
            else if (uphRate <= 92)
                detailParts.Add($"[bg-warning] UPH : {uphRate:F2}%");
        }

        // 🔒 UPPH
        if ((dto.UPPH ?? 0) > 0)
        {
            double upphRate = ((double)(dto.Current_UPPH ?? 0) / (double)(dto.UPPH ?? 1)) * 100;
            if (upphRate < 75)
                detailParts.Add($"[bg-danger] UPPH : {upphRate:F2}%");
            else if (upphRate <= 92)
                detailParts.Add($"[bg-warning] UPPH : {upphRate:F2}%");
        }

        // 🔒 Labor
        if ((dto.Labor ?? 0) > 0)
        {
            double laborRate = ((double)(dto.MaxLabor ?? 0) / (double)(dto.Labor ?? 1)) * 100;
            if (laborRate < 75)
                detailParts.Add($"[bg-danger] Labor : {laborRate:F2}%");
            else if (laborRate <= 92)
                detailParts.Add($"[bg-warning] Labor : {laborRate:F2}%");
        }
        // 🔒 Defect
        if ((dto.Total_Qty ?? 0) > 0 && (dto.Total_NG_Qty ?? 0) > 0 && (dto.Defect ?? 0) > 0)
        {
            double actualDefectTotal = ((double)(dto.Total_NG_Qty ?? 0) / (double)(dto.Total_Qty ?? 1));
            double defectTarget = (double)(dto.Defect ?? 0);
            double defectRate = (double)((actualDefectTotal) / (defectTarget)) * 100;

            if (defectRate > 100)
                detailParts.Add($"[bg-danger] Defect : {defectRate:F0}%");
            else if (80 <= defectRate && defectRate < 100)
                detailParts.Add($"[bg-warning] Defect : {defectRate:F0}%");
        }

        string typeMessage = "";
        if (detailParts.Any(p => p.StartsWith("[bg-danger]")))
            typeMessage = "bg-danger";
        else if (detailParts.Any(p => p.StartsWith("[bg-warning]")))
            typeMessage = "bg-warning";
        else
            typeMessage = "";


        if (detailParts.Count > 0)
        {
            warnings.Add(new SVN_Messages
            {
                operation = operation,
                content = string.Join(" ; ", detailParts),
                type_message = typeMessage,
                create_at = now,
                UserId = "",
                isSent = 0,

            });
        }

        return warnings;
    }


    public async Task<List<SVN_Messages>> GenerateAndSaveWarningsAsync(List<DashBoardSummaryDto> dtos)
    {
        var allMessages = new List<SVN_Messages>();

        foreach (var dto in dtos)
        {
            var warnings = GenerateWarningsFromDashboardDto(dto);

            var targetDepartments = DetermineTargetDepartments(warnings);
            string toDepartmentStr = string.Join(";", targetDepartments);

            foreach (var deptId in targetDepartments)
            {
                foreach (var w in warnings)
                {
                    allMessages.Add(new SVN_Messages
                    {
                        operation = w.operation,
                        content = w.content,
                        type_message = w.type_message,
                        create_at = w.create_at,
                        to_department = deptId,
                        UserId = "",
                        isSent = 0,
                    });
                }
            }
        }

        await SaveWarningsToDb(allMessages);
        return allMessages;
    }

    private List<int> DetermineTargetDepartments(List<SVN_Messages> warnings)
    {
        var departments = new HashSet<int>();

        foreach (var w in warnings)
        {
            var content = w.content.ToLower();

            if (content.Contains("h.plan") || content.Contains("uph") || content.Contains("upph") || content.Contains("labor"))
            {
                departments.Add(1);
                departments.Add(2);
                departments.Add(3);
            }

            if (content.Contains("defect"))  // ← BỎ "else", dùng "if" riêng
            {
                departments.Add(1);
                departments.Add(5);
            }
        }

        return departments.ToList();
    }





    // 🔹 3. Hàm lưu các cảnh báo xuống database
    public async Task SaveWarningsToDb(List<SVN_Messages> warnings)
    {
        if (warnings.Any())
        {
            _dbcontext.SVN_Messages.AddRange(warnings);
            await _dbcontext.SaveChangesAsync();
        }
    }

}

