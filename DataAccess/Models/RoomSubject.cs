using DataAccess.GeneralEntities;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class RoomSubject : BaseEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public virtual ICollection<RoomSubjectComment> Comments { get; set; }
    }

    public class RoomSubjectComment : BaseEntity
    {
        [ForeignKey("RoomSubject")]
        public string SubjectID { get; set; }
        public string Comment { get; set; }
        public string Sender { get; set; }

        public virtual RoomSubject RoomSubject { get; set; }

    }
}
