using System.ComponentModel.DataAnnotations;

namespace DataAccess.GeneralEntities
{
    public class BaseEntity
    {
        [Key]
        public string Id { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }

        public BaseEntity()
        {
                Id = Guid.NewGuid().ToString();
                CreatedAt = DateTime.UtcNow.ToString("o");
                UpdatedAt = DateTime.UtcNow.ToString("o");
        }
    }
}
