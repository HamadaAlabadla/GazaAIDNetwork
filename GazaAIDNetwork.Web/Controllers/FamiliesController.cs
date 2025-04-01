using GazaAIDNetwork.Core.Dtos;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Services.FamilyService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Web.Controllers
{
    [Authorize]
    public class FamiliesController : Controller
    {
        private readonly IFamilyService _familyService;
        private readonly UserManager<User> _userManager;
        public FamiliesController(IFamilyService familyService, UserManager<User> userManager)
        {
            _familyService = familyService;
            _userManager = userManager;
        }
        [Authorize(Roles = "superadmin,admin,representative,manager")]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [Authorize(Roles = "superadmin,admin,representative,manager")]
        [HttpPost]
        public async Task<IActionResult> GeAllFamilies()
        {
            var data = await _familyService.GetAllFamiliesAsync(Request, HttpContext);
            return Ok(data.result);
        }


        [Authorize(Roles = "superadmin,admin,representative")]
        [HttpGet]
        public IActionResult DeletedFamilies()
        {
            return View();
        }
        [Authorize(Roles = "superadmin,admin,representative")]
        [HttpPost]
        public async Task<IActionResult> GeAlltDeletedFamilies()
        {
            var data = await _familyService.GetAllDeletedFamiliesAsync(Request, HttpContext);
            return Ok(data.result);
        }


        [Authorize(Roles = "superadmin,admin,representative,manager")]
        [HttpGet]
        public IActionResult ReliefRequests()
        {
            return View();
        }
        [Authorize(Roles = "superadmin,admin,representative,manager")]
        [HttpPost]
        public async Task<IActionResult> GeAllReliefRequests()
        {
            var data = await _familyService.GetAllFamiliesAsync(Request, HttpContext);
            return Ok(data.result);
        }


        [Authorize(Roles = "representative")]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var result = await _familyService.GetFamilyDtoByFamiyIdAsync(id, HttpContext);
            if (result.Success)
                return View((FamilyDto)result.result!);
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "admin,representative")]
        [HttpGet]
        public IActionResult Create()
        {
            var familyDto = new FamilyDto();
            familyDto.DateChangeStatusForHusband = DateTime.Now;
            familyDto.DateChangeStatusForWife = DateTime.Now;
            return View(familyDto);
        }

        // POST: UserController/Create
        [Authorize(Roles = "admin,representative")]
        [HttpPost]
        public async Task<IActionResult> Create(FamilyDto familyDto)
        {

            if (!ModelState.IsValid)
            {
                // Return the view with validation errors
                return View(familyDto);
            }

            try
            {
                var result = await _familyService.CreateFamilyAsync(familyDto, HttpContext);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message); // Add service error message
                    return View(familyDto);
                }

                return RedirectToAction("Index"); // Redirect after successful creation
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء إنشاء العائلة. يرجى المحاولة لاحقًا.");
                return View(familyDto);
            }
        }
        [Authorize(Roles = "representative")]
        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("يرجى اختيار ملف صالح.");

            var result = await _familyService.ImportFamiliesAsync(file, HttpContext);

            if (result.result != null) // If an error file is generated
            {
                return File((byte[])result.result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ImportErrors.xlsx");
            }

            return Ok(result.Message);
        }



        [Authorize(Roles = "representative,admin,superadmin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _familyService.DeleteFamilyAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء حذف العائلة", error = ex.Message });
            }
        }
        [Authorize(Roles = "representative,admin,superadmin")]
        [HttpPost]
        public async Task<IActionResult> Activate(string id)
        {
            try
            {
                var result = await _familyService.ActiveFamilyAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء إعادة تفعيل العائلة", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            // Store the previous URL in TempData
            TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

            var result = (await _familyService.GetFamilyDtoByFamiyIdAsync(id, HttpContext));
            if (result.Success)
            {
                var familyDto = (FamilyDto)result.result!;
                return View(familyDto);
            }
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(FamilyDto familyDto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "يجب تسجيل الدخول للاستمرار";
                return View(familyDto);
            }
            if (user.IdNumber.Equals(familyDto.IdNumber) && !familyDto.IsPledge)
            {
                TempData["Error"] = "يجب التعهد بصحة البيانات";
                return View(familyDto);
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(", ", "البيانات غير صالحة", ModelState.Values
                                      .SelectMany(v => v.Errors)
                                      .Select(e => e.ErrorMessage));
                return View(familyDto);
            }

            try
            {
                var result = await _familyService.UpdateFamilyAsync(familyDto, HttpContext);
                if (!result.Success)
                {
                    TempData["Error"] = result.Message;
                    return View(familyDto);
                }
                TempData["Success"] = result.Message;
                var roles = (await _userManager.GetRolesAsync(user)).ToList();
                var family = (Family)result.result!;
                if (roles.Contains(Role.family.ToString()) && family.HusbandId.Equals(user.Id))
                {
                    return RedirectToAction(nameof(MyRequest), new { id = family.Id });
                }
                // Retrieve the previous URL
                string returnUrl = TempData["ReturnUrl"] as string;

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl); // Redirect back to the previous page
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = string.Join(", ", "حدث خطأ أثناء تحديث بيانات الاسرة", ex.Message);
                return View(familyDto);
            }
        }

        [Authorize(Roles = "representative")]
        [HttpPost]
        public async Task<IActionResult> Accept(string id, FinancialSituation financialSituation)
        {
            try
            {
                var result = await _familyService.ChangeStatusFamilyAsync(StatusFamily.accepted, id, HttpContext);
                if (!result.Success)
                    return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
                var resultFinancialSituation = await _familyService.ChangeFinancialSituationyAsync(financialSituation, id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء تحديث حالة طلب الإغاثة", error = ex.Message });
            }
        }

        [Authorize(Roles = "representative")]
        [HttpPost]
        public async Task<IActionResult> Rejected(string id, string message)
        {
            try
            {
                var result = await _familyService.ChangeStatusFamilyAsync(StatusFamily.rejected, id, HttpContext, message);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء تحديث حالة طلب الإغاثة", error = ex.Message });
            }
        }
        [Authorize(Roles = "family")]
        [HttpGet]
        public IActionResult MyRequest(string id)
        {
            return View("MyRequest", id);
        }
        [Authorize(Roles = "family")]
        [HttpPost]
        public async Task<IActionResult> GetMyRequest(string id)
        {
            try
            {
                var result = await _familyService.GetFamilyViewModelByFamiyIdAsync(id, HttpContext);
                return Ok(result.result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء جلب بيانات العائلة", error = ex.Message });
            }
        }

    }
}
