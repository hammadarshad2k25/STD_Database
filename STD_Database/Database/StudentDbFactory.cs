using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace STD_Database.Database
{
    public class StudentDbContextFactory : IDesignTimeDbContextFactory<StudentDB>
    {
        public StudentDB CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<StudentDB>();
            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=StudentDB;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;"
            );
            return new StudentDB(optionsBuilder.Options);
        }
    }
}
