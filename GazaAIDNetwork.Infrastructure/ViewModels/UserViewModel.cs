using System.ComponentModel.DataAnnotations;

namespace GazaAIDNetwork.Infrastructure.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        [Display(Name = "رقم الهوية")]
        public string IdNumber { get; set; }

        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; }

        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; }
        public string DivisionName { get; set; }
        public List<string?> Roles { get; set; } = new List<string?> { };
        public bool IsDelete { get; set; }
        public string CreatedDate { get; set; }

    }
}
