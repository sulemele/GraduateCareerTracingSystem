using DataAccess.GeneralEntities;
using System.ComponentModel.DataAnnotations.Schema;
namespace DataAccess.Models
{
    public class Department : BaseEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }

        [ForeignKey("Programme")]
        public string ProgrammeId { get; set; }
        public virtual Programme Programme { get; set; }
    }
}
