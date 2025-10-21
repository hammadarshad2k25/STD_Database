using Microsoft.EntityFrameworkCore;
using STD_Database.Model;

namespace STD_Database.Database
{
    public class StudentDB:DbContext
    {
        public StudentDB(DbContextOptions<StudentDB> options) : base(options) { }
        public DbSet<StudentMD> Students { get; set; }
        public DbSet<CoueseMD> Courses { get; set; }
        public DbSet<UserModel> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<StudentMD>().HasMany(c => c.Courses).WithMany(s => s.Students).UsingEntity(j => j.ToTable("StudentCourses"));
        }
    }
}
