using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SigmaNotificationBackend.Models;

namespace SigmaNotificationBackend.Data
{
    public class DashboardDbContext : DbContext
    {
        public DashboardDbContext(DbContextOptions<DashboardDbContext> options) : base(options)
        {
        }

        public DbSet<DashBoardSummaryDto> DashboardSummaries { get; set; }

        public DbSet<SVN_Messages> SVN_Messages { get; set; }

        public DbSet<DailyTarget> DailyTargets { get; set; }

        public DbSet<SVN_target> SVN_Targets { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Vì DashBoardSummaryDto không có khóa chính nên cần khai báo HasNoKey
            modelBuilder.Entity<DashBoardSummaryDto>().HasNoKey();
            modelBuilder.Entity<DailyTarget>().HasNoKey();
            modelBuilder.Entity<SVN_target>().HasNoKey();

        }







    }
}


