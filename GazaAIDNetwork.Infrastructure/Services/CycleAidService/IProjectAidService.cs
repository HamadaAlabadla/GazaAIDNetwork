using GazaAIDNetwork.Core.Dtos;
using GazaAIDNetwork.Core.Enums;
using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using GazaAIDNetwork.Infrastructure.Services.FamilyService;
using GazaAIDNetwork.Infrastructure.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic.Core;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.Services.CycleAidService
{
    public interface IProjectAidService
    {
        Task<ResultResponse> CreateProjectAidAsync(ProjectAid projectAid, HttpContext httpContext);
        Task<ResultResponse> GetAllProjectsAidAsync(HttpRequest httpRequest, string cycleAidId, HttpContext httpContext);
        Task<ResultResponse> DeleteProjectAidAsync(string projectAidId, HttpContext httpContext);
        Task<ResultResponse> StartProjectAidAsync(string projectAidId, DateTime continuingUntil, HttpContext httpContext);
        Task<ResultResponse> ImportOrdersAidForProjectAsync(IFormFile file, string projectAidId, HttpContext httpContext);
    }

    public class ProjectAidService : IProjectAidService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IRepositoryAudit _repositoryAudit;
        private readonly IFamilyService _familyService;
        private readonly IInfoRepresentativeService _infoRepresentativeService;
        private readonly ICycleAidService _cycleAidService;
        private readonly IOrderAidService _orderAidService;
        public ProjectAidService(ApplicationDbContext context,
            UserManager<User> userManager,
            IRepositoryAudit repositoryAudit,
            IFamilyService familyService,
            IInfoRepresentativeService infoRepresentativeService,
            ICycleAidService cycleAidService,
            IOrderAidService orderAidService)
        {
            _context = context;
            _userManager = userManager;
            _repositoryAudit = repositoryAudit;
            _familyService = familyService;
            _infoRepresentativeService = infoRepresentativeService;
            _cycleAidService = cycleAidService;
            _orderAidService = orderAidService;
        }
        public async Task<ResultResponse> CreateProjectAidAsync(ProjectAid projectAid, HttpContext httpContext)
        {
            if (projectAid == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب إدخال البيانات بشكل صحيح",
                    Errors = new List<string> { "بيانات المشروع غير صالحة" }
                };
            }

            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للإستمرار"
                };

            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));

            try
            {
                projectAid.DivisionId = (Guid)currentUser.DivisionId;
                projectAid.ProjectAidStatus = ProjectAidStatus.UnderPreparation;                

                await _context.ProjectAids.AddAsync(projectAid);
                await _context.SaveChangesAsync(); // ✅ Save ProjectAid first

                var familiesCount = 0;
                var quantity = projectAid.Quantity;

                foreach (string id in projectAid.RepresentativeIds)
                {
                    var families = await _familyService.GetAllFamiliesAsync(httpContext, id);
                    familiesCount += families.Count();
                }

                foreach (string id in projectAid.RepresentativeIds)
                {
                    var families = (await _familyService.GetAllFamiliesAsync(httpContext, id)).Count();
                    double percentage = familiesCount > 0
                                        ? Math.Floor((families * 10000) / (double)familiesCount) / 100
                                        : 0.0;

                    var infoRepresentative = new InfoRepresentative()
                    {
                        Percentage = percentage,
                        ProjectAidId = projectAid.Id,
                        RepresntativeId = id,
                    };

                    if (id.Equals(projectAid.RepresentativeIds.Last()) && percentage > 0)
                        infoRepresentative.Quantity = quantity;
                    else
                        infoRepresentative.Quantity = (int)(projectAid.Quantity * percentage);

                    quantity -= infoRepresentative.Quantity;

                    var resultInfo = await _infoRepresentativeService.CreateInfoRepresentative(infoRepresentative, httpContext);
                    if (!resultInfo.Success)
                    {
                        return new ResultResponse()
                        {
                            Success = false,
                            Message = "خطأ في إضافة حصص المناديب"
                        };
                    }
                }

                // Create an audit log entry
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.ProjectAid,
                    RepoId = projectAid.Id.ToString(),
                    Name = AuditName.Create,
                    CreatedDate = DateTime.UtcNow,
                    Description = "تم إضافة المشروح بنجاح",
                    AdminId = currentUser.Id,
                };

                var result = await _repositoryAudit.CreateAudit(auditLog);
                if (!result.Success)
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = result.Message,
                    };
                }

                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إضافة المشروع بنجاح",
                    result = projectAid
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل إضافة المشروع ",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ResultResponse> GetAllProjectsAidAsync(HttpRequest httpRequest, string cycleAidId, HttpContext httpContext)
        {
            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            var rolesForCurrentUser = (await _userManager.GetRolesAsync(currentUser)).ToList();
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للاستمرار"
                };
            }

            var projectsAid = Enumerable.Empty<ProjectAid>().AsQueryable();
            if (string.IsNullOrEmpty(cycleAidId))
            {
                projectsAid = _context.ProjectAids
                    .Include(x => x.Division)
                    .Include(x => x.OrderAids)
                    .Include(x => x.InfoRepresentatives)
                    .ThenInclude(x => x.Represntative)
                    .Where(x => x.CycleAidId == null && x.DivisionId.ToString().Equals(currentUser.DivisionId.ToString()));
            }
            else
            {
                projectsAid = _context.ProjectAids
                    .Include(x => x.Division)
                    .Include(x => x.OrderAids)
                    .Include(x => x.InfoRepresentatives)
                    .ThenInclude(p => p.Represntative)
                    .Where(x => x.CycleAidId != null && x.DivisionId.ToString().Equals(currentUser.DivisionId.ToString()));
            }

            try
            {
                var draw = httpRequest.Form["draw"].FirstOrDefault();
                var excel = httpRequest.Form["excel"].FirstOrDefault();
                var start = httpRequest.Form["start"].FirstOrDefault();
                var length = httpRequest.Form["length"].FirstOrDefault();
                var sortColumn = httpRequest.Form["columns[" + httpRequest.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = httpRequest.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = httpRequest.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    projectsAid = sortColumn switch
                    {
                        "DivisionName" => sortColumnDirection.ToLower() == "desc"
                            ? projectsAid.OrderByDescending(f => f.Division.Name)
                            : projectsAid.OrderBy(f => f.Division.Name),
                        _ => projectsAid.OrderBy(sortColumn + " " + sortColumnDirection), // Default, no sorting
                    };

                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    projectsAid = projectsAid.Where(m => m.Name.Contains(searchValue)
                                                || m.Descreption.Contains(searchValue)
                                                || m.InfoRepresentatives.Select(x => x.Represntative.FullName).Contains(searchValue));
                }

                recordsTotal = projectsAid.Count();

                var data = projectsAid.Skip(skip).Take(pageSize).ToList();

                var projectsAidViewModels = projectsAid.Select(x => new ProjectAidViewModel()
                {
                    Id = x.Id.ToString(),
                    Name = x.Name,
                    Descreption = x.Descreption,
                    Notes = x.Notes,
                    DivisionName = x.Division.Name,
                    NumberFamilies = x.OrderAids.Count(),
                    ProjectAidStatus = EnumHelper.GetDisplayName(x.ProjectAidStatus),
                    Quantity = x.Quantity,
                    RepresentativeNames = x.InfoRepresentatives.Select(x => x.Represntative.FullName).ToList(),
                    ContinuingUntil = x.ContinuingUntil.ToString("d-M-yyyy HH:mm:ss"),
                    DateCreate = x.DateCreate.ToString("d-M-yyyy HH:mm:ss"),
                })
                .ToList();

                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = projectsAidViewModels };

                return new ResultResponse
                {
                    Success = true,
                    result = jsonData
                };
            }
            catch (Exception ex)
            {
                throw;

            }
        }

        public async Task<ResultResponse> DeleteProjectAidAsync(string projectAidId, HttpContext httpContext)
        {
            var projectAid = await _context.ProjectAids.Include(x => x.OrderAids).FirstOrDefaultAsync(x => x.Id.ToString().Equals(projectAidId));

            if (projectAid == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "المشروع غير موجودة",
                    Errors = new List<string> { "ID المشروع غير صحيح" }
                };
            }
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للإستمرار"
                };
            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));
            if (!currentUser.DivisionId.Equals(projectAid.DivisionId))
                return new ResultResponse()
                {
                    Success = false,
                    Message = "غير مصرح لك بالتعامل مع هذه البيانات"
                };
            if (projectAid.OrderAids.Any(x => x.OrderAidStatus == OrderAidStatus.GoToPickUp))
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "لا يمكن حذف الدورة  , تحتوي بيانات لا يمكن حذفها"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.ProjectAids.Remove(projectAid);
                await _context.SaveChangesAsync();
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.ProjectAid,
                    RepoId = projectAid.Id.ToString(),
                    Name = AuditName.Delete,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = currentUser.Id,
                    Description = "تم حذف المشروع بنجاح"
                };
                var result = await _repositoryAudit.CreateAudit(auditLog);
                if (!result.Success)
                {
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = result.Message,
                    };
                }
                await transaction.CommitAsync();

                return new ResultResponse
                {
                    Success = true,
                    Message = "تم حذف المشروع بنجاح"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل حذف المشروع",
                    Errors = new List<string> { ex.Message }
                };
            }
        }


        public async Task<ResultResponse> ImportOrdersAidForProjectAsync(IFormFile file, string projectAidId, HttpContext httpContext)
        {
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للإستمرار"
                };

            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));
            var validCount = 0;

            if (file == null || file.Length == 0)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "الملف غير موجود أو فارغ."
                };

            var errors = new List<ExcelErrorProjectAid>();

            try
            {
                // Set the LicenseContext to NonCommercial for non-commercial use
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                            return new ResultResponse()
                            {
                                Success = false,
                                Message = "لم يتم العثور على ورقة العمل."
                            };

                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++) // Skip header
                        {
                            var orderAid = new OrderAid();
                            try
                            {
                                Guid.TryParse(projectAidId, out Guid result);
                                orderAid.ProjectAidId = result;
                                orderAid.Quantity = int.TryParse(worksheet.Cells[row, 2].Text?.Trim(), out int quantity) ? quantity : 1;
                                orderAid.OrderAidStatus = OrderAidStatus.Accepted;
                                var familyIdNumber = worksheet.Cells[row, 1].Text?.Trim();
                                var resultFamily = await _familyService.GetFamilyDtoByIdNumberAsync(familyIdNumber, httpContext);
                                if (resultFamily == null || !resultFamily.Success)
                                {
                                    errors.Add(new ExcelErrorProjectAid(row, familyIdNumber, null, string.Join(", ", resultFamily.Message)));
                                    continue;
                                }
                                var family = (FamilyDto)resultFamily.result;
                                Guid.TryParse(family.Id, out Guid familyId);
                                orderAid.FamilyId = familyId;

                                var validationResults = ValidateModel(orderAid);
                                if (validationResults.Any())
                                {
                                    errors.Add(new ExcelErrorProjectAid(row, familyIdNumber, family.HusbandName, string.Join(", ", validationResults)));
                                    continue;
                                }

                                // Save to database
                                var resultCreateOrderAid = await _orderAidService.CreateOrderAidAsync(orderAid, httpContext);
                                if (!resultCreateOrderAid.Success)
                                {
                                    errors.Add(new ExcelErrorProjectAid(row, familyIdNumber, family.HusbandName, resultCreateOrderAid.Message));
                                    continue;
                                }
                                validCount++;
                            }
                            catch (Exception ex)
                            {
                                errors.Add(new ExcelErrorProjectAid(row, null, null, $"Unexpected error: {ex.Message}"));
                            }
                        }
                        if (validCount > 0)
                        {
                            // Create an audit log entry
                            var auditLog = new AuditLog
                            {
                                EntityType = EntityType.ProjectAid,
                                RepoId = projectAidId,
                                Name = AuditName.Update,
                                CreatedDate = DateTime.UtcNow,
                                Description = "تم ترشيح " + validCount + " مستفيدين ضمن هذا المشروع",
                                AdminId = currentUser.Id,
                            };

                            var resultAudit = await _repositoryAudit.CreateAudit(auditLog);
                            if (!resultAudit.Success)
                            {
                                return new ResultResponse
                                {
                                    Success = false,
                                    Message = resultAudit.Message,
                                };
                            }
                        }
                    }
                }


                if (errors.Any())
                {
                    var errorFile = GenerateErrorExcel(errors);
                    return new ResultResponse()
                    {
                        Success = true,
                        Message = "بعض البيانات خطأ . حمل الملف للمزيد من التفاصيل",
                        result = errorFile
                    };
                }

                return new ResultResponse()
                {
                    Success = true,
                    Message = "تم استيراد البيانات بنجاح."
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = $"حدث خطأ أثناء الاستيراد: {ex.Message}"
                };
            }
        }




        private List<string> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, context, validationResults, true);
            return validationResults.Select(vr => vr.ErrorMessage).ToList();
        }


        private byte[] GenerateErrorExcel(List<ExcelErrorProjectAid> errors)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Errors");
                worksheet.Cells[1, 1].Value = "رقم السطر";
                worksheet.Cells[1, 2].Value = "اسم المستفيد";
                worksheet.Cells[1, 3].Value = "رقم الهوية";
                worksheet.Cells[1, 4].Value = "رسالة الخطأ";

                int row = 2;
                foreach (var error in errors)
                {
                    worksheet.Cells[row, 1].Value = error.RowNumber;
                    worksheet.Cells[row, 2].Value = error.Name;
                    worksheet.Cells[row, 3].Value = error.IdNumber;
                    worksheet.Cells[row, 4].Value = error.ErrorMessage;
                    row++;
                }

                return package.GetAsByteArray();
            }
        }

        public async Task<ResultResponse> StartProjectAidAsync(string projectAidId , DateTime continuingUntil, HttpContext httpContext)
        {
            var projectAid = await _context.ProjectAids
                .Include(x => x.OrderAids)
                .Include(x => x.CycleAid)
                .ThenInclude(p => p.ProjectAids)
                .FirstOrDefaultAsync(x => x.Id.ToString().Equals(projectAidId));

            if (projectAid == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "المشروع غير موجودة",
                    Errors = new List<string> { "ID المشروع غير صحيح" }
                };
            }
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للإستمرار"
                };
            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));
            if (!currentUser.DivisionId.Equals(projectAid.DivisionId))
                return new ResultResponse()
                {
                    Success = false,
                    Message = "غير مصرح لك بالتعامل مع هذه البيانات"
                };

            // التحقق من صلاحية تغيير الحالة
            switch (projectAid.ProjectAidStatus)
            {
                case ProjectAidStatus.Confirmed:
                    if (projectAid.ProjectAidStatus != ProjectAidStatus.UnderPreparation)
                        return new ResultResponse { Success = false, Message = "لا يمكن تغيير الحالة إلى الحالة المطلوبة من Pending" };
                    break;
                default:
                    return new ResultResponse { Success = false, Message = "حالة غير صالحة" };
            }

            if (projectAid.OrderAids.Where(x => x.OrderAidStatus == OrderAidStatus.Accepted).Select( x => x.Quantity).Count() != projectAid.Quantity)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "لا يمكن الأعتماد , يجب اختيار كافة الاسماء حسب الكمية المطلوبة"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                projectAid.ProjectAidStatus = ProjectAidStatus.Confirmed;
                TimeZoneInfo palestineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Gaza"); // أو "Asia/Hebron"
                projectAid.DateCreate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, palestineTimeZone);
                projectAid.ContinuingUntil = continuingUntil;
                _context.ProjectAids.Update(projectAid);
                await _context.SaveChangesAsync();
                if(projectAid.CycleAid.ProjectAids.Count() == 1)
                    await _cycleAidService.UpdateStatusCycleAidAsync(projectAid.CycleAidId.ToString() , CycleAidStatus.Start ,httpContext);
                foreach(OrderAid orderAid in projectAid.OrderAids)
                {
                    await _orderAidService.UpdateStatusOrderAidAsync(orderAid.Id.ToString() , OrderAidStatus.GoToPickUp , httpContext);
                }
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.ProjectAid,
                    RepoId = projectAid.Id.ToString(),
                    Name = AuditName.Delete,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = currentUser.Id,
                    Description = "تم إعتماد المشروع والبدء بالتوزيع"
                };
                var result = await _repositoryAudit.CreateAudit(auditLog);
                if (!result.Success)
                {
                    await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = result.Message,
                    };
                }
                await transaction.CommitAsync();

                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إعتماد المشروع بنجاح"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل اعتماد المشروع",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
