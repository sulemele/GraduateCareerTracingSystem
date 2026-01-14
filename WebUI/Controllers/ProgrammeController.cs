using BusinessLogic.Interfaces;
using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using WebUI.DTOs;

namespace WebUI.Controllers
{
    public class ProgrammeController : Controller
    {
        private readonly IRepository<Programme> _repoProg;
        private readonly IRepository<Department> _repoDept;

        public ProgrammeController(IRepository<Programme> repoProg,
                                   IRepository<Department> repoDept)
        {
            _repoProg = repoProg;
            _repoDept = repoDept;
        }

        // GET: Programme/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: Programme/GetProgrammes (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetProgrammes()
        {
            try
            {
                var programmeList = await _repoProg.GetAll();

                var programmes = programmeList
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(async p => new ProgrammeDTO
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Description = p.Description,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        DeptCount = _repoDept.GetAll().Result.Count()
                    })
                    .ToList();

                return Json(new { success = true, data = programmes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading programmes: " + ex.Message });
            }
        }

        // GET: Programme/GetProgramme/{id} (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetProgramme(string id)
        {
            try
            {
                var dbProg = await _repoProg.GetByIdAsync(x => x.Id == id);

                if (dbProg == null)
                {
                    return Json(new { success = false, message = "Programme not found" });
                }

                var programme = new ProgrammeDTO
                {
                    Id = dbProg.Id,
                    Title = dbProg.Title,
                    Description = dbProg.Description,
                    CreatedAt = dbProg.CreatedAt,
                    UpdatedAt = dbProg.UpdatedAt
                };

                return Json(new { success = true, data = programme });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading programme: " + ex.Message });
            }
        }

        // POST: Programme/Create (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] ProgrammeDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = errors
                });
            }

            try
            {
                var programme = new Programme
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = dto.Title,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                _repoProg.Add(programme);

                return Json(new
                {
                    success = true,
                    message = "Programme created successfully",
                    data = new ProgrammeDTO
                    {
                        Id = programme.Id,
                        Title = programme.Title,
                        Description = programme.Description,
                        CreatedAt = programme.CreatedAt,
                        UpdatedAt = programme.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating programme: " + ex.Message });
            }
        }

        // POST: Programme/Edit/{id} (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromBody] ProgrammeDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = errors
                });
            }

            try
            {
                var programme = await _repoProg.GetByIdAsync(x => x.Id == id);
                if (programme == null)
                {
                    return Json(new { success = false, message = "Programme not found" });
                }

                programme.Title = dto.Title;
                programme.Description = dto.Description;
                programme.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                _repoProg.Update(programme);

                return Json(new
                {
                    success = true,
                    message = "Programme updated successfully",
                    data = new ProgrammeDTO
                    {
                        Id = programme.Id,
                        Title = programme.Title,
                        Description = programme.Description,
                        CreatedAt = programme.CreatedAt,
                        UpdatedAt = programme.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating programme: " + ex.Message });
            }
        }

        // POST: Programme/Delete/{id} (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var programme = await _repoProg.GetByIdAsync(x => x.Id == id);
                if (programme == null)
                {
                    return Json(new { success = false, message = "Programme not found" });
                }

                // Check if programme has departments
                var departments = await _repoDept.GetByQueryAsync(d => d.ProgrammeId == id);
                if (departments.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Cannot delete programme. It has {departments.Count()} associated department(s). Please remove departments first."
                    });
                }

                _repoProg.Delete(programme);

                return Json(new { success = true, message = "Programme deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting programme: " + ex.Message });
            }
        }

        // GET: Programme/CheckDependencies/{id} (AJAX)
        [HttpGet]
        public async Task<IActionResult> CheckDependencies(string id)
        {
            try
            {
                var departments = await _repoDept.GetByQueryAsync(d => d.ProgrammeId == id);

                if (departments.Any())
                {
                    return Json(new
                    {
                        hasDependencies = true,
                        message = $"This programme has {departments.Count()} associated department(s). Please remove them first."
                    });
                }

                return Json(new { hasDependencies = false });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error checking dependencies: " + ex.Message });
            }
        }
    }
}
