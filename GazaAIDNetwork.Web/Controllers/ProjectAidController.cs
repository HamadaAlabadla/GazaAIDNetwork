using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Services.CycleAidService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazaAIDNetwork.Web.Controllers
{
    [Authorize]
    public class ProjectAidController : Controller
    {
        private readonly IProjectAidService _projectAidService;
        public ProjectAidController(IProjectAidService projectAidService)
        {
            _projectAidService = projectAidService;
        }
        [Authorize(Roles = "admin,manager")]
        [HttpGet]
        public IActionResult Index(string cycleId)
        {
            TempData["cycleId"] = cycleId;
            return View();
        }
        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> GeAllProjectssAid([FromForm]string cycleId)
        {
            var data = await _projectAidService.GetAllProjectsAidAsync(Request, cycleId, HttpContext);
            return Ok(data.result);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]

        public async Task<IActionResult> Create(ProjectAid projectAid)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات المدخلة غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            try
            {
                var result = await _projectAidService.CreateProjectAidAsync(projectAid, HttpContext);
                return Json(new { success = result.Success, message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "فشل بدء مشروع إغاثي جديد", error = ex.Message });
            }
        }


        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _projectAidService.DeleteProjectAidAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء حذف المشروع", error = ex.Message });
            }
        }
        

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Start(string id , DateTime EndDate)
        {
            try
            {
                var result = await _projectAidService.StartProjectAidAsync(id,EndDate , HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء إعتماد المشروع", error = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, string projectAidId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("يرجى اختيار ملف صالح.");

            var result = await _projectAidService.ImportOrdersAidForProjectAsync(file, projectAidId, HttpContext);

            if (result.result != null) // If an error file is generated
            {
                return File((byte[])result.result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ImportErrors.xlsx");
            }

            return Ok(result.Message);
        }


    }
}
