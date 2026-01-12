using DataAccess.GeneralEntities;

namespace DataAccess.Models
{
    public class Programme : BaseEntity
    {
        public string Title { get; set; }
        public string Description { get; set; } 

        public ICollection<Department> Departments { get; set; }

    }
}
