using BusinessLogic.Interfaces;
using DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebUI.DTOs;

namespace WebUI.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IRepository<Department> _repoDept;
        private readonly IRepository<Programme> _repoProg;

        public DepartmentController(IRepository<Department> repoDept,
                                    IRepository<Programme> repoProg)
        {
            _repoDept = repoDept;
            _repoProg = repoProg;
        }

        // GET: Department/Index
        public IActionResult Index(string? progId)
        {
            if (!string.IsNullOrWhiteSpace(progId))
                HttpContext.Session.SetString("ProgID", progId);
            else
                HttpContext.Session.SetString("ProgID", "");

            return View();
        }

        // GET: Department/GetDepartments (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var progId = HttpContext.Session.GetString("ProgID") ?? "";

            try
            {
                var departmentList = string.IsNullOrWhiteSpace(progId) == true ? await _repoDept.GetAll() : await _repoDept.GetByQueryAsync(x => x.ProgrammeId == progId);
                var programmeList = await _repoProg.GetAll();

                var departments = departmentList
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new DepartmentDTO
                    {
                        Id = d.Id,
                        Title = d.Title,
                        Description = d.Description,
                        ProgrammeId = d.ProgrammeId,
                        ProgrammeTitle = programmeList.FirstOrDefault(p => p.Id == d.ProgrammeId)?.Title ?? "Unknown Programme",
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt
                    })
                    .ToList();

                return Json(new { success = true, data = departments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading departments: " + ex.Message });
            }
        }

        // GET: Department/GetDepartment/{id} (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDepartment(string id)
        {
            try
            {
                var dbDept = await _repoDept.GetByIdAsync(x => x.Id == id);
                if (dbDept == null)
                {
                    return Json(new { success = false, message = "Department not found" });
                }

                var programme = await _repoProg.GetByIdAsync(x => x.Id == dbDept.ProgrammeId);

                var department = new DepartmentDTO
                {
                    Id = dbDept.Id,
                    Title = dbDept.Title,
                    Description = dbDept.Description,
                    ProgrammeId = dbDept.ProgrammeId,
                    ProgrammeTitle = programme?.Title ?? "Unknown Programme",
                    CreatedAt = dbDept.CreatedAt,
                    UpdatedAt = dbDept.UpdatedAt
                };

                return Json(new { success = true, data = department });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading department: " + ex.Message });
            }
        }

        // GET: Department/GetProgrammes (for dropdown)
        [HttpGet]
        public async Task<IActionResult> GetProgrammes()
        {
            try
            {
                var programmes = await _repoProg.GetAll();
                var programmeList = programmes
                    .OrderBy(p => p.Title)
                    .Select(p => new
                    {
                        id = p.Id,
                        title = p.Title
                    })
                    .ToList();

                return Json(new { success = true, data = programmeList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading programmes: " + ex.Message });
            }
        }

        // POST: Department/Create (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] DepartmentDTO dto)
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
                // Verify programme exists
                var programme = await _repoProg.GetByIdAsync(x => x.Id == dto.ProgrammeId);
                if (programme == null)
                {
                    return Json(new { success = false, message = "Selected programme does not exist" });
                }

                var department = new Department
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = dto.Title,
                    Description = dto.Description,
                    ProgrammeId = dto.ProgrammeId,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                _repoDept.Add(department);

                // Get programme title for response
                var programmeTitle = programme.Title;

                return Json(new
                {
                    success = true,
                    message = "Department created successfully",
                    data = new DepartmentDTO
                    {
                        Id = department.Id,
                        Title = department.Title,
                        Description = department.Description,
                        ProgrammeId = department.ProgrammeId,
                        ProgrammeTitle = programmeTitle,
                        CreatedAt = department.CreatedAt,
                        UpdatedAt = department.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating department: " + ex.Message });
            }
        }

        // POST: Department/Edit/{id} (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromBody] DepartmentDTO dto)
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
                var department = await _repoDept.GetByIdAsync(x => x.Id == id);
                var prog = await _repoProg.GetByIdAsync(x => x.Id == dto.ProgrammeId);

                if (department == null)
                {
                    return Json(new { success = false, message = "Department not found" });
                }

                // Verify programme exists if changed
                if (department.ProgrammeId != dto.ProgrammeId)
                {

                    if (prog== null)
                    {
                        return Json(new { success = false, message = "Selected programme does not exist" });
                    }
                }

                department.Title = dto.Title;
                department.Description = dto.Description;
                department.ProgrammeId = dto.ProgrammeId;
                department.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                _repoDept.Update(department);

                // Get updated programme title
               // var programme = await _repoProg.GetByIdAsync(x => x.Id == department.ProgrammeId);
                var programmeTitle = prog?.Title ?? "Unknown Programme";

                return Json(new
                {
                    success = true,
                    message = "Department updated successfully",
                    data = new DepartmentDTO
                    {
                        Id = department.Id,
                        Title = department.Title,
                        Description = department.Description,
                        ProgrammeId = department.ProgrammeId,
                        ProgrammeTitle = programmeTitle,
                        CreatedAt = department.CreatedAt,
                        UpdatedAt = department.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating department: " + ex.Message });
            }
        }

        // POST: Department/Delete/{id} (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var department = await _repoDept.GetByIdAsync(x => x.Id == id);
                if (department == null)
                {
                    return Json(new { success = false, message = "Department not found" });
                }

                // TODO: Check if department has related entities (e.g., Students, Courses)
                // var hasRelatedEntities = await CheckRelatedEntities(id);
                // if (hasRelatedEntities)
                // {
                //     return Json(new { success = false, message = "Cannot delete department. It has related entities." });
                // }

                _repoDept.Delete(department);

                return Json(new { success = true, message = "Department deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting department: " + ex.Message });
            }
        }

        // GET: Department/GetByProgramme/{programmeId} (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetByProgramme(string programmeId)
        {
            try
            {
                var departments = await _repoDept.GetByQueryAsync(d => d.ProgrammeId == programmeId);
                var departmentList = departments
                    .OrderBy(d => d.Title)
                    .Select(d => new
                    {
                        id = d.Id,
                        title = d.Title
                    })
                    .ToList();

                return Json(new { success = true, data = departmentList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading departments: " + ex.Message });
            }
        }
    }
}
