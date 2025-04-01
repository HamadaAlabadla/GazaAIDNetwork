namespace GazaAIDNetwork.EF.Models
{
    public class Division
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
        public virtual ICollection<User> Users { get; set; } = new HashSet<User>();
    }
}
