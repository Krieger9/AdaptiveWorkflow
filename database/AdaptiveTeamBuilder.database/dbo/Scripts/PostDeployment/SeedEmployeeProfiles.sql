-- Reference data and sample contractor profiles (idempotent).

IF NOT EXISTS (SELECT 1 FROM [dbo].[PositionTypes])
BEGIN
    INSERT INTO [dbo].[PositionTypes] ([Id], [Code], [Name], [SortOrder]) VALUES
    (1, N'Developer', N'Developer', 1),
    (2, N'UxDesigner', N'UX Designer', 2),
    (3, N'Product', N'Product', 3),
    (4, N'QualityAssurance', N'Quality Assurance', 4);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ExperienceLevels])
BEGIN
    INSERT INTO [dbo].[ExperienceLevels] ([Id], [Code], [Name], [SortOrder]) VALUES
    (1, N'Junior', N'Junior', 1),
    (2, N'Mid', N'Mid', 2),
    (3, N'Senior', N'Senior', 3),
    (4, N'Lead', N'Lead', 4),
    (5, N'Architect', N'Architect', 5);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[RoleSpecialties])
BEGIN
    INSERT INTO [dbo].[RoleSpecialties] ([Id], [PositionTypeId], [Code], [Name]) VALUES
    (1, 3, N'ScrumMaster', N'Scrum Master'),
    (2, 3, N'BusinessAnalyst', N'Business Analyst'),
    (3, 3, N'ProductOwner', N'Product Owner');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills])
BEGIN
    INSERT INTO [dbo].[Skills] ([Name]) VALUES
    (N'React'), (N'TypeScript'), (N'C#'), (N'SQL'), (N'.NET'), (N'Azure'), (N'Git'),
    (N'ASP.NET'), (N'SQL Server'), (N'Kafka'), (N'Docker'), (N'System Design'),
    (N'Mentoring'), (N'Architecture'), (N'Integration'), (N'Security'), (N'Cloud'),
    (N'Figma'), (N'User Research'), (N'Prototyping'), (N'Design Systems'),
    (N'Accessibility'), (N'Workshops'), (N'Design Ops'),
    (N'Scrum'), (N'Kanban'), (N'Jira'), (N'Facilitation'),
    (N'Requirements'), (N'Process Mapping'), (N'Roadmapping'), (N'Discovery'),
    (N'Stakeholder Mgmt'), (N'Metrics'),
    (N'Manual Testing'), (N'Test Cases'), (N'Postman'), (N'Playwright'),
    (N'API Testing'), (N'CI/CD'), (N'Quality Strategy'), (N'Test Architecture'),
    (N'Performance'), (N'Security Testing'), (N'Coaching');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[EmployeeProfiles])
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @id1 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id2 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id3 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id4 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id5 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id6 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id7 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id8 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id9 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id10 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id11 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id12 UNIQUEIDENTIFIER = NEWID();
    DECLARE @id13 UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [dbo].[EmployeeProfiles]
    (
        [Id], [FirstName], [LastName], [DisplayName], [PositionTypeId], [ExperienceLevelId], [RoleSpecialtyId],
        [Title], [Summary], [YearsExperience], [Location], [Availability], [CreatedDate], [ModifiedDate]
    )
    VALUES
    (@id1, N'Ava', N'Chen', N'Ava Chen', 1, 1, NULL,
     N'Junior Full-Stack Developer', N'Early-career engineer focused on React and C# APIs.',
     1, N'Remote', N'Available', @now, @now),
    (@id2, N'Marco', N'Diaz', N'Marco Diaz', 1, 2, NULL,
     N'Software Engineer', N'Delivers features across web stacks with solid testing habits.',
     4, N'New York, NY', N'Available', @now, @now),
    (@id3, N'Priya', N'Nair', N'Priya Nair', 1, 3, NULL,
     N'Senior Software Engineer', N'Leads delivery of complex services and mentors mid-level engineers.',
     8, N'Austin, TX', N'Available', @now, @now),
    (@id4, N'James', N'Okafor', N'James Okafor', 1, 4, NULL,
     N'Engineering Lead', N'Technical lead for multi-squad delivery with architecture ownership.',
     11, N'Chicago, IL', N'On assignment', @now, @now),
    (@id5, N'Sofia', N'Reyes', N'Sofia Reyes', 1, 5, NULL,
     N'Solution Architect', N'Designs enterprise platforms and guides technology strategy.',
     15, N'Remote', N'Available', @now, @now),
    (@id6, N'Lena', N'Park', N'Lena Park', 2, 2, NULL,
     N'UX Designer', N'Creates user flows and high-fidelity prototypes for B2B products.',
     5, N'Seattle, WA', N'Available', @now, @now),
    (@id7, N'Tom', N'Nguyen', N'Tom Nguyen', 2, 3, NULL,
     N'Senior UX Designer', N'Owns end-to-end experience design for complex workflows.',
     9, N'Remote', N'Available', @now, @now),
    (@id8, N'Hannah', N'Brooks', N'Hannah Brooks', 3, NULL, 1,
     N'Scrum Master', N'Facilitates agile ceremonies and removes delivery blockers.',
     6, N'Denver, CO', N'Available', @now, @now),
    (@id9, N'Omar', N'Saleh', N'Omar Saleh', 3, NULL, 2,
     N'Business Analyst', N'Translates stakeholder needs into clear acceptance criteria.',
     7, N'Boston, MA', N'Available', @now, @now),
    (@id10, N'Elena', N'Vasquez', N'Elena Vasquez', 3, NULL, 3,
     N'Product Owner', N'Owns roadmap prioritization and outcome-focused delivery.',
     10, N'Remote', N'On assignment', @now, @now),
    (@id11, N'Noah', N'Kim', N'Noah Kim', 4, 1, NULL,
     N'Junior QA Engineer', N'Manual and exploratory testing with growing automation skills.',
     2, N'Remote', N'Available', @now, @now),
    (@id12, N'Rita', N'Coleman', N'Rita Coleman', 4, 3, NULL,
     N'Senior QA Engineer', N'Builds automation suites and quality gates for CI pipelines.',
     9, N'Atlanta, GA', N'Available', @now, @now),
    (@id13, N'Devin', N'Walsh', N'Devin Walsh', 4, 5, NULL,
     N'QA Architect', N'Defines test architecture and quality standards across products.',
     14, N'Remote', N'Available', @now, @now);

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id1, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'React', N'TypeScript', N'C#', N'SQL');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id2, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'.NET', N'React', N'Azure', N'Git');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id3, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'C#', N'ASP.NET', N'SQL Server', N'Kafka', N'Docker');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id4, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'.NET', N'System Design', N'Azure', N'Mentoring');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id5, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Architecture', N'Integration', N'Security', N'Cloud');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id6, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Figma', N'User Research', N'Prototyping', N'Design Systems');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id7, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Figma', N'Accessibility', N'Workshops', N'Design Ops');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id8, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Scrum', N'Kanban', N'Jira', N'Facilitation');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id9, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Requirements', N'Process Mapping', N'SQL', N'Workshops');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id10, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Roadmapping', N'Discovery', N'Stakeholder Mgmt', N'Metrics');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id11, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Manual Testing', N'Test Cases', N'Postman');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id12, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Playwright', N'API Testing', N'CI/CD', N'Quality Strategy');

    INSERT INTO [dbo].[EmployeeProfileSkills] ([EmployeeProfileId], [SkillId])
    SELECT @id13, [Id] FROM [dbo].[Skills] WHERE [Name] IN (N'Test Architecture', N'Performance', N'Security Testing', N'Coaching');
END
GO

-- Contract lookups and demo engagement brief (idempotent).

IF NOT EXISTS (SELECT 1 FROM [dbo].[ContractWorkModes])
BEGIN
    INSERT INTO [dbo].[ContractWorkModes] ([Id], [Code], [Name], [SortOrder]) VALUES
    (1, N'Remote', N'Remote', 1),
    (2, N'Hybrid', N'Hybrid', 2),
    (3, N'Onsite', N'Onsite', 3);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ContractEngagementTypes])
BEGIN
    INSERT INTO [dbo].[ContractEngagementTypes] ([Id], [Code], [Name], [SortOrder]) VALUES
    (1, N'FixedBid', N'Fixed bid', 1),
    (2, N'TimeAndMaterials', N'Time and materials', 2),
    (3, N'StaffAugmentation', N'Staff augmentation', 3);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ContractSkillPriorities])
BEGIN
    INSERT INTO [dbo].[ContractSkillPriorities] ([Id], [Code], [Name], [SortOrder]) VALUES
    (1, N'MustHave', N'Must have', 1),
    (2, N'NiceToHave', N'Nice to have', 2);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ContractConstraintTypes])
BEGIN
    INSERT INTO [dbo].[ContractConstraintTypes] ([Id], [Code], [Name], [SortOrder]) VALUES
    (1, N'OnshoreOnly', N'Onshore only', 1),
    (2, N'SecurityClearance', N'Security clearance', 2),
    (3, N'LegacyIntegration', N'Legacy integration', 3),
    (4, N'ComplianceAudit', N'Compliance audit', 4),
    (5, N'FixedBudgetBand', N'Fixed budget band', 5);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ContractDeliveryRiskLevels])
BEGIN
    INSERT INTO [dbo].[ContractDeliveryRiskLevels] ([Id], [Code], [Name], [SortOrder], [ConfidenceFactor]) VALUES
    (1, N'Low', N'Low', 1, 0.95),
    (2, N'Medium', N'Medium', 2, 0.80),
    (3, N'High', N'High', 3, 0.60);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ContractStrategicValueLevels])
BEGIN
    INSERT INTO [dbo].[ContractStrategicValueLevels] ([Id], [Code], [Name], [SortOrder]) VALUES
    (1, N'Low', N'Low', 1),
    (2, N'Medium', N'Medium', 2),
    (3, N'High', N'High', 3),
    (4, N'VeryHigh', N'Very high', 4);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Contracts])
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @startDate DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);

    DECLARE @claimsId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
    DECLARE @mobileId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
    DECLARE @analyticsId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

    DECLARE @claimsTarget DATE = DATEADD(WEEK, 26, @startDate);
    DECLARE @mobileTarget DATE = DATEADD(WEEK, 16, @startDate);
    DECLARE @analyticsTarget DATE = DATEADD(WEEK, 32, @startDate);

    INSERT INTO [dbo].[Contracts]
    (
        [Id], [Code], [Title], [ClientName], [OutcomeSummary], [ScopeSummary],
        [EngagementTypeId], [WorkModeId], [DurationWeeks], [StartDate], [TargetDeliveryDate],
        [EstimatedContractValue], [EstimatedProfit], [EstimatedMarginPercent], [WinProbabilityPercent],
        [DeliveryRiskId], [StrategicValueId], [StaffingFte], [SpecialistStaffingNeeded],
        [IsDefault], [CreatedDate], [ModifiedDate]
    )
    VALUES
    (
        @claimsId,
        N'NW-CLAIMS-2026',
        N'Northwind Claims Modernization',
        N'Northwind Insurance',
        N'Replace legacy claims intake with a cloud-native portal and APIs without disrupting adjuster workflows.',
        N'Deliver a claims intake portal, modernized service APIs, and cutover from the mainframe batch feed. Team must balance greenfield UX with brittle legacy integrations, tight audit requirements, and a fixed delivery window.',
        1, -- FixedBid
        2, -- Hybrid
        26,
        @startDate,
        @claimsTarget,
        3200000.00, -- high revenue / strategic stretch
        480000.00,
        15.00,
        80.00,
        2, -- Medium risk
        3, -- High strategic
        12.0,
        N'2 claims domain architects',
        1,
        @now,
        @now
    ),
    (
        @mobileId,
        N'CT-MOBILE-2026',
        N'Contoso Retail Mobile Refresh',
        N'Contoso Retail',
        N'Rebuild the shopper mobile app with faster checkout and accessible browsing across iOS and Android.',
        N'Deliver a React Native storefront refresh, API BFF for catalog/cart, and a staged rollout. Emphasis on UX quality, performance budgets, and store release cadence under time-and-materials.',
        2, -- TimeAndMaterials
        1, -- Remote
        16,
        @startDate,
        @mobileTarget,
        1800000.00, -- strong margin / high certainty
        450000.00,
        25.00,
        95.00,
        1, -- Low risk
        2, -- Medium strategic
        8.0,
        N'1 accessibility lead',
        0,
        @now,
        @now
    ),
    (
        @analyticsId,
        N'FB-ANALYTICS-2026',
        N'Fabrikam Analytics Hub',
        N'Fabrikam Manufacturing',
        N'Stand up a governed analytics platform that turns plant and ERP telemetry into trusted operational dashboards.',
        N'Deliver ingestion pipelines, a curated warehouse layer, and role-based dashboards for plant ops. Constraints include onshore delivery, compliance audit readiness, and integration with aging ERP extracts.',
        3, -- StaffAugmentation
        2, -- Hybrid
        32,
        @startDate,
        @analyticsTarget,
        2600000.00, -- balanced profit with specialist demand / higher risk
        572000.00,
        22.00,
        65.00,
        3, -- High risk
        4, -- Very high strategic
        10.0,
        N'3 data platform architects',
        0,
        @now,
        @now
    );

    -- Northwind claims skills / constraints / deliverables / milestones
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @claimsId, [Id], 1 FROM [dbo].[Skills]
    WHERE [Name] IN (N'.NET', N'C#', N'React', N'TypeScript', N'SQL Server', N'Azure');

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @claimsId, [Id], 2 FROM [dbo].[Skills]
    WHERE [Name] IN (N'Kafka', N'Docker', N'Playwright', N'Accessibility', N'System Design');

    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES
    (@claimsId, 1), (@claimsId, 2), (@claimsId, 3), (@claimsId, 5);

    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @claimsId, 1, N'Claims intake portal MVP', N'Adjuster and claimant flows for FNOL with role-based access.'),
    (NEWID(), @claimsId, 2, N'Claims domain APIs', N'Read/write services for intake, status, and document metadata.'),
    (NEWID(), @claimsId, 3, N'Legacy feed cutover plan', N'Parallel-run strategy from mainframe batch to event-friendly APIs.'),
    (NEWID(), @claimsId, 4, N'UAT package and runbooks', N'Test evidence, rollback steps, and ops handover notes.');

    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @claimsId, 1, N'Discovery complete', DATEADD(WEEK, 4, @startDate), N'Stakeholder map, integration inventory, and risk register.'),
    (NEWID(), @claimsId, 2, N'Portal MVP demo', DATEADD(WEEK, 12, @startDate), N'End-to-end FNOL happy path in staging.'),
    (NEWID(), @claimsId, 3, N'API cutover dry run', DATEADD(WEEK, 20, @startDate), N'Parallel feed validation with ops sign-off.'),
    (NEWID(), @claimsId, 4, N'Production handoff', @claimsTarget, N'Final UAT, security checklist, and warranty kickoff.');

    -- Contoso mobile
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @mobileId, [Id], 1 FROM [dbo].[Skills]
    WHERE [Name] IN (N'React', N'TypeScript', N'Figma', N'Accessibility', N'CI/CD');

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @mobileId, [Id], 2 FROM [dbo].[Skills]
    WHERE [Name] IN (N'User Research', N'Prototyping', N'Design Systems', N'Playwright', N'Performance');

    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES
    (@mobileId, 4), (@mobileId, 5);

    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @mobileId, 1, N'Mobile storefront MVP', N'Browse, search, and checkout on iOS/Android with shared design system.'),
    (NEWID(), @mobileId, 2, N'Catalog/cart BFF', N'Edge API aggregating catalog, pricing, and cart services.'),
    (NEWID(), @mobileId, 3, N'Accessibility pass', N'WCAG-oriented audit findings and remediation backlog.'),
    (NEWID(), @mobileId, 4, N'Release playbook', N'Staged rollout, feature flags, and store submission checklist.');

    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @mobileId, 1, N'Design system freeze', DATEADD(WEEK, 3, @startDate), N'Component kit and content patterns approved.'),
    (NEWID(), @mobileId, 2, N'Beta build', DATEADD(WEEK, 9, @startDate), N'Internal pilot with instrumented funnel metrics.'),
    (NEWID(), @mobileId, 3, N'Store soft launch', DATEADD(WEEK, 13, @startDate), N'Limited geography release.'),
    (NEWID(), @mobileId, 4, N'General availability', @mobileTarget, N'Full rollout and warranty start.');

    -- Fabrikam analytics
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @analyticsId, [Id], 1 FROM [dbo].[Skills]
    WHERE [Name] IN (N'SQL', N'SQL Server', N'Azure', N'.NET', N'Metrics');

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @analyticsId, [Id], 2 FROM [dbo].[Skills]
    WHERE [Name] IN (N'Kafka', N'Docker', N'System Design', N'Security', N'Requirements');

    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES
    (@analyticsId, 1), (@analyticsId, 3), (@analyticsId, 4);

    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @analyticsId, 1, N'Ingestion pipelines', N'Reliable plant and ERP extract landing with data quality checks.'),
    (NEWID(), @analyticsId, 2, N'Curated warehouse model', N'Conformed dimensions and facts for ops KPIs.'),
    (NEWID(), @analyticsId, 3, N'Ops dashboard pack', N'Role-based views for supervisors and plant managers.'),
    (NEWID(), @analyticsId, 4, N'Governance kit', N'Access model, lineage notes, and audit evidence package.');

    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @analyticsId, 1, N'Source inventory', DATEADD(WEEK, 5, @startDate), N'ERP and plant feed catalog with owners.'),
    (NEWID(), @analyticsId, 2, N'Warehouse MVP', DATEADD(WEEK, 14, @startDate), N'Core facts available for pilot dashboards.'),
    (NEWID(), @analyticsId, 3, N'Pilot plant go-live', DATEADD(WEEK, 22, @startDate), N'One plant using production dashboards.'),
    (NEWID(), @analyticsId, 4, N'Program handoff', @analyticsTarget, N'Augmented staff transition and runbooks.');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Teams])
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @claimsId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
    DECLARE @mobileId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
    DECLARE @analyticsId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

    DECLARE @teamClaimsPortal UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA1';
    DECLARE @teamClaimsCutover UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA2';
    DECLARE @teamMobileDelivery UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBB1';
    DECLARE @teamAnalyticsPod UNIQUEIDENTIFIER = 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCC1';

    INSERT INTO [dbo].[Teams] ([Id], [Name], [ContractId], [CreatedDate], [ModifiedDate]) VALUES
    (@teamClaimsPortal, N'Claims Portal Squad', @claimsId, @now, @now),
    (@teamClaimsCutover, N'Integration Cutover', @claimsId, @now, @now),
    (@teamMobileDelivery, N'Mobile Delivery', @mobileId, @now, @now),
    (@teamAnalyticsPod, N'Data Platform Pod', @analyticsId, @now, @now);

    INSERT INTO [dbo].[TeamPositionRequirements] ([TeamId], [PositionTypeId], [RequiredCount])
    SELECT t.[Id], p.[Id],
        CASE
            WHEN t.[Id] = @teamClaimsPortal AND p.[Code] = N'Developer' THEN 3
            WHEN t.[Id] = @teamClaimsPortal AND p.[Code] = N'UxDesigner' THEN 1
            WHEN t.[Id] = @teamClaimsPortal AND p.[Code] = N'Product' THEN 1
            WHEN t.[Id] = @teamClaimsPortal AND p.[Code] = N'QualityAssurance' THEN 1
            WHEN t.[Id] = @teamClaimsCutover AND p.[Code] = N'Developer' THEN 2
            WHEN t.[Id] = @teamClaimsCutover AND p.[Code] = N'Product' THEN 1
            WHEN t.[Id] = @teamClaimsCutover AND p.[Code] = N'QualityAssurance' THEN 1
            WHEN t.[Id] = @teamMobileDelivery AND p.[Code] = N'Developer' THEN 2
            WHEN t.[Id] = @teamMobileDelivery AND p.[Code] = N'UxDesigner' THEN 2
            WHEN t.[Id] = @teamMobileDelivery AND p.[Code] = N'QualityAssurance' THEN 1
            WHEN t.[Id] = @teamAnalyticsPod AND p.[Code] = N'Developer' THEN 3
            WHEN t.[Id] = @teamAnalyticsPod AND p.[Code] = N'Product' THEN 1
            ELSE 0
        END
    FROM [dbo].[Teams] t
    CROSS JOIN [dbo].[PositionTypes] p
    WHERE t.[Id] IN (@teamClaimsPortal, @teamClaimsCutover, @teamMobileDelivery, @teamAnalyticsPod);
END
GO
