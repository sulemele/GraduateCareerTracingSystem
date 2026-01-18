using DataAccess.Models;

namespace WebUI.DTOs
{
    public class GraduateProfileViewModel
    {
        public GraduateProfileDTO Graduate { get; set; }
        public Department Department { get; set; }
        public Programme Programme { get; set; }
    }
}
