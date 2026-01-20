using Microsoft.AspNetCore.Mvc;
using School.Web.Models;
using System.Text;
using System.Text.Json;

namespace School.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "http://localhost:5266/api";

        public StudentController(IWebHostEnvironment webHostEnvironment, IHttpClientFactory httpClientFactory)
        {
            _webHostEnvironment = webHostEnvironment;
            _httpClient = httpClientFactory.CreateClient();
        }

        [Route("student/list")]
        public async Task<IActionResult> List()
        {
            ViewData["Title"] = "Student List";
            var students = new List<StudentListViewModel>();

            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/Student");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    students = JsonSerializer.Deserialize<List<StudentListViewModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<StudentListViewModel>();
                }
            }
            catch
            {
                TempData["Error"] = "Unable to connect to the API. Please ensure the API is running.";
            }

            return View(students);
        }

        [Route("student/form")]
        public async Task<IActionResult> Form()
        {
            ViewData["Title"] = "Student Form";
            var model = new StudentFormViewModel();

            // Load admissions list for dropdown
            await LoadAdmissionsDropdown();

            return View(model);
        }

        private async Task LoadAdmissionsDropdown()
        {
            var admissions = new List<AdmissionDropdownItem>();
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/Admission");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    admissions = JsonSerializer.Deserialize<List<AdmissionDropdownItem>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<AdmissionDropdownItem>();
                }
            }
            catch
            {
                // Silent fail - dropdown will be empty
            }
            ViewBag.Admissions = admissions;
        }

        [Route("student/form/{id}")]
        public async Task<IActionResult> Form(int id)
        {
            ViewData["Title"] = "Edit Student";
            var model = new StudentFormViewModel();

            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/Student/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var student = JsonSerializer.Deserialize<StudentFormViewModel>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (student != null)
                    {
                        model = student;
                    }
                }
                else
                {
                    TempData["Error"] = "Student not found.";
                    return RedirectToAction("List");
                }
            }
            catch
            {
                TempData["Error"] = "Unable to connect to the API. Please ensure the API is running.";
            }

            // Load admissions dropdown
            await LoadAdmissionsDropdown();

            return View(model);
        }

        [Route("student/from-admission/{admissionId}")]
        public async Task<IActionResult> FromAdmission(int admissionId)
        {
            ViewData["Title"] = "Create Student from Admission";

            // Check if student already exists for this admission
            try
            {
                var existsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/Student/exists-by-admission/{admissionId}");
                if (existsResponse.IsSuccessStatusCode)
                {
                    var existsJson = await existsResponse.Content.ReadAsStringAsync();
                    var existsResult = JsonSerializer.Deserialize<JsonElement>(existsJson);
                    if (existsResult.GetProperty("exists").GetBoolean())
                    {
                        TempData["Error"] = "A student record already exists for this admission.";
                        return RedirectToAction("List");
                    }
                }
            }
            catch
            {
                // Continue even if check fails
            }

            var model = new StudentFormViewModel { AdmissionId = admissionId };

            // Pre-populate from admission data via API (would need an admission endpoint)
            // For now, just pass the admission ID
            ViewData["FromAdmission"] = true;

            return View("Form", model);
        }

        [HttpPost]
        [Route("student/save")]
        public async Task<IActionResult> Save(StudentFormViewModel model)
        {
            try
            {
                string? photoPath = null;

                // Handle photo upload
                if (model.PhotoFile != null && model.PhotoFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(model.PhotoFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Invalid file type. Only JPG, JPEG, and PNG files are allowed.";
                        return View("Form", model);
                    }

                    // Validate file size (2MB max)
                    if (model.PhotoFile.Length > 2 * 1024 * 1024)
                    {
                        TempData["Error"] = "File size must be less than 2MB.";
                        return View("Form", model);
                    }

                    // Create uploads directory if it doesn't exist
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "photos");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate unique filename
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.PhotoFile.CopyToAsync(fileStream);
                    }

                    // Store the relative path for database
                    photoPath = $"/uploads/photos/{uniqueFileName}";
                    model.StudentPhoto = photoPath;
                }

                // Prepare the data for API
                var studentData = new
                {
                    id = model.Id,
                    admissionId = model.AdmissionId,
                    registrationNo = model.RegistrationNo,
                    rollNo = model.RollNo,
                    admissionNo = model.AdmissionNo,
                    dateOfAdmission = string.IsNullOrEmpty(model.DateOfAdmission) ? (DateTime?)null : DateTime.Parse(model.DateOfAdmission),
                    currentClass = model.CurrentClass,
                    section = model.Section,
                    nameOfStudent = model.NameOfStudent ?? "",
                    nameOfStudentUrdu = model.NameOfStudentUrdu,
                    fatherName = model.FatherName,
                    fatherNameUrdu = model.FatherNameUrdu,
                    fatherCNIC = model.FatherCNIC,
                    fatherOccupation = model.FatherOccupation,
                    fatherMobile = model.FatherMobile,
                    motherName = model.MotherName,
                    motherCNIC = model.MotherCNIC,
                    motherMobile = model.MotherMobile,
                    guardianName = model.GuardianName,
                    guardianCNIC = model.GuardianCNIC,
                    guardianRelation = model.GuardianRelation,
                    guardianMobile = model.GuardianMobile,
                    dateOfBirth = string.IsNullOrEmpty(model.DateOfBirth) ? (DateTime?)null : DateTime.Parse(model.DateOfBirth),
                    dateOfBirthInWords = model.DateOfBirthInWords,
                    placeOfBirth = model.PlaceOfBirth,
                    formBNo = model.FormBNo,
                    gender = model.Gender,
                    religion = model.Religion,
                    bloodGroup = model.BloodGroup,
                    nationality = model.Nationality,
                    studentPhoto = model.StudentPhoto,
                    presentAddress = model.PresentAddress,
                    presentAddressUrdu = model.PresentAddressUrdu,
                    permanentAddress = model.PermanentAddress,
                    permanentAddressUrdu = model.PermanentAddressUrdu,
                    phoneResidence = model.PhoneResidence,
                    emergencyContact = model.EmergencyContact,
                    email = model.Email,
                    previousSchool = model.PreviousSchool,
                    lastClass = model.LastClass,
                    board = model.Board,
                    yearOfPassing = model.YearOfPassing,
                    marksObtained = model.MarksObtained,
                    totalMarks = model.TotalMarks,
                    percentage = model.Percentage,
                    monthlyFee = string.IsNullOrEmpty(model.MonthlyFee) ? (decimal?)null : decimal.Parse(model.MonthlyFee),
                    admissionFee = string.IsNullOrEmpty(model.AdmissionFee) ? (decimal?)null : decimal.Parse(model.AdmissionFee),
                    feeCategory = model.FeeCategory,
                    status = model.Status ?? "Active",
                    isActive = model.IsActive
                };

                var json = JsonSerializer.Serialize(studentData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (model.Id > 0)
                {
                    // Update existing student
                    response = await _httpClient.PutAsync($"{_apiBaseUrl}/Student/{model.Id}", content);
                }
                else
                {
                    // Create new student
                    response = await _httpClient.PostAsync($"{_apiBaseUrl}/Student", content);
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = model.Id > 0
                        ? "Student updated successfully!"
                        : "Student created successfully!";
                    return RedirectToAction("List");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Failed to save student: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while saving: {ex.Message}";
            }

            return View("Form", model);
        }

        [HttpPost]
        [Route("student/create-from-admission")]
        public async Task<IActionResult> CreateFromAdmission(int admissionId, string? currentClass, string? section, string? registrationNo, string? rollNo)
        {
            try
            {
                var data = new
                {
                    admissionId,
                    currentClass,
                    section,
                    registrationNo,
                    rollNo
                };

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/Student/from-admission", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Student created successfully from admission!";
                    return RedirectToAction("List");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Failed to create student: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("FromAdmission", new { admissionId });
        }

        [HttpPost]
        [Route("student/delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/Student/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Student deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete student.";
                }
            }
            catch
            {
                TempData["Error"] = "An error occurred while deleting the student.";
            }

            return RedirectToAction("List");
        }
    }
}
