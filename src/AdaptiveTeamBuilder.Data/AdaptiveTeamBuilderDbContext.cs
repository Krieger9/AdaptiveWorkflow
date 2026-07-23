using AdaptiveTeamBuilder.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilder.Data;

public class AdaptiveTeamBuilderDbContext : DbContext
{
    public AdaptiveTeamBuilderDbContext(DbContextOptions<AdaptiveTeamBuilderDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<PositionType> PositionTypes => Set<PositionType>();

    public DbSet<ExperienceLevel> ExperienceLevels => Set<ExperienceLevel>();

    public DbSet<RoleSpecialty> RoleSpecialties => Set<RoleSpecialty>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();

    public DbSet<EmployeeProfileSkill> EmployeeProfileSkills => Set<EmployeeProfileSkill>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamPositionRequirement> TeamPositionRequirements => Set<TeamPositionRequirement>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<TeamHiddenProfile> TeamHiddenProfiles => Set<TeamHiddenProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureLookups(modelBuilder);
        ConfigureEmployeeProfile(modelBuilder);
        ConfigureTeams(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();

        user.ToTable("Users");
        user.HasKey(u => u.Id);

        user.Property(u => u.AzureAdObjectId).IsRequired().HasMaxLength(64);
        user.Property(u => u.UserName).IsRequired().HasMaxLength(256);
        user.Property(u => u.FirstName).HasMaxLength(100);
        user.Property(u => u.LastName).HasMaxLength(100);
        user.Property(u => u.DisplayName).HasMaxLength(200);
        user.Property(u => u.CreatedDate).IsRequired();
        user.Property(u => u.ModifiedDate).IsRequired();

        user.HasIndex(u => u.AzureAdObjectId)
            .IsUnique()
            .HasDatabaseName("UQ_Users_AzureAdObjectId");

        user.HasIndex(u => u.UserName)
            .IsUnique()
            .HasDatabaseName("UQ_Users_UserName");
    }

    private static void ConfigureLookups(ModelBuilder modelBuilder)
    {
        var positionType = modelBuilder.Entity<PositionType>();
        positionType.ToTable("PositionTypes");
        positionType.HasKey(p => p.Id);
        positionType.Property(p => p.Id).ValueGeneratedNever();
        positionType.Property(p => p.Code).IsRequired().HasMaxLength(32);
        positionType.Property(p => p.Name).IsRequired().HasMaxLength(100);
        positionType.Property(p => p.SortOrder).IsRequired();
        positionType.HasIndex(p => p.Code).IsUnique().HasDatabaseName("UQ_PositionTypes_Code");

        var level = modelBuilder.Entity<ExperienceLevel>();
        level.ToTable("ExperienceLevels");
        level.HasKey(l => l.Id);
        level.Property(l => l.Id).ValueGeneratedNever();
        level.Property(l => l.Code).IsRequired().HasMaxLength(32);
        level.Property(l => l.Name).IsRequired().HasMaxLength(100);
        level.Property(l => l.SortOrder).IsRequired();
        level.HasIndex(l => l.Code).IsUnique().HasDatabaseName("UQ_ExperienceLevels_Code");

        var specialty = modelBuilder.Entity<RoleSpecialty>();
        specialty.ToTable("RoleSpecialties");
        specialty.HasKey(s => s.Id);
        specialty.Property(s => s.Id).ValueGeneratedNever();
        specialty.Property(s => s.Code).IsRequired().HasMaxLength(64);
        specialty.Property(s => s.Name).IsRequired().HasMaxLength(100);
        specialty.HasIndex(s => s.Code).IsUnique().HasDatabaseName("UQ_RoleSpecialties_Code");
        specialty.HasOne(s => s.PositionType)
            .WithMany(p => p.RoleSpecialties)
            .HasForeignKey(s => s.PositionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        var skill = modelBuilder.Entity<Skill>();
        skill.ToTable("Skills");
        skill.HasKey(s => s.Id);
        skill.Property(s => s.Name).IsRequired().HasMaxLength(100);
        skill.HasIndex(s => s.Name).IsUnique().HasDatabaseName("UQ_Skills_Name");
    }

    private static void ConfigureEmployeeProfile(ModelBuilder modelBuilder)
    {
        var profile = modelBuilder.Entity<EmployeeProfile>();

        profile.ToTable("EmployeeProfiles");
        profile.HasKey(p => p.Id);

        profile.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        profile.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        profile.Property(p => p.DisplayName).IsRequired().HasMaxLength(200);
        profile.Property(p => p.Title).IsRequired().HasMaxLength(200);
        profile.Property(p => p.Summary).HasMaxLength(2000);
        profile.Property(p => p.Location).HasMaxLength(200);
        profile.Property(p => p.Availability).HasMaxLength(64);
        profile.Property(p => p.CreatedDate).IsRequired();
        profile.Property(p => p.ModifiedDate).IsRequired();

        profile.HasIndex(p => p.PositionTypeId)
            .HasDatabaseName("IX_EmployeeProfiles_PositionTypeId");

        profile.HasIndex(p => p.DisplayName)
            .HasDatabaseName("IX_EmployeeProfiles_DisplayName");

        profile.HasOne(p => p.PositionType)
            .WithMany(t => t.EmployeeProfiles)
            .HasForeignKey(p => p.PositionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        profile.HasOne(p => p.ExperienceLevel)
            .WithMany(l => l.EmployeeProfiles)
            .HasForeignKey(p => p.ExperienceLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        profile.HasOne(p => p.RoleSpecialty)
            .WithMany(s => s.EmployeeProfiles)
            .HasForeignKey(p => p.RoleSpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        var link = modelBuilder.Entity<EmployeeProfileSkill>();
        link.ToTable("EmployeeProfileSkills");
        link.HasKey(x => new { x.EmployeeProfileId, x.SkillId });

        link.HasOne(x => x.EmployeeProfile)
            .WithMany(p => p.EmployeeProfileSkills)
            .HasForeignKey(x => x.EmployeeProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        link.HasOne(x => x.Skill)
            .WithMany(s => s.EmployeeProfileSkills)
            .HasForeignKey(x => x.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        link.HasIndex(x => x.SkillId)
            .HasDatabaseName("IX_EmployeeProfileSkills_SkillId");
    }

    private static void ConfigureTeams(ModelBuilder modelBuilder)
    {
        var team = modelBuilder.Entity<Team>();
        team.ToTable("Teams");
        team.HasKey(t => t.Id);
        team.Property(t => t.Name).IsRequired().HasMaxLength(200);
        team.Property(t => t.CreatedDate).IsRequired();
        team.Property(t => t.ModifiedDate).IsRequired();
        team.HasIndex(t => t.Name).IsUnique().HasDatabaseName("UQ_Teams_Name");

        var requirement = modelBuilder.Entity<TeamPositionRequirement>();
        requirement.ToTable("TeamPositionRequirements");
        requirement.HasKey(r => new { r.TeamId, r.PositionTypeId });
        requirement.Property(r => r.RequiredCount).IsRequired();
        requirement.HasOne(r => r.Team)
            .WithMany(t => t.PositionRequirements)
            .HasForeignKey(r => r.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        requirement.HasOne(r => r.PositionType)
            .WithMany(p => p.TeamPositionRequirements)
            .HasForeignKey(r => r.PositionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        var member = modelBuilder.Entity<TeamMember>();
        member.ToTable("TeamMembers");
        member.HasKey(m => new { m.TeamId, m.EmployeeProfileId });
        member.Property(m => m.AddedDate).IsRequired();
        member.HasOne(m => m.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        member.HasOne(m => m.EmployeeProfile)
            .WithMany()
            .HasForeignKey(m => m.EmployeeProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        member.HasIndex(m => m.EmployeeProfileId)
            .HasDatabaseName("IX_TeamMembers_EmployeeProfileId");

        var hidden = modelBuilder.Entity<TeamHiddenProfile>();
        hidden.ToTable("TeamHiddenProfiles");
        hidden.HasKey(h => new { h.TeamId, h.EmployeeProfileId });
        hidden.Property(h => h.HiddenDate).IsRequired();
        hidden.HasOne(h => h.Team)
            .WithMany(t => t.HiddenProfiles)
            .HasForeignKey(h => h.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        hidden.HasOne(h => h.EmployeeProfile)
            .WithMany()
            .HasForeignKey(h => h.EmployeeProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        hidden.HasIndex(h => h.EmployeeProfileId)
            .HasDatabaseName("IX_TeamHiddenProfiles_EmployeeProfileId");
    }
}
