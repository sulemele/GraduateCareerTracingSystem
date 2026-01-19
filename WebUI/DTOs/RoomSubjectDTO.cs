namespace WebUI.DTOs
{
   
    public class RoomSubjectDTO
    {
        public string CreatedAt { get; set; }
        public string? Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }

    }

    public class RoomSubjectCommentDTO
    {

        public string CreatedAt { get; set; }
        public string? SubjectID { get; set; }
        public string Comment { get; set; }
        public string Sender { get; set; }


    }
}
