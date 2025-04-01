using GazaAIDNetwork.EF.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.EF.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            var adminRoleId = "E1A7316F-5B6E-4F77-9D77-9C37A1BC912F";
            var adminUserId = "56A8B5D6-9337-4D8B-92C3-5DBD6D5BCA71";
            var adminAuditId = "56A3D5B6-6547-6A7C-92C3-5ABD6D5CCA71";

            builder.Entity<IdentityRole>()
                .HasData(new IdentityRole()
                {
                    Id = adminRoleId,
                    Name = "superadmin",
                    NormalizedName = "SUPERADMIN"
                });
            builder.Entity<AuditLog>()
                .HasData(new AuditLog()
                {
                    AdminId = adminUserId,
                    CreatedDate = DateTime.Now,
                    Description = "تمت إضافة الآدمن بنجاح",
                    EntityType = EntityType.User,
                    Id = Guid.Parse(adminAuditId),
                    Name = AuditName.Create,
                    RepoId = adminUserId,
                });
            builder.Entity<User>()
                .HasData(new User()
                {
                    Id = adminUserId,
                    IdNumber = "407069541",
                    Email = "admin@admin.com",
                    FullName = "حمادة حسام حمادة العبادلة",
                    NormalizedUserName = "407069541",
                    PhoneNumber = "0595195186",
                    PhoneNumberConfirmed = true,
                    UserName = "407069541",
                    SecurityStamp = Guid.NewGuid().ToString(), // Required for Identity
                    ConcurrencyStamp = Guid.NewGuid().ToString(), // Required for Identity
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "Admin@123"), // Set hashed password
                    isDelete = false

                });


            builder.Entity<IdentityUserRole<string>>()
                .HasData(new IdentityUserRole<string>
                {
                    UserId = adminUserId, // Link to the admin user
                    RoleId = adminRoleId  // Link to the admin role
                });

            builder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            });

            builder.Entity<Division>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            });
            builder.Entity<Family>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            });

            builder.Entity<Address>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            });

            builder.Entity<Family>()
                 .HasOne(f => f.OriginalAddress)
                 .WithMany(a => a.FamiliesWithOriginalAddress)
                 .HasForeignKey(f => f.OriginalAddressId)
                 .OnDelete(DeleteBehavior.Restrict);  // Prevent accidental deletion


            builder.Entity<Family>()
                .HasOne(f => f.Representative) // Assuming Representative is a navigation property in Family
                .WithMany()  // If Representative is related to many families, otherwise use WithOne()
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete here

            builder.Entity<Family>()
                .HasOne(f => f.Displace)
                .WithOne(d => d.Family)
                .HasForeignKey<Displace>(d => d.FamilyId)
                .OnDelete(DeleteBehavior.Cascade); // Delete displacement record if family is deleted
            builder.Entity<Displace>()
                .HasOne(d => d.CurrentAddress)
                .WithMany()
                .HasForeignKey(d => d.CurrentAddressId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of address if it's a current address



            builder.Entity<Family>()
                .HasOne(f => f.Disability)  // A Family has one Address
                .WithOne(a => a.Family)     // Each Address has one Family
                .HasForeignKey<Disability>(a => a.FamilyId) // Address has a foreign key to Family
                .OnDelete(DeleteBehavior.Cascade); // Optional: Set delete behavior


            builder.Entity<Family>()
                .HasOne(f => f.Disease)  // A Family has one Address
                .WithOne(a => a.Family)     // Each Address has one Family
                .HasForeignKey<Disease>(a => a.FamilyId) // Address has a foreign key to Family
                .OnDelete(DeleteBehavior.Cascade); // Optional: Set delete behavior

            builder.Entity<Disability>()
                .HasOne(f => f.Family) // Assuming Representative is a navigation property in Family
                .WithOne(a => a.Disability)  // If Representative is related to many families, otherwise use WithOne()
                .HasForeignKey<Disability>(f => f.FamilyId) // Foreign key
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete here


            builder.Entity<Disease>()
                .HasOne(f => f.Family) // Assuming Representative is a navigation property in Family
                .WithOne(a => a.Disease)  // If Representative is related to many families, otherwise use WithOne()
                .HasForeignKey<Disease>(f => f.FamilyId) // Foreign key
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete here


            builder.Entity<Displace>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            });

            builder.Entity<Disability>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            });
            builder.Entity<Disease>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            });

        }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Family> Families { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Displace> Displaces { get; set; }
        public DbSet<Disability> Disabilities { get; set; }
        public DbSet<Disease> Diseases { get; set; }
        public DbSet<OrderAid> OrderAids { get; set; }
        public DbSet<InfoRepresentative> InfoRepresentatives { get; set; }
        public DbSet<CycleAid> CycleAids { get; set; }
        public DbSet<ProjectAid> ProjectAids { get; set; }
    }
}
