using GazaAIDNetwork.Core.Dtos;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazaAIDNetwork.Web.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [Authorize(Roles = "superadmin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }
        [Authorize(Roles = "superadmin,admin")]
        [HttpGet]
        public async Task<IActionResult> IndexByDivision()
        {
            var result = await _userService.GetUserByContextAsync(HttpContext);
            if (result.Success)
            {
                var currentUser = (User)result.result!;
                var users = await _userService.GetAllUsersByDivisionIdAsync(currentUser.DivisionId.ToString());
                return View(users);
            }
            return RedirectToPage("AccessDenied");
        }
        [Authorize(Roles = "superadmin,admin")]
        [HttpGet]
        public async Task<IActionResult> GetUser(string id)
        {
            var result = await _userService.GetUserAsync(id);
            return Json(new { success = result.Success, data = result.result, message = result.Message });
        }
        [Authorize(Roles = "superadmin,admin")]
        // POST: UserController/Create
        [HttpPost]

        public async Task<IActionResult> Create(UserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات المدخلة غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            try
            {
                var result = await _userService.CreateUserAsync(userDto, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "فشل إضافة مستخدم جديد", error = ex.Message });
            }
        }
        [Authorize(Roles = "superadmin,admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(UserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صالحة", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var result = await _userService.UpdateUserAsync(userDto, HttpContext);
                return Json(new { success = result.Success, message = result.Message, errors = result.Errors, data = result.result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء تحديث بيانات المستخدم", error = ex.Message });
            }
        }
        [Authorize(Roles = "superadmin,admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء حذف المستخدم", error = ex.Message });
            }
        }
        [Authorize(Roles = "superadmin,admin")]
        [HttpPost]
        public async Task<IActionResult> Activate(string id)
        {
            try
            {
                var result = await _userService.ReactivateUserAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء إعادة تفعيل المستخدم", error = ex.Message });
            }
        }

        [Authorize(Roles = "superadmin,admin")]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id)
        {
            try
            {
                var result = await _userService.ResetPasswordUserAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء إعادة تعيين كلمة المرور ", error = ex.Message });
            }
        }
        [Authorize(Roles = "superadmin,admin")]
        [HttpPost]
        public async Task<IActionResult> ResetPasswordByFamilyId(string id)
        {
            try
            {
                var result = await _userService.ResetPasswordUserByFamilyIdAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء إعادة تعيين كلمة المرور ", error = ex.Message });
            }
        }
        [Authorize(Roles = "admin,superadmin,manager")]
        [HttpGet]
        public async Task<IActionResult> GetRepresentatives(string divisionId = "")
        {
            var representatives = await _userService.GetAllRepresentativesAsync(divisionId, HttpContext);

            return Json(representatives);
        }
    }
}
