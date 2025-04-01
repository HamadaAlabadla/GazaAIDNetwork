using GazaAIDNetwork.Core.Dtos;
using GazaAIDNetwork.Core.Enums;
using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using GazaAIDNetwork.Infrastructure.Services.UserService;
using GazaAIDNetwork.Infrastructure.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic.Core; // Required for dynamic sorting
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.Services.FamilyService
{
    public interface IFamilyService
    {
        Task<ResultResponse> CreateFamilyAsync(FamilyDto familyDto, HttpContext httpContext);
        Task<ResultResponse> UpdateFamilyAsync(FamilyDto familyDto, HttpContext httpContext);
        Task<ResultResponse> DeleteFamilyAsync(string familyId, HttpContext httpContext);
        Task<ResultResponse> ActiveFamilyAsync(string familyId, HttpContext httpContext);
        Task<ResultResponse> GetAllFamiliesAsync(HttpRequest httpRequest, HttpContext httpContext);
        Task<List<Family>> GetAllFamiliesAsync(HttpContext httpContext, string representativeId = "");
        Task<ResultResponse> GetAllDeletedFamiliesAsync(HttpRequest httpRequest, HttpContext httpContext);
        Task<ResultResponse> ImportFamiliesAsync(IFormFile file, HttpContext httpContext);
        Task<ResultResponse> GetFamilyDtoByFamiyIdAsync(string familyId, HttpContext httpContext);
        Task<ResultResponse> GetFamilyDtoByUserIdAsync(string userId, HttpContext httpContext);
        Task<ResultResponse> GetFamilyDtoByIdNumberAsync(string IdNumber, HttpContext httpContext);
        Task<ResultResponse> GetFamilyViewModelByFamiyIdAsync(string familyId, HttpContext httpContext);
        Task<ResultResponse> ChangeStatusFamilyAsync(StatusFamily statusFamily, string familyId, HttpContext httpContext, string message = "");
        Task<ResultResponse> ChangeFinancialSituationyAsync(FinancialSituation financialSituation, string familyId, HttpContext httpContext);
    }

    public class FamilyService : IFamilyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepositoryAudit _repositoryAudit;
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly IAddressService _addressService;
        private readonly IDisabilityService _disabilityService;
        private readonly IDiseaseService _diseaseService;
        private readonly IDisplacedService _displacedService;
        public FamilyService(ApplicationDbContext context,
            IRepositoryAudit repositoryAudit,
            IUserService userService,
            UserManager<User> userManager,
            IAddressService addressService,
            IDisabilityService disabilityService,
            IDiseaseService diseaseService,
            IDisplacedService displacedService)
        {
            _context = context;
            _repositoryAudit = repositoryAudit;
            _userService = userService;
            _userManager = userManager;
            _addressService = addressService;
            _disabilityService = disabilityService;
            _diseaseService = diseaseService;
            _displacedService = displacedService;
        }
        public async Task<ResultResponse> ImportFamiliesAsync(IFormFile file, HttpContext httpContext)
        {
            if (file == null || file.Length == 0)
                return new ResultResponse()
                {
                    Success = false,
                    Message = "الملف غير موجود أو فارغ."
                };

            var errors = new List<ExcelErrorRow>();

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
                            var family = new FamilyDto();
                            try
                            {
                                family.HusbandName = worksheet.Cells[row, 1].Text?.Trim();
                                family.IdNumber = worksheet.Cells[row, 2].Text?.Trim();
                                family.WifeName = worksheet.Cells[row, 3].Text?.Trim();
                                family.WifeIdNumber = worksheet.Cells[row, 4].Text?.Trim();
                                // Read Marital Status and convert it to Enum
                                var maritalStatusText = worksheet.Cells[row, 5].Text?.Trim();
                                // Read Marital Status and convert it to Enum
                                if (MaritalStatusMapping.TryGetValue(maritalStatusText, out var maritalStatus))
                                {
                                    family.MaritalStatus = maritalStatus;
                                }
                                else
                                {
                                    // Handle unknown values (e.g., log an error or set a default value)
                                    family.MaritalStatus = MaritalStatus.Married; // Default value
                                }
                                if (int.TryParse(worksheet.Cells[row, 6].Text?.Trim(), out int numberMembers))
                                {
                                    family.NumberMembers = numberMembers;
                                }
                                else
                                {
                                    family.NumberMembers = 0; // Default value or handle error
                                }

                                family.PhoneNumber = worksheet.Cells[row, 7].Text?.Trim();
                                family.IsDisplaced = worksheet.Cells[row, 10].Text?.Trim() == "نعم";
                                family.CurrentGovernotate = Governotate.Kanyounis;
                                family.CurrentCity = !(worksheet.Cells[row, 11].Text?.Trim()).Equals("مواصي القرارة") ? City.other : City.AlQarara;
                                family.CurrentNeighborhood = (worksheet.Cells[row, 11].Text?.Trim()).Equals("مواصي القرارة") ? Neighborhood.MawasiAlQarara : Neighborhood.CentralRegion;
                                family.DateChangeStatusForHusband = DateTime.UtcNow;
                                family.DateChangeStatusForWife = DateTime.UtcNow;
                                family.GenderForWife = Gender.Female;
                                family.OriginaNeighborhood = Neighborhood.CentralRegion;
                                // Validate ModelState for FamilyDto
                                var validationResults = ValidateModel(family);
                                if (validationResults.Any())
                                {
                                    errors.Add(new ExcelErrorRow(row, family, string.Join(", ", validationResults)));
                                    continue;
                                }

                                // Save to database
                                var result = await CreateFamilyAsync(family, httpContext);
                                if (!result.Success)
                                {
                                    errors.Add(new ExcelErrorRow(row, family, result.Message));
                                }

                            }
                            catch (Exception ex)
                            {
                                errors.Add(new ExcelErrorRow(row, family, $"Unexpected error: {ex.Message}"));
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

        // Define your mapping
        private static readonly Dictionary<string, MaritalStatus> MaritalStatusMapping = new()
        {
            { "أعزب", MaritalStatus.Single },
            { "غير متزوجة", MaritalStatus.Single },
            { "متزوج", MaritalStatus.Married },
            { "مطلقة", MaritalStatus.Divorced },
            { "مطلق", MaritalStatus.Divorced },
            { "ارمل", MaritalStatus.Widowed },
            { "ارملة", MaritalStatus.Widowed },
            { "أرمل", MaritalStatus.Widowed },
            { "أرملة", MaritalStatus.Widowed },
            { "زوجة ثانية", MaritalStatus.SecondWife },
        };

        private byte[] GenerateErrorExcel(List<ExcelErrorRow> errors)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Errors");
                worksheet.Cells[1, 1].Value = "Row Number";
                worksheet.Cells[1, 2].Value = "Husband Name";
                worksheet.Cells[1, 3].Value = "ID Number";
                worksheet.Cells[1, 4].Value = "Phone Number";
                worksheet.Cells[1, 5].Value = "Error Message";

                int row = 2;
                foreach (var error in errors)
                {
                    worksheet.Cells[row, 1].Value = error.RowNumber;
                    worksheet.Cells[row, 2].Value = error.Family.HusbandName;
                    worksheet.Cells[row, 3].Value = error.Family.IdNumber;
                    worksheet.Cells[row, 4].Value = error.Family.PhoneNumber;
                    worksheet.Cells[row, 5].Value = error.ErrorMessage;
                    row++;
                }

                return package.GetAsByteArray();
            }
        }


        private List<string> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, context, validationResults, true);
            return validationResults.Select(vr => vr.ErrorMessage).ToList();
        }


        public async Task<ResultResponse> CreateFamilyAsync(FamilyDto familyDto, HttpContext httpContext)
        {
            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للاستمرار"
                };
            }
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            // Role-based validation for family creation
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can create any family
            }
            else if (currentRoles.Contains(Role.admin.ToString()))
            {
                // Admin can only create family in their own division
                if (!currentUser.Division.Users.Select(x => x.Id).Contains(familyDto.RepresentativeId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "لا يمكن إضافة أسرة لشعبة أخرى"
                    };
                }
            }
            else if (currentRoles.Contains(Role.representative.ToString()))
            {
                familyDto.RepresentativeId = currentUserId;

                // Representative can only create a family within their own division
                if (!currentUser.Id.Equals(familyDto.RepresentativeId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "لا يمكن إضافة أسرة لمندوب آخر"
                    };
                }
            }
            else
            {
                // Other roles cannot create families
                return new ResultResponse
                {
                    Success = false,
                    Message = "أنت غير مصرح لك بإضافة أسرة."
                };
            }


            var representative = await _userService.GetUserAsync(familyDto.RepresentativeId);
            if (!representative.Success)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "خلل في تحديد المندوب"
                };
            }
            var user = new UserDto()
            {
                FullName = familyDto.HusbandName,
                IdNumber = familyDto.IdNumber,
                PhoneNumber = familyDto.PhoneNumber,
                DivisionId = ((UserDto)representative.result).DivisionId,
                Roles = [(int)Role.family],
            };
            var resultCheckIdNumbers = await CheckValidataForIdNumbers(familyDto);
            if (!resultCheckIdNumbers.Success)
                return resultCheckIdNumbers;

            var resultCreateUser = await _userService.CreateUserAsync(user, httpContext);
            if (!resultCreateUser.Success)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = resultCreateUser.Message,
                    Errors = resultCreateUser.Errors
                };
            }
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    // Create the family entity
                    var newFamily = new Family
                    {
                        HusbandId = ((User)resultCreateUser.result).Id,
                        NumberMembers = familyDto.NumberMembers,
                        HusbandStatus = familyDto.HusbandStatus,
                        DateChangeStatusForHusband = familyDto.DateChangeStatusForHusband,
                        GenderForHusband = familyDto.GenderForHusband,
                        WifeName = familyDto.WifeName,
                        WifeIdNumber = familyDto.WifeIdNumber,
                        WifeStatus = familyDto.WifeStatus,
                        DateChangeStatusForWife = familyDto.DateChangeStatusForHusband,
                        GenderForWife = familyDto.GenderForWife,
                        MaritalStatus = familyDto.MaritalStatus,
                        FinancialSituation = FinancialSituation.NotSelected,
                        RepresentativeId = currentUserId,
                        IsDeleted = false,
                        IsPledge = false,
                        StatusFamily = StatusFamily.noRequest,

                    };


                    var originalAddress = new Address()
                    {
                        Governotate = familyDto.OriginalGovernotate,
                        City = familyDto.OriginaCity,
                        Neighborhood = familyDto.OriginaNeighborhood,
                    };
                    var createOriginalAddressResult = await _addressService.CreateAddressForFamilyAsync(originalAddress);
                    if (!createOriginalAddressResult.Success)
                    {
                        await transaction.RollbackAsync();
                        DeleteUserForFamily(familyDto.IdNumber);
                        return new ResultResponse
                        {
                            Success = false,
                            Message = createOriginalAddressResult.Message
                        };
                    }
                    // Create family record in the database
                    newFamily.OriginalAddressId = ((Address)createOriginalAddressResult.result).Id;
                    _context.Families.Add(newFamily);
                    var saveResult = await _context.SaveChangesAsync();
                    if (saveResult <= 0)
                    {
                        await transaction.RollbackAsync();
                        DeleteUserForFamily(familyDto.IdNumber);
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "خلل في إضافة الأسرة"
                        };
                    }
                    if (familyDto.IsDisplaced)
                    {
                        var currentAddress = new Address()
                        {
                            Governotate = (Governotate)familyDto.CurrentGovernotate,
                            City = (City)familyDto.CurrentCity,
                            Neighborhood = (Neighborhood)familyDto.CurrentNeighborhood,
                        };
                        var createcurrentAddressResult = await _addressService.CreateAddressForFamilyAsync(currentAddress);
                        if (!createcurrentAddressResult.Success)
                        {
                            await transaction.RollbackAsync();
                            DeleteUserForFamily(familyDto.IdNumber);
                            return new ResultResponse
                            {
                                Success = false,
                                Message = createcurrentAddressResult.Message
                            };
                        }
                        var displace = new Displace()
                        {
                            CurrentAddressId = ((Address)createcurrentAddressResult.result).Id,
                            FamilyId = newFamily.Id,
                            IsDeleted = false,
                        };
                        var createcurrentDisplaceResult = await _displacedService.CreateDisplaceForFamilyAsync(displace);
                        if (!createcurrentDisplaceResult.Success)
                        {
                            await transaction.RollbackAsync();
                            DeleteUserForFamily(familyDto.IdNumber);
                            return new ResultResponse
                            {
                                Success = false,
                                Message = createcurrentDisplaceResult.Message
                            };
                        }
                    }

                    if (familyDto.IsDisability)
                    {
                        var disability = new Disability()
                        {
                            Hearing = familyDto.Hearing == null ? 0 : (int)familyDto.Hearing,
                            Mental = familyDto.Mental == null ? 0 : (int)familyDto.Mental,
                            Motor = familyDto.Motor == null ? 0 : (int)familyDto.Motor,
                            Visual = familyDto.Visual == null ? 0 : (int)familyDto.Visual,
                            FamilyId = newFamily.Id
                        };
                        var createcurrentDisabilityResult = await _disabilityService.CreateDisabilityAsync(disability);
                        if (!createcurrentDisabilityResult.Success)
                        {
                            await transaction.RollbackAsync();
                            DeleteUserForFamily(familyDto.IdNumber);
                            return new ResultResponse
                            {
                                Success = false,
                                Message = createcurrentDisabilityResult.Message
                            };
                        }
                    }


                    if (familyDto.IsDisease)
                    {
                        var disease = new Disease()
                        {
                            BloodPressure = familyDto.BloodPressure == null ? 0 : (int)familyDto.BloodPressure,
                            Cancer = familyDto.Cancer == null ? 0 : (int)familyDto.Cancer,
                            Diabetes = familyDto.Diabetes == null ? 0 : (int)familyDto.Diabetes,
                            KidneyFailure = familyDto.KidneyFailure == null ? 0 : (int)familyDto.KidneyFailure,
                            FamilyId = newFamily.Id
                        };
                        var createcurrentDiseaseResult = await _diseaseService.CreateDiseaseAsync(disease);
                        if (!createcurrentDiseaseResult.Success)
                        {
                            await transaction.RollbackAsync();
                            DeleteUserForFamily(familyDto.IdNumber);
                            return new ResultResponse
                            {
                                Success = false,
                                Message = createcurrentDiseaseResult.Message
                            };
                        }
                    }

                    // Audit log for family creation
                    var auditLog = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = EntityType.Family,
                        RepoId = newFamily.Id.ToString(),
                        Name = AuditName.Create,
                        Description = "تم إضافة الأسرة بنجاح",
                        CreatedDate = DateTime.UtcNow,
                        AdminId = currentUserId
                    };

                    var auditResult = await _repositoryAudit.CreateAudit(auditLog);
                    if (!auditResult.Success)
                    {
                        await transaction.RollbackAsync();
                        DeleteUserForFamily(familyDto.IdNumber);
                        return new ResultResponse
                        {
                            Success = false,
                            Message = auditResult.Message
                        };
                    }

                    // Commit the transaction if everything is successful
                    await transaction.CommitAsync();

                    return new ResultResponse
                    {
                        Success = true,
                        Message = "تمت إضافة الأسرة بنجاح",
                        result = newFamily
                    };
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync();
                    DeleteUserForFamily(familyDto.IdNumber);
                    return new ResultResponse
                    {
                        Success = false,
                        Message = $"حدث خلل غير متوقع عند إضافة الأسرة: {ex.Message}"
                    };
                }
            }
        }
        public async Task<ResultResponse> UpdateFamilyAsync(FamilyDto familyDto, HttpContext httpContext)
        {
            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للاستمرار"
                };
            }
            var existFamily = await _context.Families
                .Include(x => x.OriginalAddress)
                .Include(x => x.Displace)
                .FirstOrDefaultAsync(x => x.Id.ToString().Equals(familyDto.Id));
            if (existFamily == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = " لم يتم العثور على العائلة المطلوبة"
                };
            }
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            // Role-based validation for family creation
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can create any family
                if ((await _userService.GetAllRepresentativesAsync(currentUser.DivisionId.ToString(), httpContext)).Select(x => x.Id).Contains(familyDto.RepresentativeId)) ;
                existFamily.RepresentativeId = familyDto.RepresentativeId;
                if (existFamily.HusbandId.Equals(currentUser.Id))
                    existFamily.StatusFamily = StatusFamily.pending;
            }
            else if (currentRoles.Contains(Role.admin.ToString()))
            {

                if (!existFamily.Husband.DivisionId.ToString().Equals(currentUser.DivisionId.ToString()))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "لا يمكن تحديث بيانات أسرة لمنطقة أخرى"
                    };
                }
                if ((await _userService.GetAllRepresentativesAsync(currentUser.DivisionId.ToString(), httpContext)).Select(x => x.Id).Contains(familyDto.RepresentativeId)) ;
                existFamily.RepresentativeId = familyDto.RepresentativeId;
                if (existFamily.HusbandId.Equals(currentUser.Id))
                    existFamily.StatusFamily = StatusFamily.pending;
            }
            else if (currentRoles.Contains(Role.representative.ToString()))
            {
                if (currentUser.Id.Equals(existFamily.HusbandId))
                {
                    existFamily.StatusFamily = StatusFamily.pending;
                }
                // Representative can only create a family within their own division
                if (!currentUser.Id.Equals(familyDto.RepresentativeId) && !currentUser.Id.Equals(existFamily.HusbandId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "لا يمكن تحديث بيانات أسرة لمندوب آخر"
                    };
                }
            }
            else
            {

                // Other roles cannot create families
                if (!currentUser.IdNumber.Equals(familyDto.IdNumber))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "لا يمكن تحديث بيانات أسرة غير أسرتك"
                    };
                }
                existFamily.StatusFamily = StatusFamily.pending;

            }


            var representative = await _userService.GetUserAsync(familyDto.RepresentativeId);
            if (!representative.Success)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "خلل في تحديد المندوب"
                };
            }
            var husband = existFamily.Husband;
            var rolesHusband = (await _userManager.GetRolesAsync(husband)).ToList();
            var user = new UserDto()
            {
                Id = existFamily.HusbandId,
                FullName = familyDto.HusbandName,
                IdNumber = familyDto.IdNumber,
                PhoneNumber = familyDto.PhoneNumber,
                DivisionId = ((UserDto)representative.result).DivisionId,
                Roles = rolesHusband.Select(x => (int)((Role)Enum.Parse(typeof(Role), x))).ToArray().Append((int)Role.family).ToArray(),
            };
            var resultCheckIdNumbers = await CheckValidataForUpdateIdNumbers(familyDto);
            if (!resultCheckIdNumbers.Success)
                return resultCheckIdNumbers;

            var resultUpdateUser = await _userService.UpdateUserAsync(user, httpContext);
            if (!resultUpdateUser.Success)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = resultUpdateUser.Message,
                    Errors = resultUpdateUser.Errors
                };
            }
            //using (var transaction = await _context.Database.BeginTransactionAsync())
            //{
            try
            {
                existFamily.HusbandStatus = familyDto.HusbandStatus;
                existFamily.DateChangeStatusForHusband = familyDto.DateChangeStatusForHusband;
                existFamily.GenderForHusband = familyDto.GenderForHusband;
                existFamily.WifeName = familyDto.WifeName;
                existFamily.WifeIdNumber = familyDto.WifeIdNumber;
                existFamily.WifeStatus = familyDto.WifeStatus;
                existFamily.DateChangeStatusForWife = familyDto.DateChangeStatusForWife;
                existFamily.GenderForWife = familyDto.GenderForWife;
                existFamily.MaritalStatus = familyDto.MaritalStatus;
                existFamily.IsPledge = familyDto.IsPledge;
                existFamily.NumberMembers = familyDto.NumberMembers;


                var originalAddress = new Address()
                {
                    Governotate = familyDto.OriginalGovernotate,
                    City = familyDto.OriginaCity,
                    Neighborhood = familyDto.OriginaNeighborhood,
                };
                var updateOriginalAddressResult = await _addressService.UpdateAddressForFamilyAsync(originalAddress, existFamily.OriginalAddressId);
                if (!updateOriginalAddressResult.Success)
                {
                    //await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = updateOriginalAddressResult.Message
                    };
                }
                if (familyDto.IsDisplaced)
                {
                    var existDisplace = _context.Displaces
                                                .Include(x => x.CurrentAddress)
                                                .FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id) && !x.IsDeleted);

                    if (existDisplace == null)
                    {
                        //await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = "لا يمكن تغيير الحالة إلى نازح"
                        };
                    }
                    var currentAddress = new Address()
                    {
                        Governotate = (Governotate)familyDto.CurrentGovernotate,
                        City = (City)familyDto.CurrentCity,
                        Neighborhood = (Neighborhood)familyDto.CurrentNeighborhood,
                    };
                    var updatecurrentAddressResult = await _addressService.UpdateAddressForFamilyAsync(currentAddress, existDisplace.CurrentAddressId);
                    if (!updatecurrentAddressResult.Success)
                    {
                        //await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = updatecurrentAddressResult.Message
                        };
                    }
                }
                else
                {
                    var displace = _context.Displaces.FirstOrDefault(x => x.FamilyId == existFamily.Id && !x.IsDeleted);
                    if (displace != null)
                    {
                        displace.IsDeleted = true;
                        _context.Displaces.Update(displace);
                        _context.SaveChanges();
                    }
                }

                if (familyDto.IsDisability)
                {
                    var disability = new Disability()
                    {
                        Hearing = familyDto.Hearing == null ? 0 : (int)familyDto.Hearing,
                        Mental = familyDto.Mental == null ? 0 : (int)familyDto.Mental,
                        Motor = familyDto.Motor == null ? 0 : (int)familyDto.Motor,
                        Visual = familyDto.Visual == null ? 0 : (int)familyDto.Visual,
                        FamilyId = existFamily.Id
                    };
                    var createcurrentDisabilityResult = await _disabilityService.CreateDisabilityAsync(disability);
                    if (!createcurrentDisabilityResult.Success)
                    {
                        //await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = createcurrentDisabilityResult.Message
                        };
                    }
                }
                else
                {
                    var disability = _context.Disabilities.FirstOrDefault(x => x.FamilyId == existFamily.Id && !x.IsDelete);
                    if (disability != null)
                    {
                        disability.IsDelete = true;
                        _context.Disabilities.Update(disability);
                        _context.SaveChanges();
                    }
                }


                if (familyDto.IsDisease)
                {
                    var disease = new Disease()
                    {
                        BloodPressure = familyDto.BloodPressure == null ? 0 : (int)familyDto.BloodPressure,
                        Cancer = familyDto.Cancer == null ? 0 : (int)familyDto.Cancer,
                        Diabetes = familyDto.Diabetes == null ? 0 : (int)familyDto.Diabetes,
                        KidneyFailure = familyDto.KidneyFailure == null ? 0 : (int)familyDto.KidneyFailure,
                        FamilyId = existFamily.Id
                    };
                    var createcurrentDiseaseResult = await _diseaseService.CreateDiseaseAsync(disease);
                    if (!createcurrentDiseaseResult.Success)
                    {
                        //await transaction.RollbackAsync();
                        return new ResultResponse
                        {
                            Success = false,
                            Message = createcurrentDiseaseResult.Message
                        };
                    }
                }
                else
                {
                    var disease = _context.Diseases.FirstOrDefault(x => x.FamilyId == existFamily.Id && !x.IsDelete);
                    if (disease != null)
                    {
                        disease.IsDelete = true;
                        _context.Diseases.Update(disease);
                        _context.SaveChanges();
                    }
                }

                // Create family record in the database
                _context.Families.Update(existFamily);
                var saveResult = await _context.SaveChangesAsync();
                if (saveResult <= 0)
                {
                    //await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "خلل في تحديث بيانات الأسرة"
                    };
                }
                // Audit log for family creation
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityType = EntityType.Family,
                    RepoId = existFamily.Id.ToString(),
                    Name = AuditName.Update,
                    Description = "تم تحديث بيانات الأسرة بنجاح",
                    CreatedDate = DateTime.UtcNow,
                    AdminId = currentUserId
                };


                var auditResult = await _repositoryAudit.CreateAudit(auditLog);
                if (!auditResult.Success)
                {
                    //await transaction.RollbackAsync();
                    return new ResultResponse
                    {
                        Success = false,
                        Message = auditResult.Message
                    };
                }

                // Commit the transaction if everything is successful
                //await transaction.CommitAsync();

                return new ResultResponse
                {
                    Success = true,
                    Message = "تمت تحديث بيانات الأسرة بنجاح",
                    result = existFamily
                };
            }
            catch (Exception ex)
            {
                // Rollback the transaction in case of an error
                //await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خلل غير متوقع عند تحديث بيانات الأسرة: {ex.Message}"
                };
            }
        }
        //}
        private async void DeleteUserForFamily(string idNumber)
        {
            var user = await _userManager.FindByNameAsync(idNumber);
            if (user != null)
            {
                var roles = (await _userManager.GetRolesAsync(user)).ToList();
                if (roles != null && roles.Contains(Role.family.ToString()))
                {
                    await _userManager.RemoveFromRoleAsync(user, Role.family.ToString());
                }
                roles = (await _userManager.GetRolesAsync(user)).ToList();
                if (!(roles.Count() > 0))
                    await _userManager.DeleteAsync(user);
            }
        }
        private async Task<ResultResponse> CheckValidataForIdNumbers(FamilyDto familyDto)
        {
            var checkIdNumberInIdNumber = await _context.Users.FirstOrDefaultAsync(x => x.IdNumber.Equals(familyDto.IdNumber));

            if (checkIdNumberInIdNumber != null)
            {
                var roleForThisFamily = (await _userManager.GetRolesAsync(checkIdNumberInIdNumber)).ToList();
                if (roleForThisFamily != null && roleForThisFamily.Contains(Role.family.ToString()))
                    return new ResultResponse()
                    {
                        Success = false,
                        Message = "رقم هوية الزوج موجود بالفعل ",
                    };
            }
            var checkIdNumberInWifeIdNumber = await _context.Families.FirstOrDefaultAsync(x => !string.IsNullOrEmpty(familyDto.WifeIdNumber) && x.WifeIdNumber.Equals(familyDto.IdNumber));
            if (checkIdNumberInWifeIdNumber != null)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "رقم هوية الزوج موجود بالفعل ك رقم هوية الزوجة",
                };
            }
            var checkWifeIdNumberInIdNumber = await _context.Users.FirstOrDefaultAsync(u => u.IdNumber.Equals(familyDto.WifeIdNumber));
            if (checkWifeIdNumberInIdNumber != null)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "رقم هوية الزوجة مسجل بالفعل ك رقم هوية الزوج",
                };
            }
            var checkWifeIdNumberInWifeIdNumber = _context.Families.Where(x => !string.IsNullOrEmpty(familyDto.WifeIdNumber) && x.WifeIdNumber.Equals(familyDto.WifeIdNumber)).ToList();

            if (checkWifeIdNumberInWifeIdNumber.Count() != 0 && familyDto.GenderForWife == Gender.Female)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "رقم هوية الزوجة موجود بالفعل",
                };

            }
            if (checkWifeIdNumberInWifeIdNumber != null && familyDto.GenderForWife == Gender.Male && checkWifeIdNumberInWifeIdNumber.Count() >= 4)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "لا يمكن إضافة أكثر من 4 زوجات",
                };
            }

            return new ResultResponse()
            {
                Success = true,
                Message = "تم التحقق من ارقام الهوايا"
            };
        }

        private async Task<ResultResponse> CheckValidataForUpdateIdNumbers(FamilyDto familyDto)
        {
            var family = await _context.Families.Include(x => x.Husband).FirstOrDefaultAsync(x => x.Id.ToString().Equals(familyDto.Id));
            var checkIdNumberInIdNumber = await _context.Users.FirstOrDefaultAsync(x => x.IdNumber.Equals(familyDto.IdNumber));

            if (checkIdNumberInIdNumber != null && !family.Husband.IdNumber.Equals(familyDto.IdNumber))
            {
                var roleForThisFamily = (await _userManager.GetRolesAsync(checkIdNumberInIdNumber)).ToList();
                if (roleForThisFamily != null && roleForThisFamily.Contains(Role.family.ToString()))
                    return new ResultResponse()
                    {
                        Success = false,
                        Message = "رقم هوية الزوج موجود بالفعل ",
                    };
            }
            var checkIdNumberInWifeIdNumber = await _context.Families.FirstOrDefaultAsync(x => !string.IsNullOrEmpty(familyDto.WifeIdNumber) && x.WifeIdNumber.Equals(familyDto.IdNumber));
            if (checkIdNumberInWifeIdNumber != null)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "رقم هوية الزوج موجود بالفعل ك رقم هوية الزوجة",
                };
            }
            var checkWifeIdNumberInIdNumber = await _context.Users.FirstOrDefaultAsync(u => u.IdNumber.Equals(familyDto.WifeIdNumber));
            if (checkWifeIdNumberInIdNumber != null)
            {
                return new ResultResponse()
                {
                    Success = false,
                    Message = "رقم هوية الزوجة مسجل بالفعل ك رقم هوية الزوج",
                };
            }
            var checkWifeIdNumberInWifeIdNumber = _context.Families.Where(x => !string.IsNullOrEmpty(familyDto.WifeIdNumber) && x.WifeIdNumber.Equals(familyDto.WifeIdNumber)).ToList();

            if (checkWifeIdNumberInWifeIdNumber.Count() != 0 && familyDto.GenderForWife == Gender.Female)
            {
                if (!family.WifeIdNumber.Equals(familyDto.WifeIdNumber))
                    return new ResultResponse()
                    {
                        Success = false,
                        Message = "رقم هوية الزوجة موجود بالفعل",
                    };

            }
            if (checkWifeIdNumberInWifeIdNumber.Count() != 0 && familyDto.GenderForWife == Gender.Male && checkWifeIdNumberInWifeIdNumber.Count() >= 4)
            {
                if (!checkWifeIdNumberInWifeIdNumber.Select(x => x.Id).ToList().Contains(family.Id))
                    return new ResultResponse()
                    {
                        Success = false,
                        Message = "لا يمكن إضافة أكثر من 4 زوجات",
                    };
            }

            return new ResultResponse()
            {
                Success = true,
                Message = "تم التحقق من ارقام الهوايا"
            };
        }
        public async Task<ResultResponse> GetAllFamiliesAsync(HttpRequest httpRequest, HttpContext httpContext)
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

            var families = Enumerable.Empty<Family>().AsQueryable();
            if (rolesForCurrentUser.Contains(Role.superadmin.ToString()))
                families = _context.Families.Include(x => x.Husband)
                    .Include(x => x.OriginalAddress)
                    .Include(x => x.Disability)
                    .Include(x => x.Disease)
                    .Include(x => x.Displace)
                    .Include(x => x.Representative)
                    .Where(x => !x.IsDeleted);
            else if (rolesForCurrentUser.Contains(Role.admin.ToString()) || rolesForCurrentUser.Contains(Role.manager.ToString()))
                families = _context.Families.Include(x => x.Husband)
                    .Include(x => x.OriginalAddress)
                    .Include(x => x.Disability)
                    .Include(x => x.Disease)
                    .Include(x => x.Displace)
                    .Include(x => x.Representative)
                    .Where(x => !x.IsDeleted && x.Husband.DivisionId.ToString().Equals(currentUser.DivisionId.ToString()));
            else if (rolesForCurrentUser.Contains(Role.representative.ToString()))
                families = _context.Families.Include(x => x.Husband)
                    .Include(x => x.OriginalAddress)
                    .Include(x => x.Disability)
                    .Include(x => x.Disease)
                    .Include(x => x.Displace)
                    .Include(x => x.Representative)
                    .Where(x => !x.IsDeleted && x.RepresentativeId.Equals(currentUser.Id));
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
                bool.TryParse(excel, out bool isExcel);

                var filterOrder = httpRequest.Form["filterOrder"].FirstOrDefault()?.Split(",") ?? new string[0];

                var filters = new Dictionary<string, string>();
                foreach (var key in httpRequest.Form.Keys)
                {
                    if (key.StartsWith("filter[") && key.EndsWith("]"))
                    {
                        var filterName = key.Substring(7, key.Length - 8);
                        var filterValue = httpRequest.Form[key].FirstOrDefault();
                        if (!string.IsNullOrEmpty(filterValue))
                        {
                            filters[filterName] = filterValue;
                        }
                    }
                }

                int recordsTotal = 0;

                // ✅ Apply Filters in Order of Selection
                foreach (var filter in filterOrder)
                {
                    if (!filters.ContainsKey(filter)) continue;

                    var value = filters[filter];
                    switch (filter)
                    {
                        case "IsDisplaced":
                            if (bool.TryParse(value, out bool result))
                            {
                                if (!result)
                                    families = families.Where(f => !_context.Displaces.Any(d => d.FamilyId == f.Id) ||
                                                                    _context.Displaces.Any(d => d.FamilyId == f.Id && d.IsDeleted));
                                else
                                    families = families.Where(f => _context.Displaces.Any(d => d.FamilyId == f.Id && !d.IsDeleted));

                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "FinancialSituation":
                            if (Enum.TryParse(value, out FinancialSituation resultFinancialSituation))
                            {
                                families = families.Where(f => f.FinancialSituation == resultFinancialSituation);
                            }
                            else
                            {
                                Console.WriteLine("Invalid Enum Value");
                            }
                            break;
                        case "IsDisability":
                            if (bool.TryParse(value, out bool resultIsDisability))
                            {
                                if (!resultIsDisability)
                                    families = families.Where(f => !_context.Disabilities.Any(d => d.FamilyId == f.Id) ||
                                                                    _context.Disabilities.Any(d => d.FamilyId == f.Id && d.IsDelete));
                                else
                                    families = families.Where(f => _context.Disabilities.Any(d => d.FamilyId == f.Id && !d.IsDelete));
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "IsDisease":
                            if (bool.TryParse(value, out bool resultIsDisease))
                            {
                                if (!resultIsDisease)
                                    families = families.Where(f => !_context.Diseases.Any(d => d.FamilyId == f.Id) ||
                                                                    _context.Diseases.Any(d => d.FamilyId == f.Id && d.IsDelete));
                                else
                                    families = families.Where(f => _context.Diseases.Any(d => d.FamilyId == f.Id && !d.IsDelete));
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "familyStatus":
                            if (Enum.TryParse(value, out StatusFamily statusFamily))
                            {
                                families = families.Where(f => f.StatusFamily == statusFamily);
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "divisionId":
                            if (!string.IsNullOrEmpty(value))
                            {
                                families = families.Where(f => f.Husband.DivisionId.ToString().Equals(value));
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "representativeId":
                            if (!string.IsNullOrEmpty(value))
                            {
                                families = families.Where(f => f.RepresentativeId.Equals(value));
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                            // Add more filters...
                    }
                }
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    families = sortColumn switch
                    {
                        "FullName" => sortColumnDirection.ToLower() == "desc"
                            ? families.OrderByDescending(f => f.Husband.FullName)
                            : families.OrderBy(f => f.Husband.FullName),
                        "IdNumber" => sortColumnDirection.ToLower() == "desc"
                            ? families.OrderByDescending(f => f.Husband.IdNumber)
                            : families.OrderBy(f => f.Husband.IdNumber),
                        "NumberMembers" => sortColumnDirection.ToLower() == "desc"
                            ? families.OrderByDescending(f => f.NumberMembers).ThenBy(f => f.Husband.FullName)
                            : families.OrderBy(f => f.NumberMembers).ThenBy(f => f.Husband.FullName),
                        _ => families.OrderBy(sortColumn + " " + sortColumnDirection), // Default, no sorting
                    };
                }
                if (!string.IsNullOrEmpty(searchValue))
                {
                    families = families.Where(m => m.Husband.FullName.Contains(searchValue)
                                                || m.Husband.IdNumber.Contains(searchValue)
                                                || m.WifeName.Contains(searchValue)
                                                || m.WifeIdNumber.Contains(searchValue));
                }
                recordsTotal = families.Count();
                if (isExcel)
                {
                    pageSize = recordsTotal;
                    skip = 0;
                }
                var data = families.Skip(skip).Take(pageSize).ToList();
                var dataViewModels = data.Select(x => new FamilyViewModel
                {
                    Id = x.Id.ToString(),
                    HusbandName = x.Husband.FullName,
                    IdNumber = x.Husband.IdNumber,
                    PhoneNumber = x.Husband.PhoneNumber,
                    WifeName = x.WifeName,
                    WifeIdNumber = x.WifeIdNumber,
                    NumberMembers = x.NumberMembers,
                    DateChangeStatusForHusband = x.DateChangeStatusForHusband.ToString("d-M-yyyy"),
                    DateChangeStatusForWife = x.DateChangeStatusForWife.ToString("d-M-yyyy"),
                    FinancialSituation = EnumHelper.GetDisplayName(x.FinancialSituation),
                    GenderForHusband = EnumHelper.GetDisplayName(x.GenderForHusband),
                    GenderForWife = EnumHelper.GetDisplayName(x.GenderForWife),
                    HusbandStatus = EnumHelper.GetDisplayName(x.HusbandStatus),
                    MaritalStatus = EnumHelper.GetDisplayName(x.MaritalStatus),
                    OriginaCity = x.OriginalAddress.City,
                    OriginalGovernotate = x.OriginalAddress.Governotate,
                    OriginaNeighborhood = x.OriginalAddress.Neighborhood,
                    RepresentativeName = x.Representative.FullName,
                    WifeStatus = EnumHelper.GetDisplayName(x.WifeStatus),
                    StatusFamily = EnumHelper.GetDisplayName(x.StatusFamily) // تحويل Enum إلى نص
                });
                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = dataViewModels };

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
        public async Task<ResultResponse> GetAllDeletedFamiliesAsync(HttpRequest httpRequest, HttpContext httpContext)
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

            var families = Enumerable.Empty<Family>().AsQueryable();
            if (rolesForCurrentUser.Contains(Role.superadmin.ToString()))
                families = _context.Families.Include(x => x.Husband)
                    .Include(x => x.OriginalAddress)
                    .Include(x => x.Disability)
                    .Include(x => x.Disease)
                    .Include(x => x.Displace)
                    .Include(x => x.Representative)
                    .Where(x => x.IsDeleted);
            else if (rolesForCurrentUser.Contains(Role.admin.ToString()) || rolesForCurrentUser.Contains(Role.manager.ToString()))
                families = _context.Families.Include(x => x.Husband)
                    .Include(x => x.OriginalAddress)
                    .Include(x => x.Disability)
                    .Include(x => x.Disease)
                    .Include(x => x.Displace)
                    .Include(x => x.Representative)
                    .Where(x => x.IsDeleted && x.Husband.DivisionId.ToString().Equals(currentUser.DivisionId.ToString()));
            else if (rolesForCurrentUser.Contains(Role.representative.ToString()))
                families = _context.Families.Include(x => x.Husband)
                    .Include(x => x.OriginalAddress)
                    .Include(x => x.Disability)
                    .Include(x => x.Disease)
                    .Include(x => x.Displace)
                    .Include(x => x.Representative)
                    .Where(x => x.IsDeleted && x.RepresentativeId.Equals(currentUser.Id));
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
                bool.TryParse(excel, out bool isExcel);

                var filterOrder = httpRequest.Form["filterOrder"].FirstOrDefault()?.Split(",") ?? new string[0];

                var filters = new Dictionary<string, string>();
                foreach (var key in httpRequest.Form.Keys)
                {
                    if (key.StartsWith("filter[") && key.EndsWith("]"))
                    {
                        var filterName = key.Substring(7, key.Length - 8);
                        var filterValue = httpRequest.Form[key].FirstOrDefault();
                        if (!string.IsNullOrEmpty(filterValue))
                        {
                            filters[filterName] = filterValue;
                        }
                    }
                }

                int recordsTotal = 0;

                // ✅ Apply Filters in Order of Selection
                foreach (var filter in filterOrder)
                {
                    if (!filters.ContainsKey(filter)) continue;

                    var value = filters[filter];
                    switch (filter)
                    {
                        case "IsDisplaced":
                            if (bool.TryParse(value, out bool result))
                            {
                                if (!result)
                                    families = families.Where(f => !_context.Displaces.Any(d => d.FamilyId == f.Id) ||
                                                                    _context.Displaces.Any(d => d.FamilyId == f.Id && d.IsDeleted));
                                else
                                    families = families.Where(f => _context.Displaces.Any(d => d.FamilyId == f.Id && !d.IsDeleted));

                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "FinancialSituation":
                            if (Enum.TryParse(value, out FinancialSituation resultFinancialSituation))
                            {
                                families = families.Where(f => f.FinancialSituation == resultFinancialSituation);
                            }
                            else
                            {
                                Console.WriteLine("Invalid Enum Value");
                            }
                            break;
                        case "IsDisability":
                            if (bool.TryParse(value, out bool resultIsDisability))
                            {
                                if (!resultIsDisability)
                                    families = families.Where(f => !_context.Disabilities.Any(d => d.FamilyId == f.Id) ||
                                                                    _context.Disabilities.Any(d => d.FamilyId == f.Id && d.IsDelete));
                                else
                                    families = families.Where(f => _context.Disabilities.Any(d => d.FamilyId == f.Id && !d.IsDelete));
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "IsDisease":
                            if (bool.TryParse(value, out bool resultIsDisease))
                            {
                                if (!resultIsDisease)
                                    families = families.Where(f => !_context.Diseases.Any(d => d.FamilyId == f.Id) ||
                                                                    _context.Diseases.Any(d => d.FamilyId == f.Id && d.IsDelete));
                                else
                                    families = families.Where(f => _context.Diseases.Any(d => d.FamilyId == f.Id && !d.IsDelete));
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "familyStatus":
                            if (Enum.TryParse(value, out StatusFamily statusFamily))
                            {
                                families = families.Where(f => f.StatusFamily == statusFamily);
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "divisionId":
                            if (!string.IsNullOrEmpty(value))
                            {
                                families = families.Where(f => f.Husband.DivisionId.ToString().Equals(value));
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                        case "representativeId":
                            if (!string.IsNullOrEmpty(value))
                            {
                                families = families.Where(f => f.RepresentativeId.Equals(value));
                            }
                            else
                            {
                                Console.WriteLine("Invalid boolean string");
                            }
                            break;
                            // Add more filters...
                    }
                }
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    families = sortColumn switch
                    {
                        "FullName" => sortColumnDirection.ToLower() == "desc"
                            ? families.OrderByDescending(f => f.Husband.FullName)
                            : families.OrderBy(f => f.Husband.FullName),
                        "IdNumber" => sortColumnDirection.ToLower() == "desc"
                            ? families.OrderByDescending(f => f.Husband.IdNumber)
                            : families.OrderBy(f => f.Husband.IdNumber),
                        "NumberMembers" => sortColumnDirection.ToLower() == "desc"
                            ? families.OrderByDescending(f => f.NumberMembers).ThenBy(f => f.Husband.FullName)
                            : families.OrderBy(f => f.NumberMembers).ThenBy(f => f.Husband.FullName),
                        _ => families.OrderBy(sortColumn + " " + sortColumnDirection), // Default, no sorting
                    };
                }
                if (!string.IsNullOrEmpty(searchValue))
                {
                    families = families.Where(m => m.Husband.FullName.Contains(searchValue)
                                                || m.Husband.IdNumber.Contains(searchValue)
                                                || m.WifeName.Contains(searchValue)
                                                || m.WifeIdNumber.Contains(searchValue));
                }
                recordsTotal = families.Count();
                if (isExcel)
                {
                    pageSize = recordsTotal;
                    skip = 0;
                }
                var data = families.Skip(skip).Take(pageSize).ToList();
                var dataViewModels = data.Select(x => new FamilyViewModel
                {
                    Id = x.Id.ToString(),
                    HusbandName = x.Husband.FullName,
                    IdNumber = x.Husband.IdNumber,
                    PhoneNumber = x.Husband.PhoneNumber,
                    WifeName = x.WifeName,
                    WifeIdNumber = x.WifeIdNumber,
                    NumberMembers = x.NumberMembers,
                    DateChangeStatusForHusband = x.DateChangeStatusForHusband.ToString("d-M-yyyy"),
                    DateChangeStatusForWife = x.DateChangeStatusForWife.ToString("d-M-yyyy"),
                    FinancialSituation = EnumHelper.GetDisplayName(x.FinancialSituation),
                    GenderForHusband = EnumHelper.GetDisplayName(x.GenderForHusband),
                    GenderForWife = EnumHelper.GetDisplayName(x.GenderForWife),
                    HusbandStatus = EnumHelper.GetDisplayName(x.HusbandStatus),
                    MaritalStatus = EnumHelper.GetDisplayName(x.MaritalStatus),
                    OriginaCity = x.OriginalAddress.City,
                    OriginalGovernotate = x.OriginalAddress.Governotate,
                    OriginaNeighborhood = x.OriginalAddress.Neighborhood,
                    RepresentativeName = x.Representative.FullName,
                    WifeStatus = EnumHelper.GetDisplayName(x.WifeStatus),
                    StatusFamily = EnumHelper.GetDisplayName(x.StatusFamily) // تحويل Enum إلى نص
                });
                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = dataViewModels };

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

        public async Task<ResultResponse> DeleteFamilyAsync(string familyId, HttpContext httpContext)
        {
            var family = await _context.Families.Include(x => x.Husband).FirstOrDefaultAsync(x => x.Id.ToString().Equals(familyId));
            var admin = await _userManager.GetUserAsync(httpContext.User);
            var rolesAdmin = (await _userManager.GetRolesAsync(admin)).ToList();
            if (rolesAdmin.Contains(Role.superadmin.ToString()))
            {

            }
            else if (rolesAdmin.Contains(Role.admin.ToString()))
            {
                if (!family.Husband.DivisionId.Equals(admin.DivisionId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بحذف العائلات خارج منطقتك",
                    };
                }
            }
            else
            {
                if (!family.RepresentativeId.Equals(admin.Id))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بحذف العائلات خارج مربعك",
                    };
                }
            }
            if (family == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة",
                    Errors = new List<string> { "ID العائلة غير صحيح" }
                };
            }

            if (family.IsDeleted)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "تم حذف العائلة مسبقا"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Mark as deleted instead of removing
                family.IsDeleted = true;
                await _userManager.RemoveFromRoleAsync(family.Husband, Role.family.ToString());
                _context.Families.Update(family);
                await _context.SaveChangesAsync();
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.Family,
                    RepoId = family.Id.ToString(),
                    Name = AuditName.Delete,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = admin.Id,
                    Description = "تم حذف العائلة بنجاح"
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
                    Message = "تم حذف العائلة بنجاح"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل حذف العائلة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }



        public async Task<ResultResponse> ActiveFamilyAsync(string familyId, HttpContext httpContext)
        {
            var family = await _context.Families.Include(x => x.Husband).FirstOrDefaultAsync(x => x.Id.ToString().Equals(familyId));
            var admin = await _userManager.GetUserAsync(httpContext.User);
            var rolesAdmin = (await _userManager.GetRolesAsync(admin)).ToList();
            if (rolesAdmin.Contains(Role.superadmin.ToString()))
            {

            }
            else if (rolesAdmin.Contains(Role.admin.ToString()))
            {
                if (!family.Husband.DivisionId.Equals(admin.DivisionId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بتفعسل العائلات خارج منطقتك",
                    };
                }
            }
            else
            {
                if (!family.RepresentativeId.Equals(admin.Id))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بتفعيل العائلات خارج مربعك",
                    };
                }
            }
            if (family == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة",
                    Errors = new List<string> { "Id الخاص بالعائلة غير متاح" }
                };
            }

            // Check if division is already active (not deleted)
            if (!family.IsDeleted)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة فعالة مسبقا",
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Reactivate division by setting IsDeleted to false
                family.IsDeleted = false;
                await _userManager.AddToRoleAsync(family.Husband, Role.family.ToString());
                _context.Families.Update(family);
                await _context.SaveChangesAsync();
                // Add audit log for activation
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.Family,
                    RepoId = family.Id.ToString(),
                    Name = AuditName.ReActive,
                    CreatedDate = DateTime.UtcNow,
                    AdminId = admin.Id,
                    Description = "تم إعادة تفعيل العائلة بنجاح"
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
                    Message = "تم إعادة تفعيل العائلة بنجاح",
                    result = family,

                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل إعادة تفعيل العائلة",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ResultResponse> GetFamilyDtoByFamiyIdAsync(string familyId, HttpContext httpContext)
        {

            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            var family = await _context.Families
                .Include(x => x.Husband)
                .ThenInclude(o => o.Division)
                .Include(x => x.OriginalAddress)
                .Include(x => x.Disability)
                .Include(x => x.Displace)
                .Include(x => x.Disease)
                .FirstOrDefaultAsync(x => x.Id.ToString().Equals(familyId));
            if (family == null)
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات العائلة غير مسجلة"
                };
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للاستمرار"
                };
            }
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            // Role-based validation for family creation
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can create any family
            }
            else if (currentRoles.Contains(Role.admin.ToString()) || currentRoles.Contains(Role.manager.ToString()))
            {
                // Admin can only create family in their own division
                if (!currentUser.DivisionId.Equals(family.Husband.DivisionId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            else if (currentRoles.Contains(Role.representative.ToString()))
            {
                // Representative can only create a family within their own division
                if (!currentUser.Id.Equals(family.HusbandId) && !currentUser.Id.Equals(family.RepresentativeId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            else
            {
                if (!currentUser.Id.Equals(family.HusbandId))
                {
                    // Other roles cannot create families
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            var familyDto = new FamilyDto
            {
                Id = family.Id.ToString(),
                HusbandName = family.Husband.FullName,
                IdNumber = family.Husband.IdNumber,
                HusbandStatus = family.HusbandStatus,
                DateChangeStatusForHusband = family.DateChangeStatusForHusband,
                GenderForHusband = family.GenderForHusband,
                WifeIdNumber = family.WifeIdNumber,
                WifeName = family.WifeName,
                WifeStatus = family.WifeStatus,
                DateChangeStatusForWife = family.DateChangeStatusForWife,
                GenderForWife = family.GenderForWife,
                FinancialSituation = family.FinancialSituation,
                NumberMembers = family.NumberMembers,
                MaritalStatus = family.MaritalStatus,
                PhoneNumber = family.Husband.PhoneNumber,
                RepresentativeId = family.RepresentativeId,
                OriginalGovernotate = family.OriginalAddress.Governotate,
                OriginaCity = family.OriginalAddress.City,
                OriginaNeighborhood = family.OriginalAddress.Neighborhood,
                IsDisability = _context.Disabilities.Any(x => x.FamilyId.ToString().Equals(familyId) && !x.IsDelete),
                IsDisplaced = _context.Displaces.Any(x => x.FamilyId.ToString().Equals(familyId) && !x.IsDeleted),
                IsDisease = _context.Diseases.Any(x => x.FamilyId.ToString().Equals(familyId) && !x.IsDelete),
                IsPledge = family.IsPledge,
                DivisionName = family.Husband.Division.Name,
            };

            if (familyDto.IsDisplaced)
            {
                var displace = _context.Displaces.Include(x => x.CurrentAddress).FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.CurrentGovernotate = displace.CurrentAddress.Governotate;
                familyDto.CurrentCity = displace.CurrentAddress.City;
                familyDto.CurrentNeighborhood = displace.CurrentAddress.Neighborhood;
            }
            if (familyDto.IsDisability)
            {
                var disability = _context.Disabilities.FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.Visual = (disability.Visual <= 0) ? null : disability.Visual;
                familyDto.Motor = (disability.Motor <= 0) ? null : disability.Motor;
                familyDto.Hearing = (disability.Hearing <= 0) ? null : disability.Hearing;
                familyDto.Mental = (disability.Mental <= 0) ? null : disability.Mental;
            }
            if (familyDto.IsDisease)
            {
                var disease = _context.Diseases.FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.BloodPressure = (disease.BloodPressure <= 0) ? null : disease.BloodPressure;
                familyDto.Diabetes = (disease.Diabetes <= 0) ? null : disease.Diabetes;
                familyDto.KidneyFailure = (disease.KidneyFailure <= 0) ? null : disease.KidneyFailure;
                familyDto.Cancer = (disease.Cancer <= 0) ? null : disease.Cancer;
            }

            return new ResultResponse
            {
                Success = true,
                result = familyDto,
                Message = "تم جلب بيانات العائلة بنجاح"
            };

        }
        public async Task<ResultResponse> GetFamilyDtoByUserIdAsync(string userId, HttpContext httpContext)
        {

            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            var family = await _context.Families
                .Include(x => x.Husband)
                .Include(x => x.OriginalAddress)
                .Include(x => x.Disability)
                .Include(x => x.Displace)
                .Include(x => x.Disease)
                .FirstOrDefaultAsync(x => x.Husband.Id.Equals(userId));
            if (family == null)
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات العائلة غير مسجلة"
                };
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للاستمرار"
                };
            }
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            // Role-based validation for family creation
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can create any family
            }
            else if (currentRoles.Contains(Role.admin.ToString()))
            {
                // Admin can only create family in their own division
                if (!currentUser.DivisionId.Equals(family.Husband.DivisionId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            else if (currentRoles.Contains(Role.representative.ToString()))
            {
                // Representative can only create a family within their own division
                if (!currentUser.Id.Equals(family.HusbandId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            else
            {
                if (!currentUser.Id.Equals(family.HusbandId))
                {
                    // Other roles cannot create families
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            var familyDto = new FamilyDto
            {
                Id = family.Id.ToString(),
                HusbandName = family.Husband.FullName,
                IdNumber = family.Husband.IdNumber,
                HusbandStatus = family.HusbandStatus,
                DateChangeStatusForHusband = family.DateChangeStatusForHusband,
                GenderForHusband = family.GenderForHusband,
                WifeIdNumber = family.WifeIdNumber,
                WifeName = family.WifeName,
                WifeStatus = family.WifeStatus,
                DateChangeStatusForWife = family.DateChangeStatusForWife,
                GenderForWife = family.GenderForWife,
                FinancialSituation = family.FinancialSituation,
                NumberMembers = family.NumberMembers,
                MaritalStatus = family.MaritalStatus,
                PhoneNumber = family.Husband.PhoneNumber,
                RepresentativeId = family.RepresentativeId,
                OriginalGovernotate = family.OriginalAddress.Governotate,
                OriginaCity = family.OriginalAddress.City,
                OriginaNeighborhood = family.OriginalAddress.Neighborhood,
                IsDisability = _context.Disabilities.Any(x => x.FamilyId.ToString().Equals(family.Id) && !x.IsDelete),
                IsDisplaced = _context.Displaces.Any(x => x.FamilyId.ToString().Equals(family.Id) && !x.IsDeleted),
                IsDisease = _context.Disabilities.Any(x => x.FamilyId.ToString().Equals(family.Id) && !x.IsDelete),
                IsPledge = family.IsPledge,
                StatusFamily = family.StatusFamily,
            };

            if (familyDto.IsDisplaced)
            {
                var displace = _context.Displaces.Include(x => x.CurrentAddress).FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.CurrentGovernotate = displace.CurrentAddress.Governotate;
                familyDto.CurrentCity = displace.CurrentAddress.City;
                familyDto.CurrentNeighborhood = displace.CurrentAddress.Neighborhood;
            }
            if (familyDto.IsDisability)
            {
                var disability = _context.Disabilities.FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.Visual = disability.Visual;
                familyDto.Motor = disability.Motor;
                familyDto.Hearing = disability.Hearing;
                familyDto.Mental = disability.Mental;
            }
            if (familyDto.IsDisease)
            {
                var disease = _context.Diseases.FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.BloodPressure = disease.BloodPressure;
                familyDto.Diabetes = disease.Diabetes;
                familyDto.KidneyFailure = disease.KidneyFailure;
                familyDto.Cancer = disease.Cancer;
            }

            return new ResultResponse
            {
                Success = true,
                result = familyDto,
                Message = "تم جلب بيانات العائلة بنجاح"
            };

        }

        public async Task<ResultResponse> ChangeStatusFamilyAsync(StatusFamily statusFamily, string familyId, HttpContext httpContext, string message = "")
        {
            var family = await _context.Families.Include(x => x.Husband).FirstOrDefaultAsync(x => x.Id.ToString().Equals(familyId));
            var admin = await _userManager.GetUserAsync(httpContext.User);
            var rolesAdmin = (await _userManager.GetRolesAsync(admin)).ToList();
            if (family == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة",
                    Errors = new List<string> { "ID العائلة غير صحيح" }
                };
            }

            if (family.IsDeleted)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "تم حذف العائلة مسبقا"
                };
            }
            if (!family.RepresentativeId.Equals(admin.Id))
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "غير مصرح لك بالتعامل العائلات خارج مربعك",
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (family.StatusFamily != StatusFamily.pending && family.StatusFamily != StatusFamily.noRequest)
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "يجب تقديم طلب إغاثة من العائلة",
                    };
                }
                family.StatusFamily = statusFamily;
                _context.Families.Update(family);
                await _context.SaveChangesAsync();
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.Family,
                    RepoId = family.Id.ToString(),
                    CreatedDate = DateTime.UtcNow,
                    AdminId = admin.Id,
                };

                if (family.StatusFamily == StatusFamily.accepted)
                {
                    auditLog.Name = AuditName.Accepted;
                    auditLog.Description = "تم الموافقة على طلب الإغاثة";
                }
                else
                {
                    auditLog.Name = AuditName.Rejected;
                    auditLog.Description = "تم رفض طلب الإغاثة وذلك بسبب : " + message;
                }

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
                    Message = "تم تحديث حالة طلب الإغاثة"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل تحديث طلب الإغاثة",
                    Errors = new List<string> { ex.Message }
                };
            }

        }

        public async Task<ResultResponse> GetFamilyViewModelByFamiyIdAsync(string familyId, HttpContext httpContext)
        {

            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            var family = await _context.Families
                .Include(x => x.Husband)
                .Include(x => x.OriginalAddress)
                .Include(x => x.Disability)
                .Include(x => x.Displace)
                .Include(x => x.Disease)
                .Include(x => x.Representative)
                .FirstOrDefaultAsync(x => x.Id.ToString().Equals(familyId));
            if (family == null)
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات العائلة غير مسجلة"
                };
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للاستمرار"
                };
            }
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            // Role-based validation for family creation
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can create any family
            }
            else if (currentRoles.Contains(Role.admin.ToString()))
            {
                // Admin can only create family in their own division
                if (!currentUser.DivisionId.Equals(family.Husband.DivisionId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            else if (currentRoles.Contains(Role.representative.ToString()))
            {
                // Representative can only create a family within their own division
                if (!currentUser.Id.Equals(family.HusbandId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            else
            {
                if (!currentUser.Id.Equals(family.HusbandId))
                {
                    // Other roles cannot create families
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            var familyViewModel = new FamilyViewModel
            {
                Id = family.Id.ToString(),
                HusbandName = family.Husband.FullName,
                IdNumber = family.Husband.IdNumber,
                HusbandStatus = EnumHelper.GetDisplayName(family.HusbandStatus),
                DateChangeStatusForHusband = family.DateChangeStatusForHusband.ToString("d-M-yyyy"),
                GenderForHusband = EnumHelper.GetDisplayName(family.GenderForHusband),
                WifeIdNumber = family.WifeIdNumber,
                WifeName = family.WifeName,
                WifeStatus = EnumHelper.GetDisplayName(family.WifeStatus),
                DateChangeStatusForWife = family.DateChangeStatusForWife.ToString("d-M-yyyy"),
                GenderForWife = EnumHelper.GetDisplayName(family.GenderForWife),
                FinancialSituation = EnumHelper.GetDisplayName(family.FinancialSituation),
                NumberMembers = family.NumberMembers,
                MaritalStatus = EnumHelper.GetDisplayName(family.MaritalStatus),
                PhoneNumber = family.Husband.PhoneNumber,
                RepresentativeName = family.Representative.FullName,
                OriginalGovernotate = family.OriginalAddress.Governotate,
                OriginaCity = family.OriginalAddress.City,
                OriginaNeighborhood = family.OriginalAddress.Neighborhood,
                StatusFamily = EnumHelper.GetDisplayName(family.StatusFamily),
            };
            var jsonData = new { draw = 1, recordsFiltered = 1, recordsTotal = 1, data = new List<FamilyViewModel> { familyViewModel } };

            return new ResultResponse
            {
                Success = true,
                result = jsonData,
                Message = "تم جلب بيانات العائلة بنجاح"
            };

        }

        public async Task<List<Family>> GetAllFamiliesAsync(HttpContext httpContext, string representativeId = "")
        {
            var currentUser = await _userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
                return new List<Family>();
            currentUser = _context.Users.Include(x => x.Division).FirstOrDefault(x => x.Id.Equals(currentUser.Id));

            var families = _context.Families
                .Include(x => x.Husband)
                .Where(x => !x.IsDeleted && x.StatusFamily == StatusFamily.accepted)
                .ToList();

            if (string.IsNullOrEmpty(representativeId))
                families = families.Where(x => x.Husband.DivisionId.ToString().Equals(currentUser.DivisionId.ToString()))
                            .ToList();
            else
                families = families.Where(x => x.RepresentativeId.Equals(representativeId))
                            .ToList();

            return families;
        }

        public async Task<ResultResponse> ChangeFinancialSituationyAsync(FinancialSituation financialSituation, string familyId, HttpContext httpContext)
        {
            var family = await _context.Families.Include(x => x.Husband).FirstOrDefaultAsync(x => x.Id.ToString().Equals(familyId));
            var admin = await _userManager.GetUserAsync(httpContext.User);
            var rolesAdmin = (await _userManager.GetRolesAsync(admin)).ToList();
            if (family == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة",
                    Errors = new List<string> { "ID العائلة غير صحيح" }
                };
            }

            if (family.IsDeleted)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "تم حذف العائلة مسبقا"
                };
            }
            if (!family.RepresentativeId.Equals(admin.Id))
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "غير مصرح لك بالتعامل العائلات خارج مربعك",
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (family.StatusFamily != StatusFamily.pending && family.StatusFamily != StatusFamily.noRequest)
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "يجب تقديم طلب إغاثة من العائلة",
                    };
                }
                family.FinancialSituation = financialSituation;
                _context.Families.Update(family);
                await _context.SaveChangesAsync();
                // Add audit log
                var auditLog = new AuditLog
                {
                    EntityType = EntityType.Family,
                    RepoId = family.Id.ToString(),
                    CreatedDate = DateTime.UtcNow,
                    AdminId = admin.Id,
                    Name = AuditName.Update,
                    Description = "تم تحديث الحالة المادية للأسرة"
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
                    Message = "تم تحديث الحالة المادية للأسرة"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultResponse
                {
                    Success = false,
                    Message = "فشل تحديث الحالة المادية للأسرة",
                    Errors = new List<string> { ex.Message }
                };
            }

        }

        public async Task<ResultResponse> GetFamilyDtoByIdNumberAsync(string IdNumber, HttpContext httpContext)
        {

            // Get the current user's roles
            var currentUserId = (await _userManager.GetUserAsync(httpContext.User)).Id;
            currentUserId = (currentUserId == null) ? string.Empty : currentUserId;
            var currentUser = await _context.Users.Include(x => x.Division).ThenInclude(x => x.Users).FirstOrDefaultAsync(x => x.Id.Equals(currentUserId));
            var family = await _context.Families
                .Include(x => x.Husband)
                .Include(x => x.OriginalAddress)
                .Include(x => x.Disability)
                .Include(x => x.Displace)
                .Include(x => x.Disease)
                .FirstOrDefaultAsync(x => x.Husband.IdNumber.Equals(IdNumber));
            if (family == null)
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات العائلة غير مسجلة"
                };
            if (currentUser == null || currentUser.isDelete)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "يجب تسجيل الدخول للاستمرار"
                };
            }
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            // Role-based validation for family creation
            if (currentRoles.Contains(Role.superadmin.ToString()))
            {
                // Superadmin can create any family
            }
            else if (currentRoles.Contains(Role.admin.ToString()))
            {
                // Admin can only create family in their own division
                if (!currentUser.DivisionId.Equals(family.Husband.DivisionId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            else if (currentRoles.Contains(Role.representative.ToString()))
            {
                // Representative can only create a family within their own division
                if (!currentUser.Id.Equals(family.HusbandId))
                {
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            else
            {
                if (!currentUser.Id.Equals(family.HusbandId))
                {
                    // Other roles cannot create families
                    return new ResultResponse
                    {
                        Success = false,
                        Message = "غير مصرح لك بالوصول لهذه البيانات"
                    };
                }
            }
            var familyDto = new FamilyDto
            {
                Id = family.Id.ToString(),
                HusbandName = family.Husband.FullName,
                IdNumber = family.Husband.IdNumber,
                HusbandStatus = family.HusbandStatus,
                DateChangeStatusForHusband = family.DateChangeStatusForHusband,
                GenderForHusband = family.GenderForHusband,
                WifeIdNumber = family.WifeIdNumber,
                WifeName = family.WifeName,
                WifeStatus = family.WifeStatus,
                DateChangeStatusForWife = family.DateChangeStatusForWife,
                GenderForWife = family.GenderForWife,
                FinancialSituation = family.FinancialSituation,
                NumberMembers = family.NumberMembers,
                MaritalStatus = family.MaritalStatus,
                PhoneNumber = family.Husband.PhoneNumber,
                RepresentativeId = family.RepresentativeId,
                OriginalGovernotate = family.OriginalAddress.Governotate,
                OriginaCity = family.OriginalAddress.City,
                OriginaNeighborhood = family.OriginalAddress.Neighborhood,
                IsDisability = _context.Disabilities.Any(x => x.FamilyId.ToString().Equals(family.Id) && !x.IsDelete),
                IsDisplaced = _context.Displaces.Any(x => x.FamilyId.ToString().Equals(family.Id) && !x.IsDeleted),
                IsDisease = _context.Disabilities.Any(x => x.FamilyId.ToString().Equals(family.Id) && !x.IsDelete),
                IsPledge = family.IsPledge,
                StatusFamily = family.StatusFamily,
            };

            if (familyDto.IsDisplaced)
            {
                var displace = _context.Displaces.Include(x => x.CurrentAddress).FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.CurrentGovernotate = displace.CurrentAddress.Governotate;
                familyDto.CurrentCity = displace.CurrentAddress.City;
                familyDto.CurrentNeighborhood = displace.CurrentAddress.Neighborhood;
            }
            if (familyDto.IsDisability)
            {
                var disability = _context.Disabilities.FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.Visual = disability.Visual;
                familyDto.Motor = disability.Motor;
                familyDto.Hearing = disability.Hearing;
                familyDto.Mental = disability.Mental;
            }
            if (familyDto.IsDisease)
            {
                var disease = _context.Diseases.FirstOrDefault(x => x.FamilyId.ToString().Equals(familyDto.Id));
                familyDto.BloodPressure = disease.BloodPressure;
                familyDto.Diabetes = disease.Diabetes;
                familyDto.KidneyFailure = disease.KidneyFailure;
                familyDto.Cancer = disease.Cancer;
            }

            return new ResultResponse
            {
                Success = true,
                result = familyDto,
                Message = "تم جلب بيانات العائلة بنجاح"
            };

        }
    }
}
