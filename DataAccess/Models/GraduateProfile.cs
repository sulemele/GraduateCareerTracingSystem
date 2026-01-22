using DataAccess.GeneralEntities;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class GraduateProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string MatricNumber { get; set; }
        [ForeignKey("Department")]
        public string DepartmentId { get; set; }
        public int YearOfGraduation { get; set; }

        public string? EmploymentStatus { get; set; }
        public string? CurrentEmployer { get; set; }
        public string? JobTitle { get; set; }
        public string? Location { get; set; }
        public string? Skills { get; set; }

        //Bio
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string? PhotoUrl { get; set; }
        public string? HighestAcademicQualification { get; set; }

        //Navigation Properties
        public Department? Department { get; set; }
    }
}
