using GazaAIDNetwork.EF.Data;
using GazaAIDNetwork.EF.Models;
using GazaAIDNetwork.Infrastructure.Respons;
using Microsoft.EntityFrameworkCore;

namespace GazaAIDNetwork.Infrastructure.Services.FamilyService
{
    public interface IAddressService
    {
        Task<ResultResponse> CreateAddressForFamilyAsync(Address address);
        Task<ResultResponse> UpdateAddressForFamilyAsync(Address address, Guid id);
    }
    public class AddressService : IAddressService
    {
        private readonly ApplicationDbContext _context;
        public AddressService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ResultResponse> UpdateAddressForFamilyAsync(Address address, Guid id)
        {
            if (address == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "عنوان غير صالح."
                };
            }

            var exsistAddress = await _context.Addresses.FirstOrDefaultAsync(x => x.Id == id);
            exsistAddress.Governotate = address.Governotate;
            exsistAddress.City = address.City;
            exsistAddress.Neighborhood = address.Neighborhood;
            // Add the address
            _context.Addresses.Update(exsistAddress);

            // Commit the transaction
            try
            {
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم تحديث العنوان بنجاح.",
                    result = address
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ أثناء تحديث العنوان: {ex.Message}"
                };
            }
        }
        public async Task<ResultResponse> CreateAddressForFamilyAsync(Address address)
        {
            if (address == null)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = "عنوان غير صالح."
                };
            }

            // Add the address
            _context.Addresses.Add(address);

            // Commit the transaction
            try
            {
                await _context.SaveChangesAsync();
                return new ResultResponse
                {
                    Success = true,
                    Message = "تم إضافة العنوان بنجاح.",
                    result = address
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse
                {
                    Success = false,
                    Message = $"حدث خطأ أثناء إضافة العنوان: {ex.Message}"
                };
            }
        }

    }
}
