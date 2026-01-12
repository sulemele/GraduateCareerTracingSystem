using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.DataBase
{
    public class DatabaseEntity : DbContext
    {
        public DatabaseEntity(DbContextOptions<DatabaseEntity> options)
       : base(options) { }

        public DbSet<GraduateProfile> GraduateProfiles { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Programme> Programmes { get; set; }
    }
}
