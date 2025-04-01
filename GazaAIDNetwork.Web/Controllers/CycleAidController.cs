using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Services.CycleAidService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazaAIDNetwork.Web.Controllers
{
    [Authorize]
    public class CycleAidController : Controller
    {
        private readonly ICycleAidService _cycleAidService;
        public CycleAidController(ICycleAidService cycleAidService)
        {
            _cycleAidService = cycleAidService;
        }

        [Authorize(Roles = "admin,manager")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cycleAidViewModels = await _cycleAidService.GetAllCycleAidsAsync(HttpContext);
            return View(cycleAidViewModels);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]

        public async Task<IActionResult> Create(CycleAid cycleAid)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات المدخلة غير صحيحة", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            try
            {
                var result = await _cycleAidService.CreateCycleAidAsync(cycleAid, HttpContext);
                return Json(new { success = result.Success, message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "فشل بدء دورة توزيع جديدة", error = ex.Message });
            }
        }


        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _cycleAidService.DeleteCycleAidAsync(id, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء حذف الشعبة", error = ex.Message });
            }
        }

    }
}
