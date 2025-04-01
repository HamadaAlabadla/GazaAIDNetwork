using System.ComponentModel.DataAnnotations;

namespace GazaAIDNetwork.EF.Models
{
    public class Disability
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid FamilyId { get; set; }

        [Required]
        public Family Family { get; set; }

        private int _mental = 0;
        public int Mental
        {
            get => _mental;
            set
            {
                _mental = value < 0 ? 0 : value;
                UpdateIsDelete();
            }
        }

        private int _motor = 0;
        public int Motor
        {
            get => _motor;
            set
            {
                _motor = value < 0 ? 0 : value;
                UpdateIsDelete();
            }
        }

        private int _hearing = 0;
        public int Hearing
        {
            get => _hearing;
            set
            {
                _hearing = value < 0 ? 0 : value;
                UpdateIsDelete();
            }
        }

        private int _visual = 0;
        public int Visual
        {
            get => _visual;
            set
            {
                _visual = value < 0 ? 0 : value;
                UpdateIsDelete();
            }
        }

        public bool IsDelete { get; set; } = false;

        public void UpdateIsDelete()
        {
            IsDelete = (_mental == 0 && _motor == 0 && _hearing == 0 && _visual == 0);
        }
    }
}
