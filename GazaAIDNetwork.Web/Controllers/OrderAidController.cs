using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Services.CycleAidService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Web.Controllers
{
    public class OrderAidController : Controller
    {
        private readonly IOrderAidService _orderAidService;
        public OrderAidController(IOrderAidService orderAidService)
        {
            _orderAidService = orderAidService;
        }
        [Authorize(Roles = "admin,manager")]
        [HttpGet]
        public IActionResult Index(string projectId)
        {
            TempData["projectId"] = projectId;

            return View();
        }

        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> GeAllOrdersAid(string id)
        {
            var data = await _orderAidService.GetAllOrderAidsByProjectAidIdAsync(id, Request, HttpContext);
            return Ok(data.result);
        }

        [Authorize(Roles = "family")]
        [HttpGet]
        public IActionResult MyAids(string familyId)
        {
            TempData["familyId"] = familyId;

            return View();
        }

        [Authorize(Roles = "family")]
        [HttpPost]
        public async Task<IActionResult> GeAllMyAids(string familyId)
        {
            var data = await _orderAidService.GetAllOrderAidsByFamilyIdAsync(familyId, Request, HttpContext);
            return Ok(data.result);
        }


        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Confirm(string id)
        {
            try
            {
                var result = await _orderAidService.UpdateStatusOrderAidAsync(id, OrderAidStatus.Delivered, HttpContext);
                return Json(new { success = result.Success, message = result.Message, data = result.result, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء إعتماد المشروع", error = ex.Message });
            }
        }

    }
}
