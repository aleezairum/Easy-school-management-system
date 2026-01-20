using Microsoft.AspNetCore.Mvc;
using School.Web.Models;

namespace School.Web.Controllers
{
    public class AdmissionController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdmissionController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [Route("admission/form")]
        public IActionResult Form()
        {
            ViewData["Title"] = "Admission Form";
            var model = new AdmissionFormViewModel();
            return View(model);
        }

        [Route("admission/form/print")]
        public IActionResult PrintForm()
        {
            ViewData["Title"] = "Admission Form - Print";
            var model = new AdmissionFormViewModel();
            return View("FormPrint", model);
        }

        [HttpPost]
        [Route("admission/save")]
        public async Task<IActionResult> Save(AdmissionFormViewModel model)
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

                // TODO: Save to Admissions table via API call
                TempData["Success"] = "Admission form submitted successfully!" +
                    (photoPath != null ? $" Photo saved." : "");

                return RedirectToAction("Form");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while saving the form: {ex.Message}";
                return View("Form", model);
            }
        }
    }
}
