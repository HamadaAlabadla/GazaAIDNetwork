using System.ComponentModel.DataAnnotations;

namespace GazaAIDNetwork.EF.Models
{
    public class Disease
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid FamilyId { get; set; }

        [Required]
        public Family Family { get; set; }
        public int _diabetes = 0;
        public int Diabetes
        {
            get => _diabetes;
            set => _diabetes = value < 0 ? 0 : value;
        }
        public int _bloodPressure = 0;
        public int BloodPressure
        {
            get => _bloodPressure;
            set => _bloodPressure = value < 0 ? 0 : value;
        }
        public int _cancer = 0;
        public int Cancer
        {
            get => _cancer;
            set => _cancer = value < 0 ? 0 : value;
        }
        public int _kidneyFailure = 0;
        public int KidneyFailure
        {
            get => _kidneyFailure;
            set => _kidneyFailure = value < 0 ? 0 : value;
        }

        public bool IsDelete { get; set; }

        public void UpdateIsDelete()
        {
            IsDelete = (_bloodPressure == 0 && _cancer == 0 && _diabetes == 0 && _kidneyFailure == 0);
        }


    }
}
