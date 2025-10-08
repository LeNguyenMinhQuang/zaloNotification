using Microsoft.EntityFrameworkCore;
using SigmaNotificationBackend.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<SVN_Users> SVN_Users { get; set; }
    public DbSet<SVN_Departments> SVN_Departments { get; set; }
    public DbSet<SVN_Messages> SVN_Messages { get; set; }
    public DbSet<SVN_Logs> SVN_Logs { get; set; }

    public DbSet<ZaloToken> ZaloTokens { get; set; }

    public DbSet<Follower> Followers { get; set; }





    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SVN_Users>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.id_department);
    }


}
