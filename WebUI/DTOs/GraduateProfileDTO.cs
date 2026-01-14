using System.ComponentModel.DataAnnotations;

namespace WebUI.DTOs
{
    public class GraduateProfileDTO
    {
        public string? Id { get; set; }
        public string MatricNumber { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int YearOfGraduation { get; set; }

        public string? EmploymentStatus { get; set; }
        public string? CurrentEmployer { get; set; }
        public string? JobTitle { get; set; }
        public string? Location { get; set; }

        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string? PhotoUrl { get; set; }
        public string? HighestAcademicQualification { get; set; }

        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }

    public class GraduateBulkUploadDTO
    {
        [Required(ErrorMessage = "Department is required")]
        public string DepartmentId { get; set; }

        [Required(ErrorMessage = "Graduation year is required")]
        [Range(1900, 2100, ErrorMessage = "Please enter a valid year")]
        public int YearOfGraduation { get; set; }

        [Required(ErrorMessage = "Excel file is required")]
        public IFormFile ExcelFile { get; set; }
    }

    public class ExcelColumnMapping
    {
        public string MatricNumber { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        // Add more as needed
    }

    public class GraduateUpdateDTO
    {
        [Required(ErrorMessage = "Matric number is required")]
        [StringLength(50, ErrorMessage = "Matric number cannot exceed 50 characters")]
        public string MatricNumber { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        [StringLength(100, ErrorMessage = "Qualification cannot exceed 100 characters")]
        public string? HighestAcademicQualification { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public string DepartmentId { get; set; }

        [Required(ErrorMessage = "Graduation year is required")]
        [Range(1900, 2100, ErrorMessage = "Please enter a valid year")]
        public int YearOfGraduation { get; set; }

        public string? EmploymentStatus { get; set; }

        [StringLength(200, ErrorMessage = "Employer name cannot exceed 200 characters")]
        public string? CurrentEmployer { get; set; }

        [StringLength(100, ErrorMessage = "Job title cannot exceed 100 characters")]
        public string? JobTitle { get; set; }

        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }

        public IFormFile? PassportPhoto { get; set; }
        public bool? RemovePassportPhoto { get; set; }
    }
}