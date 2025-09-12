using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SigmaNotificationBackend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SigmaNotificationBackend.Data
{
    public class DashboardDbContext : DbContext
    {
        public DashboardDbContext(DbContextOptions<DashboardDbContext> options) : base(options)
        {
        }

        public DbSet<DashBoardSummaryDto> DashboardSummaries { get; set; }

        public DbSet<SVN_Messages> SVN_Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Vì DashBoardSummaryDto không có khóa chính nên cần khai báo HasNoKey
            modelBuilder.Entity<DashBoardSummaryDto>().HasNoKey();
        }







    }
}


