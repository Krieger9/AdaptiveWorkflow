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

    public DbSet<UserCollaborationState> UserCollaborationStates => Set<UserCollaborationState>();

    public DbSet<CollaborationTurnDigest> CollaborationTurnDigests => Set<CollaborationTurnDigest>();

    public DbSet<CollaborationStateChangeLog> CollaborationStateChangeLogs => Set<CollaborationStateChangeLog>();

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

    public DbSet<ContractWorkMode> ContractWorkModes => Set<ContractWorkMode>();

    public DbSet<ContractEngagementType> ContractEngagementTypes => Set<ContractEngagementType>();

    public DbSet<ContractSkillPriority> ContractSkillPriorities => Set<ContractSkillPriority>();

    public DbSet<ContractConstraintType> ContractConstraintTypes => Set<ContractConstraintType>();

    public DbSet<ContractDeliveryRiskLevel> ContractDeliveryRiskLevels => Set<ContractDeliveryRiskLevel>();

    public DbSet<ContractStrategicValueLevel> ContractStrategicValueLevels => Set<ContractStrategicValueLevel>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<ContractSkill> ContractSkills => Set<ContractSkill>();

    public DbSet<ContractConstraint> ContractConstraints => Set<ContractConstraint>();

    public DbSet<ContractDeliverable> ContractDeliverables => Set<ContractDeliverable>();

    public DbSet<ContractMilestone> ContractMilestones => Set<ContractMilestone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureLookups(modelBuilder);
        ConfigureEmployeeProfile(modelBuilder);
        ConfigureContracts(modelBuilder);
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

        var collab = modelBuilder.Entity<UserCollaborationState>();
        collab.ToTable("UserCollaborationStates");
        collab.HasKey(c => c.UserId);
        collab.Property(c => c.TendencySource).IsRequired().HasMaxLength(32);
        collab.Property(c => c.UpdatedAt).IsRequired();
        collab.HasOne(c => c.User)
            .WithOne()
            .HasForeignKey<UserCollaborationState>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var turnDigest = modelBuilder.Entity<CollaborationTurnDigest>();
        turnDigest.ToTable("CollaborationTurnDigests");
        turnDigest.HasKey(d => d.Id);
        turnDigest.Property(d => d.Id).ValueGeneratedOnAdd();
        turnDigest.Property(d => d.Sequence).IsRequired();
        turnDigest.Property(d => d.CreatedAt).IsRequired();
        turnDigest.Property(d => d.DigestText).IsRequired();
        turnDigest.HasIndex(d => new { d.UserId, d.Sequence })
            .HasDatabaseName("IX_CollaborationTurnDigests_UserId_Sequence");
        turnDigest.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var changeLog = modelBuilder.Entity<CollaborationStateChangeLog>();
        changeLog.ToTable("CollaborationStateChangeLogs");
        changeLog.HasKey(l => l.Id);
        changeLog.Property(l => l.Id).ValueGeneratedOnAdd();
        changeLog.Property(l => l.Reason).IsRequired();
        changeLog.Property(l => l.CreatedAt).IsRequired();
        changeLog.HasIndex(l => new { l.UserId, l.CreatedAt })
            .HasDatabaseName("IX_CollaborationStateChangeLogs_UserId_CreatedAt");
        changeLog.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        changeLog.HasOne(l => l.TurnDigest)
            .WithMany()
            .HasForeignKey(l => l.TurnDigestId)
            .OnDelete(DeleteBehavior.SetNull);
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

    private static void ConfigureContracts(ModelBuilder modelBuilder)
    {
        var workMode = modelBuilder.Entity<ContractWorkMode>();
        workMode.ToTable("ContractWorkModes");
        workMode.HasKey(x => x.Id);
        workMode.Property(x => x.Id).ValueGeneratedNever();
        workMode.Property(x => x.Code).IsRequired().HasMaxLength(32);
        workMode.Property(x => x.Name).IsRequired().HasMaxLength(100);
        workMode.Property(x => x.SortOrder).IsRequired();
        workMode.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_ContractWorkModes_Code");

        var engagement = modelBuilder.Entity<ContractEngagementType>();
        engagement.ToTable("ContractEngagementTypes");
        engagement.HasKey(x => x.Id);
        engagement.Property(x => x.Id).ValueGeneratedNever();
        engagement.Property(x => x.Code).IsRequired().HasMaxLength(32);
        engagement.Property(x => x.Name).IsRequired().HasMaxLength(100);
        engagement.Property(x => x.SortOrder).IsRequired();
        engagement.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_ContractEngagementTypes_Code");

        var priority = modelBuilder.Entity<ContractSkillPriority>();
        priority.ToTable("ContractSkillPriorities");
        priority.HasKey(x => x.Id);
        priority.Property(x => x.Id).ValueGeneratedNever();
        priority.Property(x => x.Code).IsRequired().HasMaxLength(32);
        priority.Property(x => x.Name).IsRequired().HasMaxLength(100);
        priority.Property(x => x.SortOrder).IsRequired();
        priority.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_ContractSkillPriorities_Code");

        var constraintType = modelBuilder.Entity<ContractConstraintType>();
        constraintType.ToTable("ContractConstraintTypes");
        constraintType.HasKey(x => x.Id);
        constraintType.Property(x => x.Id).ValueGeneratedNever();
        constraintType.Property(x => x.Code).IsRequired().HasMaxLength(64);
        constraintType.Property(x => x.Name).IsRequired().HasMaxLength(100);
        constraintType.Property(x => x.SortOrder).IsRequired();
        constraintType.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_ContractConstraintTypes_Code");

        var deliveryRisk = modelBuilder.Entity<ContractDeliveryRiskLevel>();
        deliveryRisk.ToTable("ContractDeliveryRiskLevels");
        deliveryRisk.HasKey(x => x.Id);
        deliveryRisk.Property(x => x.Id).ValueGeneratedNever();
        deliveryRisk.Property(x => x.Code).IsRequired().HasMaxLength(32);
        deliveryRisk.Property(x => x.Name).IsRequired().HasMaxLength(100);
        deliveryRisk.Property(x => x.SortOrder).IsRequired();
        deliveryRisk.Property(x => x.ConfidenceFactor).HasPrecision(4, 2);
        deliveryRisk.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_ContractDeliveryRiskLevels_Code");

        var strategicValue = modelBuilder.Entity<ContractStrategicValueLevel>();
        strategicValue.ToTable("ContractStrategicValueLevels");
        strategicValue.HasKey(x => x.Id);
        strategicValue.Property(x => x.Id).ValueGeneratedNever();
        strategicValue.Property(x => x.Code).IsRequired().HasMaxLength(32);
        strategicValue.Property(x => x.Name).IsRequired().HasMaxLength(100);
        strategicValue.Property(x => x.SortOrder).IsRequired();
        strategicValue.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_ContractStrategicValueLevels_Code");

        var contract = modelBuilder.Entity<Contract>();
        contract.ToTable("Contracts");
        contract.HasKey(c => c.Id);
        contract.Property(c => c.Code).IsRequired().HasMaxLength(64);
        contract.Property(c => c.Title).IsRequired().HasMaxLength(200);
        contract.Property(c => c.ClientName).IsRequired().HasMaxLength(200);
        contract.Property(c => c.OutcomeSummary).IsRequired().HasMaxLength(500);
        contract.Property(c => c.ScopeSummary).IsRequired().HasMaxLength(2000);
        contract.Property(c => c.EstimatedContractValue).HasPrecision(18, 2);
        contract.Property(c => c.EstimatedProfit).HasPrecision(18, 2);
        contract.Property(c => c.EstimatedMarginPercent).HasPrecision(5, 2);
        contract.Property(c => c.WinProbabilityPercent).HasPrecision(5, 2);
        contract.Property(c => c.StaffingFte).HasPrecision(6, 1);
        contract.Property(c => c.SpecialistStaffingNeeded).HasMaxLength(200);
        contract.Property(c => c.IsDefault).IsRequired();
        contract.Property(c => c.DemoSortOrder).IsRequired();
        contract.Property(c => c.LastSelectedAt);
        contract.Property(c => c.CreatedDate).IsRequired();
        contract.Property(c => c.ModifiedDate).IsRequired();
        contract.HasIndex(c => c.Code).IsUnique().HasDatabaseName("UQ_Contracts_Code");
        contract.HasIndex(c => new { c.LastSelectedAt, c.DemoSortOrder })
            .HasDatabaseName("IX_Contracts_LastSelectedAt_DemoSortOrder");
        contract.HasOne(c => c.EngagementType)
            .WithMany(e => e.Contracts)
            .HasForeignKey(c => c.EngagementTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        contract.HasOne(c => c.WorkMode)
            .WithMany(w => w.Contracts)
            .HasForeignKey(c => c.WorkModeId)
            .OnDelete(DeleteBehavior.Restrict);
        contract.HasOne(c => c.DeliveryRisk)
            .WithMany(r => r.Contracts)
            .HasForeignKey(c => c.DeliveryRiskId)
            .OnDelete(DeleteBehavior.Restrict);
        contract.HasOne(c => c.StrategicValue)
            .WithMany(s => s.Contracts)
            .HasForeignKey(c => c.StrategicValueId)
            .OnDelete(DeleteBehavior.Restrict);

        var skill = modelBuilder.Entity<ContractSkill>();
        skill.ToTable("ContractSkills");
        skill.HasKey(x => new { x.ContractId, x.SkillId });
        skill.HasOne(x => x.Contract)
            .WithMany(c => c.Skills)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        skill.HasOne(x => x.Skill)
            .WithMany()
            .HasForeignKey(x => x.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
        skill.HasOne(x => x.Priority)
            .WithMany(p => p.ContractSkills)
            .HasForeignKey(x => x.PriorityId)
            .OnDelete(DeleteBehavior.Restrict);
        skill.HasIndex(x => x.SkillId).HasDatabaseName("IX_ContractSkills_SkillId");
        skill.HasIndex(x => x.PriorityId).HasDatabaseName("IX_ContractSkills_PriorityId");

        var constraint = modelBuilder.Entity<ContractConstraint>();
        constraint.ToTable("ContractConstraints");
        constraint.HasKey(x => new { x.ContractId, x.ConstraintTypeId });
        constraint.HasOne(x => x.Contract)
            .WithMany(c => c.Constraints)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        constraint.HasOne(x => x.ConstraintType)
            .WithMany(t => t.ContractConstraints)
            .HasForeignKey(x => x.ConstraintTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        constraint.HasIndex(x => x.ConstraintTypeId)
            .HasDatabaseName("IX_ContractConstraints_ConstraintTypeId");

        var deliverable = modelBuilder.Entity<ContractDeliverable>();
        deliverable.ToTable("ContractDeliverables");
        deliverable.HasKey(d => d.Id);
        deliverable.Property(d => d.Title).IsRequired().HasMaxLength(200);
        deliverable.Property(d => d.Detail).HasMaxLength(500);
        deliverable.Property(d => d.SortOrder).IsRequired();
        deliverable.HasOne(d => d.Contract)
            .WithMany(c => c.Deliverables)
            .HasForeignKey(d => d.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        deliverable.HasIndex(d => new { d.ContractId, d.SortOrder })
            .HasDatabaseName("IX_ContractDeliverables_ContractId_SortOrder");

        var milestone = modelBuilder.Entity<ContractMilestone>();
        milestone.ToTable("ContractMilestones");
        milestone.HasKey(m => m.Id);
        milestone.Property(m => m.Name).IsRequired().HasMaxLength(200);
        milestone.Property(m => m.Description).HasMaxLength(500);
        milestone.Property(m => m.SortOrder).IsRequired();
        milestone.HasOne(m => m.Contract)
            .WithMany(c => c.Milestones)
            .HasForeignKey(m => m.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        milestone.HasIndex(m => new { m.ContractId, m.SortOrder })
            .HasDatabaseName("IX_ContractMilestones_ContractId_SortOrder");
    }

    private static void ConfigureTeams(ModelBuilder modelBuilder)
    {
        var team = modelBuilder.Entity<Team>();
        team.ToTable("Teams");
        team.HasKey(t => t.Id);
        team.Property(t => t.Name).IsRequired().HasMaxLength(200);
        team.Property(t => t.ContractId).IsRequired();
        team.Property(t => t.CreatedDate).IsRequired();
        team.Property(t => t.ModifiedDate).IsRequired();
        team.HasIndex(t => new { t.ContractId, t.Name })
            .IsUnique()
            .HasDatabaseName("UQ_Teams_ContractId_Name");
        team.HasIndex(t => t.ContractId).HasDatabaseName("IX_Teams_ContractId");
        team.HasOne(t => t.Contract)
            .WithMany(c => c.Teams)
            .HasForeignKey(t => t.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

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
