using BusinessLogic.Interfaces;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class SocialNetworkController : Controller
    {
        private readonly IRepository<RoomSubject> repoSub;
        private readonly IRepository<RoomSubjectComment> repoComment;

        public SocialNetworkController(IRepository<RoomSubject> _repoSub, 
                                       IRepository<RoomSubjectComment> _repoComment)
        {
            repoSub = _repoSub;
            repoComment = _repoComment;
        }
        public async Task<IActionResult> Index()
        {
            var subjects = await repoSub.GetAll();
            var subjectViewModels = subjects.Select(s => new RoomSubjectViewModel
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                CreatedDate = Convert.ToDateTime(s.CreatedAt),
                CommentCount = repoComment.GetByQueryAsync(x => x.SubjectID == s.Id).Result.Count(),
            }).OrderByDescending(s => s.CreatedDate).ToList();

            return View(subjectViewModels);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjectDetail(string id)
        {
            var subject = await repoSub.GetByIdAsync(x => x.Id == id);
            if (subject == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var comments =  repoComment.GetByQueryAsync(x => x.SubjectID == subject.Id).Result.Select(c => new CommentViewModel
            {
                Id = c.Id,
                Comment = c.Comment,
                Sender = c.Sender,
                CreatedDate = Convert.ToDateTime(c.CreatedAt),
                IsCurrentUser = c.Sender == currentUserId
            }).OrderBy(c => c.CreatedDate).ToList();

            var viewModel = new RoomSubjectDetailViewModel
            {
                Id = subject.Id,
                Title = subject.Title,
                Description = subject.Description,
                Comments = comments ?? new List<CommentViewModel>()
            };

            return PartialView("_SubjectDetailPartial", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(CreateRoomSubjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var subject = new RoomSubject
            {
                Title = model.Title,
                Description = model.Description
            };

             repoSub.Add(subject);

            return Json(new { success = true, id = subject.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddComment(string subjectId, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return BadRequest("Comment cannot be empty");

            var currentUser = User.Identity.Name;
            if (string.IsNullOrEmpty(currentUser))
                return Unauthorized();

            var newComment = new RoomSubjectComment
            {
                SubjectID = subjectId,
                Comment = comment,
                Sender = currentUser
            };

             repoComment.Add(newComment);

            return Json(new
            {
                success = true,
                comment = newComment.Comment,
                sender = newComment.Sender,
                createdDate = newComment.CreatedAt,
                isCurrentUser = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(string commentId, string newComment)
        {
            if (string.IsNullOrWhiteSpace(newComment))
                return BadRequest("Comment cannot be empty");

            var comment = await repoComment.GetByIdAsync(x => x.Id == commentId);
            if (comment == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (comment.Sender != currentUserId)
                return Forbid();

            comment.Comment = newComment;
            comment.UpdatedAt = DateTime.UtcNow.ToString();

            repoComment.Update(comment);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubject(string subjectId, string title, string description)
        {
            var subject = await repoSub.GetByIdAsync(x => x.Id == subjectId);
            if (subject == null)
                return NotFound();

            subject.Title = title;
            subject.Description = description;
            subject.UpdatedAt = DateTime.UtcNow.ToString();

            repoSub.Update(subject);

            return Json(new { success = true });
        }


        //SUPPORTING VIEW MODELS
        public class RoomSubjectViewModel
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime CreatedDate { get; set; }
            public int CommentCount { get; set; }
        }

        public class RoomSubjectDetailViewModel
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public List<CommentViewModel> Comments { get; set; }
            public string NewComment { get; set; }
        }

        public class CommentViewModel
        {
            public string Id { get; set; }
            public string Comment { get; set; }
            public string Sender { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool IsCurrentUser { get; set; }
        }

        public class CreateRoomSubjectViewModel
        {
            [Required(ErrorMessage = "Title is required")]
            [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
            public string Title { get; set; }

            [Required(ErrorMessage = "Description is required")]
            [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
            public string Description { get; set; }
        }
    }
}
