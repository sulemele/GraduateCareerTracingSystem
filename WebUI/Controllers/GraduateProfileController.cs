using BusinessLogic.Interfaces;
using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using WebUI.DTOs;

namespace WebUI.Controllers
{
    public class GraduateProfileController : Controller
    {

        private readonly IRepository<GraduateProfile> _repoGraduate;
        private readonly IRepository<Department> _repoDept;
        private readonly IRepository<Programme> _repoProg;
        private readonly IWebHostEnvironment _environment;

        public GraduateProfileController(
            IRepository<GraduateProfile> repoGraduate,
            IRepository<Department> repoDept,
            IRepository<Programme> repoProg,
            IWebHostEnvironment environment)
        {
            _repoGraduate = repoGraduate;
            _repoDept = repoDept;
            _repoProg = repoProg;
            _environment = environment;

            // Set EPPlus license context
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage.License.SetNonCommercialPersonal("fafaf");

        }

        // GET: GraduateProfile/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: GraduateProfile/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // GET: GraduateProfile/GetGraduates (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetGraduates()
        {
            try
            {
                var graduateList = await _repoGraduate.GetAll();
                var departmentList = await _repoDept.GetAll();
                var progs = await _repoProg.GetAll();

                var graduates = graduateList
                    .OrderByDescending(g => g.CreatedAt)
                    .Select(g =>
                    {
                        var department = departmentList
                                        .FirstOrDefault(d => d.Id == g.DepartmentId);

                        var programme = progs
                            .FirstOrDefault(p => p.Id == department?.ProgrammeId);
                        return new GraduateProfileDTO
                        {
                            Id = g.Id,
                            MatricNumber = g.MatricNumber,
                            DepartmentId = g.DepartmentId,
                            DepartmentName = department.Title ?? "Unknown",
                            ProgrammeId = department.ProgrammeId,
                            ProgrammeName = programme.Title,

                            YearOfGraduation = g.YearOfGraduation,
                            EmploymentStatus = g.EmploymentStatus,
                            CurrentEmployer = g.CurrentEmployer,
                            JobTitle = g.JobTitle,
                            Location = g.Location,
                            Name = g.Name,
                            Email = g.Email,
                            PhoneNumber = g.PhoneNumber,
                            Gender = g.Gender,
                            PhotoUrl = g.PhotoUrl,
                            HighestAcademicQualification = g.HighestAcademicQualification,
                            CreatedAt = g.CreatedAt,
                            UpdatedAt = g.UpdatedAt
                        };
                    })
                    .ToList();

                return Json(new { success = true, data = graduates });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading graduates: " + ex.Message });
            }
        }

        // GET: GraduateProfile/GetDepartments (for dropdown)
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var departments = await _repoDept.GetAll();
                var departmentList = departments
                    .OrderBy(d => d.Title)
                    .Select(d => new
                    {
                        id = d.Id,
                        title = d.Title,
                        programme = d.ProgrammeId // You might want to join with Programme for full name
                    })
                    .ToList();

                return Json(new { success = true, data = departmentList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading departments: " + ex.Message });
            }
        }

        // POST: GraduateProfile/UploadExcel (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel([FromForm] GraduateBulkUploadDTO dto)
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
                // Verify department exists
                var department = await _repoDept.GetByIdAsync(x => x.Id == dto.DepartmentId);
                if (department == null)
                {
                    return Json(new { success = false, message = "Selected department does not exist" });
                }

                // Validate file
                if (dto.ExcelFile == null || dto.ExcelFile.Length == 0)
                {
                    return Json(new { success = false, message = "Please upload an Excel file" });
                }

                var fileExtension = Path.GetExtension(dto.ExcelFile.FileName).ToLower();
                if (fileExtension != ".xlsx" && fileExtension != ".xls")
                {
                    return Json(new { success = false, message = "Only Excel files (.xlsx, .xls) are allowed" });
                }

                // Create uploads directory if not exists
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "excel");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Save the uploaded file
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ExcelFile.CopyToAsync(stream);
                }

                // Process Excel file
                var processingResult = ProcessExcelFile(filePath, dto.DepartmentId, dto.YearOfGraduation);

                // Clean up uploaded file
                System.IO.File.Delete(filePath);

                if (!processingResult.Success)
                {
                    return Json(new
                    {
                        success = false,
                        message = processingResult.Message,
                        errors = processingResult.Errors,
                        failedRows = processingResult.FailedRows
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"Successfully uploaded {processingResult.ProcessedCount} graduate(s).",
                    processed = processingResult.ProcessedCount,
                    skipped = processingResult.SkippedCount,
                    details = processingResult.Details
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing upload: " + ex.Message });
            }
        }

        // POST: GraduateProfile/ValidateExcel (Preview)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateExcel([FromForm] IFormFile excelFile)
        {
            try
            {
                if (excelFile == null || excelFile.Length == 0)
                {
                    return Json(new { success = false, message = "Please upload an Excel file" });
                }

                var fileExtension = Path.GetExtension(excelFile.FileName).ToLower();
                if (fileExtension != ".xlsx" && fileExtension != ".xls")
                {
                    return Json(new { success = false, message = "Only Excel files (.xlsx, .xls) are allowed" });
                }

                // Create temp file
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await excelFile.CopyToAsync(stream);
                }

                // Validate Excel structure
                var validationResult = ValidateExcelStructure(tempFilePath);

                // Clean up temp file
                System.IO.File.Delete(tempFilePath);

                if (!validationResult.Success)
                {
                    return Json(new
                    {
                        success = false,
                        message = validationResult.Message,
                        errors = validationResult.Errors
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Excel file is valid and ready for upload",
                    preview = validationResult.PreviewData,
                    columns = validationResult.Columns,
                    rowCount = validationResult.RowCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error validating file: " + ex.Message });
            }
        }

        // POST: GraduateProfile/CreateSingle (for manual entry)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSingle([FromBody] GraduateProfileDTO dto)
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
                // Verify department exists
                var department = await _repoDept.GetByIdAsync(x => x.Id == dto.DepartmentId);
                if (department == null)
                {
                    return Json(new { success = false, message = "Selected department does not exist" });
                }

                // Check for duplicate matric number
                var existingGraduate = await _repoGraduate.GetByIdAsync(x => x.MatricNumber == dto.MatricNumber);
                if (existingGraduate != null)
                {
                    return Json(new { success = false, message = $"Graduate with matric number '{dto.MatricNumber}' already exists" });
                }

                var graduate = new GraduateProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    MatricNumber = dto.MatricNumber,
                    DepartmentId = dto.DepartmentId,
                    YearOfGraduation = dto.YearOfGraduation,
                    EmploymentStatus = dto.EmploymentStatus,
                    CurrentEmployer = dto.CurrentEmployer,
                    JobTitle = dto.JobTitle,
                    Location = dto.Location,
                    Name = dto.Name,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Gender = dto.Gender,
                    PhotoUrl = dto.PhotoUrl,
                    HighestAcademicQualification = dto.HighestAcademicQualification,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                _repoGraduate.Add(graduate);

                return Json(new
                {
                    success = true,
                    message = "Graduate profile created successfully",
                    data = new GraduateProfileDTO
                    {
                        Id = graduate.Id,
                        MatricNumber = graduate.MatricNumber,
                        DepartmentId = graduate.DepartmentId,
                        YearOfGraduation = graduate.YearOfGraduation,
                        Name = graduate.Name,
                        Email = graduate.Email,
                        Gender = graduate.Gender
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating graduate profile: " + ex.Message });
            }
        }

        public IActionResult Analytics()
        {
            return View();
        }

        // GET: GraduateProfile/DownloadTemplate
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Graduates");

                    // Add headers with formatting
                    worksheet.Cells["A1"].Value = "MatricNumber";
                    worksheet.Cells["B1"].Value = "Name";
                    worksheet.Cells["C1"].Value = "Email";
                    worksheet.Cells["D1"].Value = "Gender";
                    worksheet.Cells["E1"].Value = "PhoneNumber";
                    worksheet.Cells["F1"].Value = "HighestAcademicQualification";

                    // Style the header
                    using (var range = worksheet.Cells["A1:F1"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }

                    // Set column widths
                    worksheet.Column(1).Width = 20; // MatricNumber
                    worksheet.Column(2).Width = 25; // Name
                    worksheet.Column(3).Width = 30; // Email
                    worksheet.Column(4).Width = 15; // Gender
                    worksheet.Column(5).Width = 20; // PhoneNumber
                    worksheet.Column(6).Width = 30; // HighestAcademicQualification

                    // Add example data
                    worksheet.Cells["A2"].Value = "PGDE/0000/0000";
                    worksheet.Cells["B2"].Value = "John Doe";
                    worksheet.Cells["C2"].Value = "john.doe@example.com";
                    worksheet.Cells["D2"].Value = "Male";
                    worksheet.Cells["E2"].Value = "+2348012345678";
                    worksheet.Cells["F2"].Value = "B.Sc Computer Science";

                    // Add notes sheet
                    var notesSheet = package.Workbook.Worksheets.Add("Instructions");
                    notesSheet.Cells["A1"].Value = "Excel Upload Template Instructions";
                    notesSheet.Cells["A1"].Style.Font.Bold = true;
                    notesSheet.Cells["A1"].Style.Font.Size = 14;

                    notesSheet.Cells["A3"].Value = "Required Columns:";
                    notesSheet.Cells["A3"].Style.Font.Bold = true;
                    notesSheet.Cells["A4"].Value = "• MatricNumber: Unique student identification number (Required)";
                    notesSheet.Cells["A5"].Value = "• Name: Full name of graduate (Required)";
                    notesSheet.Cells["A6"].Value = "• Email: Valid email address";
                    notesSheet.Cells["A7"].Value = "• Gender: Male/Female/Other";
                    notesSheet.Cells["A8"].Value = "• PhoneNumber: Contact phone number";
                    notesSheet.Cells["A9"].Value = "• HighestAcademicQualification: Highest degree obtained";

                    notesSheet.Cells["A11"].Value = "Notes:";
                    notesSheet.Cells["A11"].Style.Font.Bold = true;
                    notesSheet.Cells["A12"].Value = "• Do not modify the column headers";
                    notesSheet.Cells["A13"].Value = "• All columns are optional except MatricNumber and Name";
                    notesSheet.Cells["A14"].Value = "• Remove the example row before uploading your data";
                    notesSheet.Cells["A15"].Value = "• Save the file as .xlsx format";

                    // Auto-fit columns
                    notesSheet.Cells["A1:A15"].AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var fileName = $"Graduate_Upload_Template_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating template: " + ex.Message });
            }
        }

        // GET: GraduateProfile/GetGraduate/{id} (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetGraduate(string id)
        {
            try
            {
                var dbGraduate = await _repoGraduate.GetByIdAsync(x => x.Id == id);
                if (dbGraduate == null)
                {
                    return Json(new { success = false, message = "Graduate not found" });
                }

                var department = await _repoDept.GetByIdAsync(x => x.Id == dbGraduate.DepartmentId);

                var graduate = new GraduateProfileDTO
                {
                    Id = dbGraduate.Id,
                    MatricNumber = dbGraduate.MatricNumber,
                    DepartmentId = dbGraduate.DepartmentId,
                    DepartmentName = department?.Title ?? "Unknown",
                    YearOfGraduation = dbGraduate.YearOfGraduation,
                    EmploymentStatus = dbGraduate.EmploymentStatus,
                    CurrentEmployer = dbGraduate.CurrentEmployer,
                    JobTitle = dbGraduate.JobTitle,
                    Location = dbGraduate.Location,
                    Name = dbGraduate.Name,
                    Email = dbGraduate.Email,
                    PhoneNumber = dbGraduate.PhoneNumber,
                    Gender = dbGraduate.Gender,
                    PhotoUrl = dbGraduate.PhotoUrl,
                    HighestAcademicQualification = dbGraduate.HighestAcademicQualification,
                    CreatedAt = dbGraduate.CreatedAt,
                    UpdatedAt = dbGraduate.UpdatedAt
                };

                return Json(new { success = true, data = graduate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading graduate: " + ex.Message });
            }
        }

        // POST: GraduateProfile/Edit/{id} (AJAX with file upload)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromForm] GraduateUpdateDTO dto)
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
                var graduate = await _repoGraduate.GetByIdAsync(x => x.Id == id);
                if (graduate == null)
                {
                    return Json(new { success = false, message = "Graduate not found" });
                }

                // Verify department exists
                var department = await _repoDept.GetByIdAsync(x => x.Id == dto.DepartmentId);
                if (department == null)
                {
                    return Json(new { success = false, message = "Selected department does not exist" });
                }

                // Check for duplicate matric number if changed
                if (graduate.MatricNumber != dto.MatricNumber)
                {
                    var existingGraduate = await _repoGraduate.GetByIdAsync(x => x.MatricNumber == dto.MatricNumber);
                    if (existingGraduate != null)
                    {
                        return Json(new { success = false, message = $"Graduate with matric number '{dto.MatricNumber}' already exists" });
                    }
                }

                // Update graduate
                graduate.MatricNumber = dto.MatricNumber;
                graduate.Name = dto.Name;
                graduate.Email = dto.Email;
                graduate.PhoneNumber = dto.PhoneNumber;
                graduate.Gender = dto.Gender;
                graduate.HighestAcademicQualification = dto.HighestAcademicQualification;
                graduate.DepartmentId = dto.DepartmentId;
                graduate.YearOfGraduation = dto.YearOfGraduation;
                graduate.EmploymentStatus = dto.EmploymentStatus;
                graduate.CurrentEmployer = dto.CurrentEmployer;
                graduate.JobTitle = dto.JobTitle;
                graduate.Location = dto.Location;
                graduate.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                // Handle passport photo upload/removal
                if (dto.RemovePassportPhoto == true && !string.IsNullOrEmpty(graduate.PhotoUrl))
                {
                    // Remove existing photo
                    DeletePassportPhoto(graduate.PhotoUrl);
                    graduate.PhotoUrl = null;
                }
                else if (dto.PassportPhoto != null && dto.PassportPhoto.Length > 0)
                {
                    // Remove old photo if exists
                    if (!string.IsNullOrEmpty(graduate.PhotoUrl))
                    {
                        DeletePassportPhoto(graduate.PhotoUrl);
                    }

                    // Save new photo
                    var photoUrl = await SavePassportPhoto(dto.PassportPhoto, graduate.Id);
                    graduate.PhotoUrl = photoUrl;
                }

                _repoGraduate.Update(graduate);

                return Json(new
                {
                    success = true,
                    message = "Graduate profile updated successfully",
                    data = new GraduateProfileDTO
                    {
                        Id = graduate.Id,
                        MatricNumber = graduate.MatricNumber,
                        Name = graduate.Name,
                        DepartmentId = graduate.DepartmentId,
                        YearOfGraduation = graduate.YearOfGraduation,
                        PhotoUrl = graduate.PhotoUrl
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating graduate profile: " + ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userEmail = User.Identity.Name;

                // Get graduate by ID
                var dbGraduate = await _repoGraduate.GetByIdAsync(x => x.Email == userEmail);
                if (dbGraduate == null)
                {
                    return NotFound();
                }

                // Get department
                var department = await _repoDept.GetByIdAsync(x => x.Id == dbGraduate.DepartmentId);

                // Get programme (if needed)
                var programme = department != null ? await _repoProg.GetByIdAsync(x => x.Id == department.ProgrammeId) : null;

                // Map to DTO
                var graduate = new GraduateProfileDTO
                {
                    Id = dbGraduate.Id,
                    MatricNumber = dbGraduate.MatricNumber,
                    DepartmentId = dbGraduate.DepartmentId,
                    DepartmentName = department?.Title ?? "Unknown Department",
                    YearOfGraduation = dbGraduate.YearOfGraduation,
                    EmploymentStatus = dbGraduate.EmploymentStatus,
                    CurrentEmployer = dbGraduate.CurrentEmployer,
                    JobTitle = dbGraduate.JobTitle,
                    Location = dbGraduate.Location,
                    Name = dbGraduate.Name,
                    Email = dbGraduate.Email,
                    PhoneNumber = dbGraduate.PhoneNumber,
                    Gender = dbGraduate.Gender,
                    PhotoUrl = dbGraduate.PhotoUrl,
                    HighestAcademicQualification = dbGraduate.HighestAcademicQualification,
                    CreatedAt = dbGraduate.CreatedAt,
                    UpdatedAt = dbGraduate.UpdatedAt
                };

                // Prepare view model
                var viewModel = new GraduateProfileViewModel
                {
                    Graduate = graduate,
                    Department = department,
                    Programme = programme,
                    // You can add more related data here
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error loading graduate profile with ID: {Id}", id);
                return View("Error");
            }
        }



        #region Private Methods

        private ExcelProcessingResult ProcessExcelFile(string filePath, string departmentId, int yearOfGraduation)
        {
            var result = new ExcelProcessingResult();
            var failedRows = new List<FailedRow>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        result.Message = "Excel file does not contain any worksheets";
                        return result;
                    }

                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    var colCount = worksheet.Dimension?.Columns ?? 0;

                    if (rowCount <= 1) // Only header row
                    {
                        result.Message = "Excel file contains no data rows";
                        return result;
                    }

                    // Find column indices
                    var columnMapping = FindColumnIndices(worksheet);

                    // Process each row
                    for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip header)
                    {
                        try
                        {
                            // Skip empty rows
                            if (IsRowEmpty(worksheet, row, columnMapping))
                            {
                                result.SkippedCount++;
                                continue;
                            }

                            var matricNumber = GetCellValue(worksheet, row, 1);
                            var name = GetCellValue(worksheet, row, columnMapping.NameIndex);
                            var email = GetCellValue(worksheet, row, columnMapping.EmailIndex);
                            var gender = GetCellValue(worksheet, row, columnMapping.GenderIndex);
                            var phone = GetCellValue(worksheet, row, 5);
                            var qualification = GetCellValue(worksheet, row, columnMapping.QualificationIndex);



                            // Validate required fields
                            if (string.IsNullOrWhiteSpace(matricNumber))
                            {
                                failedRows.Add(new FailedRow
                                {
                                    RowNumber = row,
                                    Error = "Matric number is required",
                                    Data = GetRowData(worksheet, row)
                                });
                                continue;
                            }




                            if (string.IsNullOrWhiteSpace(name))
                            {
                                failedRows.Add(new FailedRow
                                {
                                    RowNumber = row,
                                    Error = "Name is required",
                                    Data = GetRowData(worksheet, row)
                                });
                                continue;
                            }

                            // Check for duplicate matric number
                            var existingGraduate = _repoGraduate.GetByIdAsync(x => x.MatricNumber == matricNumber).Result;
                            if (existingGraduate != null)
                            {
                                failedRows.Add(new FailedRow
                                {
                                    RowNumber = row,
                                    Error = $"Duplicate matric number: {matricNumber}",
                                    Data = GetRowData(worksheet, row)
                                });
                                continue;
                            }

                            // Create graduate profile
                            var graduate = new GraduateProfile
                            {
                                Id = Guid.NewGuid().ToString(),
                                MatricNumber = matricNumber.Trim(),
                                DepartmentId = departmentId,
                                YearOfGraduation = yearOfGraduation,
                                Name = name?.Trim(),
                                Email = email?.Trim(),
                                Gender = gender?.Trim(),
                                PhoneNumber = phone?.Trim(),
                                HighestAcademicQualification = qualification?.Trim(),
                                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                                UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                            };

                            _repoGraduate.Add(graduate);
                            result.ProcessedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedRows.Add(new FailedRow
                            {
                                RowNumber = row,
                                Error = $"Processing error: {ex.Message}",
                                Data = GetRowData(worksheet, row)
                            });
                        }
                    }
                }

                result.Success = true;
                result.Message = $"Processed {result.ProcessedCount} graduate(s)";
                result.FailedRows = failedRows;
                result.Details = new
                {
                    TotalRows = result.ProcessedCount + result.SkippedCount + failedRows.Count,
                    Success = result.ProcessedCount,
                    Failed = failedRows.Count,
                    Skipped = result.SkippedCount
                };
            }
            catch (Exception ex)
            {
                result.Message = $"Error processing Excel file: {ex.Message}";
                result.Errors = new List<string> { ex.Message };
            }

            return result;
        }

        private ExcelValidationResult ValidateExcelStructure(string filePath)
        {
            var result = new ExcelValidationResult();
            var previewData = new List<Dictionary<string, string>>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        result.Message = "Excel file does not contain any worksheets";
                        return result;
                    }

                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    var colCount = worksheet.Dimension?.Columns ?? 0;

                    // Get column headers
                    var headers = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var header = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(header))
                        {
                            headers.Add(header);
                            result.Columns.Add(header);
                        }
                    }

                    // Check for required columns
                    var requiredColumns = new[] { "MatricNumber", "Name" };
                    var missingColumns = requiredColumns.Where(rc => !headers.Any(h =>
                        string.Equals(h, rc, StringComparison.OrdinalIgnoreCase))).ToList();

                    if (missingColumns.Any())
                    {
                        result.Message = $"Missing required columns: {string.Join(", ", missingColumns)}";
                        result.Errors = missingColumns;
                        return result;
                    }

                    result.RowCount = rowCount - 1; // Exclude header

                    // Get preview data (first 5 rows)
                    var previewRows = Math.Min(6, rowCount); // Include header + 5 data rows
                    for (int row = 1; row <= previewRows; row++)
                    {
                        var rowData = new Dictionary<string, string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            if (row == 1) // Header row
                            {
                                var header = worksheet.Cells[row, col].Text?.Trim();
                                if (!string.IsNullOrWhiteSpace(header))
                                {
                                    rowData[header] = worksheet.Cells[row, col].Text?.Trim();
                                }
                            }
                            else // Data rows
                            {
                                var header = worksheet.Cells[1, col].Text?.Trim();
                                if (!string.IsNullOrWhiteSpace(header))
                                {
                                    rowData[header] = worksheet.Cells[row, col].Text?.Trim();
                                }
                            }
                        }
                        previewData.Add(rowData);
                    }

                    result.PreviewData = previewData;
                    result.Success = true;
                    result.Message = "Excel file structure is valid";
                }
            }
            catch (Exception ex)
            {
                result.Message = $"Error validating Excel file: {ex.Message}";
                result.Errors = new List<string> { ex.Message };
            }

            return result;
        }

        private ColumnMapping FindColumnIndices(ExcelWorksheet worksheet)
        {
            var mapping = new ColumnMapping();
            var colCount = worksheet.Dimension?.Columns ?? 0;

            for (int col = 1; col <= colCount; col++)
            {
                var header = worksheet.Cells[1, col].Text?.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(header))
                    continue;

                if (header.Contains("matric") || header.Contains("id") || header.Contains("number"))
                    mapping.MatricNumberIndex = col;
                else if (header.Contains("name") || header.Contains("fullname"))
                    mapping.NameIndex = col;
                else if (header.Contains("email") || header.Contains("mail"))
                    mapping.EmailIndex = col;
                else if (header.Contains("gender") || header.Contains("sex"))
                    mapping.GenderIndex = col;
                else if (header.Contains("phone") || header.Contains("mobile") || header.Contains("contact"))
                    mapping.PhoneIndex = col;
                else if (header.Contains("qualification") || header.Contains("degree") || header.Contains("education"))
                    mapping.QualificationIndex = col;
            }

            return mapping;
        }

        private bool IsRowEmpty(ExcelWorksheet worksheet, int row, ColumnMapping mapping)
        {
            // Check if all mapped columns are empty
            var indices = new[]
            {
                mapping.MatricNumberIndex,
                mapping.NameIndex,
                mapping.EmailIndex,
                mapping.GenderIndex,
                mapping.PhoneIndex,
                mapping.QualificationIndex
            };

            return indices.All(index => string.IsNullOrWhiteSpace(GetCellValue(worksheet, row, index)));
        }

        private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            if (col <= 0) return null;
            return worksheet.Cells[row, col].Text?.Trim();
        }

        private Dictionary<string, string> GetRowData(ExcelWorksheet worksheet, int row)
        {
            var data = new Dictionary<string, string>();
            var colCount = worksheet.Dimension?.Columns ?? 0;

            for (int col = 1; col <= colCount; col++)
            {
                var header = worksheet.Cells[1, col].Text?.Trim();
                var value = worksheet.Cells[row, col].Text?.Trim();

                if (!string.IsNullOrWhiteSpace(header))
                {
                    data[header] = value;
                }
            }

            return data;
        }

        #endregion

        #region Helper Classes

        private class ExcelProcessingResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public int ProcessedCount { get; set; }
            public int SkippedCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<FailedRow> FailedRows { get; set; } = new List<FailedRow>();
            public object Details { get; set; }
        }

        private class ExcelValidationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Columns { get; set; } = new List<string>();
            public List<Dictionary<string, string>> PreviewData { get; set; }
            public int RowCount { get; set; }
        }

        private class ColumnMapping
        {
            public int MatricNumberIndex { get; set; }
            public int NameIndex { get; set; }
            public int EmailIndex { get; set; }
            public int GenderIndex { get; set; }
            public int PhoneIndex { get; set; }
            public int QualificationIndex { get; set; }
        }

        private class FailedRow
        {
            public int RowNumber { get; set; }
            public string Error { get; set; }
            public Dictionary<string, string> Data { get; set; }
        }

        #endregion

        private async Task<string> SavePassportPhoto(IFormFile file, string graduateId)
        {
            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "passports");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                var fileName = $"passport_{graduateId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Optimize image (optional - you can use ImageSharp or similar)
                // await OptimizeImage(filePath);

                // Return relative URL
                return $"/uploads/passports/{fileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving passport photo: {ex.Message}");
            }
        }

        private void DeletePassportPhoto(string photoUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(photoUrl))
                {
                    var fileName = Path.GetFileName(photoUrl);
                    var filePath = Path.Combine(_environment.WebRootPath, "uploads", "passports", fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want to fail the whole operation
                // because of a file deletion error
                Console.WriteLine($"Error deleting passport photo: {ex.Message}");
            }
        }

        private async Task OptimizeImage(string filePath)
        {
            // Optional: Use ImageSharp or similar library to optimize images
            // This reduces file size while maintaining quality
            // Example with ImageSharp (requires NuGet package SixLabors.ImageSharp)
            /*
            using var image = await Image.LoadAsync(filePath);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(400, 400),
                Mode = ResizeMode.Max
            }));

            // Save with quality compression
            await image.SaveAsync(filePath, new JpegEncoder
            {
                Quality = 80
            });
            */
        }
    }
}
