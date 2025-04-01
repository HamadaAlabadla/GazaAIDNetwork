using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using Microsoft.EntityFrameworkCore;

namespace GazaAIDNetwork.Infrastructure.Services.FamilyService
{
    public interface IDisplacedService
    {
        Task<ResultResponse> CreateDisplaceForFamilyAsync(Displace displace);

    }
    public class DisplacedService : IDisplacedService
    {
        private readonly ApplicationDbContext _context;
        public DisplacedService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ResultResponse> CreateDisplaceForFamilyAsync(Displace displace)
        {
            if (displace == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "بيانات غير صالح."
                };
            }

            // Check if the FamilyId exists in the Families table
            var existingFamily = await _context.Families.FirstOrDefaultAsync(f => f.Id == displace.FamilyId);
            if (existingFamily == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "العائلة غير موجودة."
                };
            }

            // Add the address
            _context.Displaces.Add(displace);

            // Commit the transaction
            try
            {
                await _context.SaveChangesAsync();
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إضافة حالة النزوح بنجاح."
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ أثناء إضافة حالة النزوح: {ex.Message}"
                };
            }
        }
    }
}
