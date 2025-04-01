using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using GazaAIDNetwork.Infrastructure.Services.DivisionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GazaAIDNetwork.Web.Controllers
{
    [Authorize]
    public class DivisionsController : Controller
    {
        private readonly IDivisionService _divisionService;
        private readonly UserManager<User> _userManager;

        public DivisionsController(IDivisionService divisionService,
            UserManager<User> userManager)
        {
            _divisionService = divisionService;
            _userManager = userManager;
        }
        [Authorize(Roles = "superadmin")]
        public async Task<IActionResult> Index()
        {
            var divisions = await _divisionService.GetAllDivisionsAsync();
            return View(divisions);
        }
        [HttpGet]
        [Authorize(Roles = "superadmin,admin,representative")]
        public async Task<IActionResult> GetAllDivisions()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null)
                return RedirectToPage("AccessDenied");
            var roleForUser = (await _userManager.GetRolesAsync(currentUser)).ToList();
            if (roleForUser == null || roleForUser.Count < 1)
                return RedirectToPage("AccessDenied");
            var divisions = await _divisionService.GetAllDivisionsAsync();

            if (roleForUser.Contains("superadmin"))
                return Ok(new ResultResponse()
                {
                    Success = true,
                    Message = "تم جلب البيانات بنجاح ",
                    result = divisions
                });
            else
                return Ok(new ResultResponse()
                {
                    Success = true,
                    Message = "تم جلب البيانات بنجاح ",
                    result = divisions.Where(x => x.Id.ToString().Equals(currentUser.DivisionId)),
                });
        }
        [Authorize(Roles = "superadmin")]
        [HttpGet]
        public async Task<IActionResult> GetDivision(string id)
        {
            var result = await _divisionService.GetDivisionAsync(id);
            return Json(new { success = result.Success, data = result.result, message = result.Message });
        }
        [Authorize(Roles = "superadmin")]
        // POST: DivisionsController/Create
        [HttpPost]

        public async Task<IActionResult> Create(Division division)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات المدخلة غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            try
            {
                var result = await _divisionService.CreateDivisionAsync(division, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "فشل إضافة شعبة جديدة", error = ex.Message });
            }
        }

        [Authorize(Roles = "superadmin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Division division)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صالحة", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var result = await _divisionService.UpdateDivisionAsync(division, HttpContext);
                return Json(new { success = result.Success, message = result.Message, errors = result.Errors, data = result.result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء تحديث الشعبة", error = ex.Message });
            }
        }
        [Authorize(Roles = "superadmin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _divisionService.DeleteDivisionAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء حذف الشعبة", error = ex.Message });
            }
        }
        [Authorize(Roles = "superadmin")]
        [HttpPost]
        public async Task<IActionResult> Activate(string id)
        {
            try
            {
                var result = await _divisionService.ActiveDivisionAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء إعادة تفعيل الشعبة", error = ex.Message });
            }
        }

    }
}
