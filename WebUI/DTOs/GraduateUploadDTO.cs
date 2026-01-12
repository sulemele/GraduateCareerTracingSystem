namespace WebUI.DTOs
{
    public class GraduateUploadDTO
    {
        public IFormFile File { get; set; }
        public string ProgrammeId { get; set; }
        public string DepartmentId { get; set; }
        public int GraduationYear { get; set; }

    }
}
