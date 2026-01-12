using System.ComponentModel.DataAnnotations;

namespace WebUI.DTOs
{
    //public class ProgrammeDTO
    //{

    //    public string? Id { get; set; }
    //    public string Title { get; set; }
    //    public string Description { get; set; }
    //}

    // ProgrammeCreateDto.cs
    public class ProgrammeDTO
    {
        public string? Id { get; set; }
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }
    }
}
