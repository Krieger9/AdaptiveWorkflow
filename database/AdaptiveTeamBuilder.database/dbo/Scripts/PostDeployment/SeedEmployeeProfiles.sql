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

    -- Wave 1 (DemoSortOrder 1-3): Margin top2 = Mobile+Analytics; Profit top2 = Analytics+Claims;
    -- Value top2 = Claims+Analytics. Demo: expand highest Margin (Mobile, Analytics), select either.
    INSERT INTO [dbo].[Contracts]
    (
        [Id], [Code], [Title], [ClientName], [OutcomeSummary], [ScopeSummary],
        [EngagementTypeId], [WorkModeId], [DurationWeeks], [StartDate], [TargetDeliveryDate],
        [EstimatedContractValue], [EstimatedProfit], [EstimatedMarginPercent], [WinProbabilityPercent],
        [DeliveryRiskId], [StrategicValueId], [StaffingFte], [SpecialistStaffingNeeded],
        [IsDefault], [DemoSortOrder], [LastSelectedAt], [CreatedDate], [ModifiedDate]
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
        3200000.00, -- highest value, lowest margin in wave
        480000.00,
        15.00,
        80.00,
        2, -- Medium risk
        3, -- High strategic
        12.0,
        N'2 claims domain architects',
        1,
        1,
        NULL,
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
        1800000.00, -- lowest value, highest margin in wave
        450000.00,
        25.00,
        95.00,
        1, -- Low risk
        2, -- Medium strategic
        8.0,
        N'1 accessibility lead',
        0,
        2,
        NULL,
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
        2600000.00, -- mid value, highest profit, strong margin
        572000.00,
        22.00,
        65.00,
        3, -- High risk
        4, -- Very high strategic
        10.0,
        N'3 data platform architects',
        0,
        3,
        NULL,
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

-- ---------------------------------------------------------------------------
-- Collaboration demo pool: DemoSortOrder + six contrastive contracts (additive).
-- Wave 1 (1-3): Margin leaders Mobile+Analytics vs Profit Analytics+Claims vs Value Claims+Analytics.
-- Wave 2 (4-6): Margin Litware+Adventure vs Profit WideWorld+Adventure vs Value Adventure+WideWorld.
-- Wave 3 (7-9): Margin Alpine+Tailwind vs Profit Tailwind+Nod vs Value Nod+Tailwind.
-- Demo habit: expand the two highest-Margin cards in the visible trio, then select one of them.
-- ---------------------------------------------------------------------------

UPDATE [dbo].[Contracts] SET [DemoSortOrder] = 1 WHERE [Code] = N'NW-CLAIMS-2026' AND [DemoSortOrder] <> 1;
UPDATE [dbo].[Contracts] SET [DemoSortOrder] = 2 WHERE [Code] = N'CT-MOBILE-2026' AND [DemoSortOrder] <> 2;
UPDATE [dbo].[Contracts] SET [DemoSortOrder] = 3 WHERE [Code] = N'FB-ANALYTICS-2026' AND [DemoSortOrder] <> 3;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Contracts] WHERE [Code] = N'LW-PORTAL-2026')
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @startDate DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);
    DECLARE @id UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
    DECLARE @target DATE = DATEADD(WEEK, 14, @startDate);

    INSERT INTO [dbo].[Contracts]
    (
        [Id], [Code], [Title], [ClientName], [OutcomeSummary], [ScopeSummary],
        [EngagementTypeId], [WorkModeId], [DurationWeeks], [StartDate], [TargetDeliveryDate],
        [EstimatedContractValue], [EstimatedProfit], [EstimatedMarginPercent], [WinProbabilityPercent],
        [DeliveryRiskId], [StrategicValueId], [StaffingFte], [SpecialistStaffingNeeded],
        [IsDefault], [DemoSortOrder], [LastSelectedAt], [CreatedDate], [ModifiedDate]
    )
    VALUES
    (
        @id, N'LW-PORTAL-2026', N'Litware Partner Portal', N'Litware Inc',
        N'Ship a partner self-service portal that cuts onboarding cycle time without a large program budget.',
        N'Deliver partner registration, catalog access, and light approval workflows on a fixed bid with a lean team.',
        1, 1, 14, @startDate, @target,
        1000000.00, 280000.00, 28.00, 70.00,
        1, 2, 5.0, N'1 partner-domain BA',
        0, 4, NULL, @now, @now
    );

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 1 FROM [dbo].[Skills] WHERE [Name] IN (N'React', N'TypeScript', N'.NET', N'Azure');
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 2 FROM [dbo].[Skills] WHERE [Name] IN (N'Accessibility', N'CI/CD');
    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES (@id, 4), (@id, 5);
    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @id, 1, N'Partner MVP', N'Registration and catalog browse for invited partners.'),
    (NEWID(), @id, 2, N'Approval workflow', N'Light multi-step approvals with audit trail.');
    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @id, 1, N'Pilot partners', DATEADD(WEEK, 6, @startDate), N'First five partners live.'),
    (NEWID(), @id, 2, N'GA handoff', @target, N'Warranty and runbooks.');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Contracts] WHERE [Code] = N'WW-ERP-2026')
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @startDate DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);
    DECLARE @id UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555555';
    DECLARE @target DATE = DATEADD(WEEK, 36, @startDate);

    INSERT INTO [dbo].[Contracts]
    (
        [Id], [Code], [Title], [ClientName], [OutcomeSummary], [ScopeSummary],
        [EngagementTypeId], [WorkModeId], [DurationWeeks], [StartDate], [TargetDeliveryDate],
        [EstimatedContractValue], [EstimatedProfit], [EstimatedMarginPercent], [WinProbabilityPercent],
        [DeliveryRiskId], [StrategicValueId], [StaffingFte], [SpecialistStaffingNeeded],
        [IsDefault], [DemoSortOrder], [LastSelectedAt], [CreatedDate], [ModifiedDate]
    )
    VALUES
    (
        @id, N'WW-ERP-2026', N'Wide World ERP Cutover', N'Wide World Importers',
        N'Migrate regional ERP instances onto a single cloud ERP with heavy change management.',
        N'Large fixed-bid cutover: data migration, integration adapters, and dual-run hypercare across regions.',
        1, 2, 36, @startDate, @target,
        2400000.00, 700000.00, 12.00, 60.00,
        3, 3, 14.0, N'2 ERP cutover leads',
        0, 5, NULL, @now, @now
    );

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 1 FROM [dbo].[Skills] WHERE [Name] IN (N'.NET', N'SQL Server', N'Azure', N'System Design');
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 2 FROM [dbo].[Skills] WHERE [Name] IN (N'Kafka', N'Security', N'Requirements');
    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES (@id, 1), (@id, 2), (@id, 3);
    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @id, 1, N'Migration factory', N'Repeatable regional cutover playbooks.'),
    (NEWID(), @id, 2, N'Hypercare kit', N'War-room staffing and rollback criteria.');
    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @id, 1, N'First region live', DATEADD(WEEK, 16, @startDate), N'Pilot region cutover complete.'),
    (NEWID(), @id, 2, N'Program close', @target, N'All regions on single ERP.');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Contracts] WHERE [Code] = N'AW-LOYALTY-2026')
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @startDate DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);
    DECLARE @id UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666666';
    DECLARE @target DATE = DATEADD(WEEK, 24, @startDate);

    INSERT INTO [dbo].[Contracts]
    (
        [Id], [Code], [Title], [ClientName], [OutcomeSummary], [ScopeSummary],
        [EngagementTypeId], [WorkModeId], [DurationWeeks], [StartDate], [TargetDeliveryDate],
        [EstimatedContractValue], [EstimatedProfit], [EstimatedMarginPercent], [WinProbabilityPercent],
        [DeliveryRiskId], [StrategicValueId], [StaffingFte], [SpecialistStaffingNeeded],
        [IsDefault], [DemoSortOrder], [LastSelectedAt], [CreatedDate], [ModifiedDate]
    )
    VALUES
    (
        @id, N'AW-LOYALTY-2026', N'Adventure Works Loyalty Rebuild', N'Adventure Works Cycles',
        N'Rebuild the loyalty platform into a brand-wide engagement hub with high contract value.',
        N'T&M rebuild of points engine, offers, and mobile wallet surfaces with staged market rollout.',
        2, 1, 24, @startDate, @target,
        4000000.00, 400000.00, 20.00, 75.00,
        2, 4, 11.0, N'1 loyalty domain architect',
        0, 6, NULL, @now, @now
    );

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 1 FROM [dbo].[Skills] WHERE [Name] IN (N'React', N'TypeScript', N'.NET', N'SQL Server', N'Azure');
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 2 FROM [dbo].[Skills] WHERE [Name] IN (N'Kafka', N'Performance', N'Design Systems');
    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES (@id, 4), (@id, 5);
    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @id, 1, N'Points engine', N'Real-time accrual and redemption APIs.'),
    (NEWID(), @id, 2, N'Mobile wallet pack', N'Offer surfaces for iOS/Android.');
    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @id, 1, N'Engine beta', DATEADD(WEEK, 10, @startDate), N'Pilot market accrual live.'),
    (NEWID(), @id, 2, N'National launch', @target, N'All markets live.');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Contracts] WHERE [Code] = N'AS-EDGE-2026')
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @startDate DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);
    DECLARE @id UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777777';
    DECLARE @target DATE = DATEADD(WEEK, 12, @startDate);

    INSERT INTO [dbo].[Contracts]
    (
        [Id], [Code], [Title], [ClientName], [OutcomeSummary], [ScopeSummary],
        [EngagementTypeId], [WorkModeId], [DurationWeeks], [StartDate], [TargetDeliveryDate],
        [EstimatedContractValue], [EstimatedProfit], [EstimatedMarginPercent], [WinProbabilityPercent],
        [DeliveryRiskId], [StrategicValueId], [StaffingFte], [SpecialistStaffingNeeded],
        [IsDefault], [DemoSortOrder], [LastSelectedAt], [CreatedDate], [ModifiedDate]
    )
    VALUES
    (
        @id, N'AS-EDGE-2026', N'Alpine Ski Edge Analytics', N'Alpine Ski House',
        N'Stand up edge analytics for resort ops with a compact high-margin engagement.',
        N'Short staff-aug pod delivering edge ingest, local dashboards, and a thin cloud sync.',
        3, 1, 12, @startDate, @target,
        1200000.00, 360000.00, 30.00, 55.00,
        2, 2, 4.0, N'1 edge/IoT specialist',
        0, 7, NULL, @now, @now
    );

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 1 FROM [dbo].[Skills] WHERE [Name] IN (N'.NET', N'SQL', N'Azure', N'Metrics');
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 2 FROM [dbo].[Skills] WHERE [Name] IN (N'Docker', N'Kafka');
    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES (@id, 4);
    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @id, 1, N'Edge ingest', N'Resort-site collectors with local buffering.'),
    (NEWID(), @id, 2, N'Ops dashboards', N'Supervisor views for lift and lodge KPIs.');
    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @id, 1, N'Pilot lodge', DATEADD(WEEK, 5, @startDate), N'One lodge producing live metrics.'),
    (NEWID(), @id, 2, N'Handoff', @target, N'Runbooks and staff transition.');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Contracts] WHERE [Code] = N'TW-CHECKOUT-2026')
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @startDate DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);
    DECLARE @id UNIQUEIDENTIFIER = '88888888-8888-8888-8888-888888888888';
    DECLARE @target DATE = DATEADD(WEEK, 20, @startDate);

    INSERT INTO [dbo].[Contracts]
    (
        [Id], [Code], [Title], [ClientName], [OutcomeSummary], [ScopeSummary],
        [EngagementTypeId], [WorkModeId], [DurationWeeks], [StartDate], [TargetDeliveryDate],
        [EstimatedContractValue], [EstimatedProfit], [EstimatedMarginPercent], [WinProbabilityPercent],
        [DeliveryRiskId], [StrategicValueId], [StaffingFte], [SpecialistStaffingNeeded],
        [IsDefault], [DemoSortOrder], [LastSelectedAt], [CreatedDate], [ModifiedDate]
    )
    VALUES
    (
        @id, N'TW-CHECKOUT-2026', N'Tailwind Checkout Modernization', N'Tailwind Traders',
        N'Modernize checkout for higher conversion with strong profit and mid-high margin.',
        N'T&M rebuild of cart, payments, and fraud hooks with staged storefront rollout.',
        2, 2, 20, @startDate, @target,
        2000000.00, 480000.00, 24.00, 70.00,
        2, 3, 9.0, N'1 payments specialist',
        0, 8, NULL, @now, @now
    );

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 1 FROM [dbo].[Skills] WHERE [Name] IN (N'React', N'TypeScript', N'.NET', N'Azure', N'Security');
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 2 FROM [dbo].[Skills] WHERE [Name] IN (N'Performance', N'Playwright', N'CI/CD');
    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES (@id, 3), (@id, 5);
    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @id, 1, N'Checkout MVP', N'Cart through paid confirmation.'),
    (NEWID(), @id, 2, N'Fraud hooks', N'Risk scoring integration points.');
    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @id, 1, N'Beta storefront', DATEADD(WEEK, 9, @startDate), N'Internal traffic on new checkout.'),
    (NEWID(), @id, 2, N'GA', @target, N'Full traffic cutover.');
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Contracts] WHERE [Code] = N'NP-FLEET-2026')
BEGIN
    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @startDate DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);
    DECLARE @id UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999999';
    DECLARE @target DATE = DATEADD(WEEK, 28, @startDate);

    INSERT INTO [dbo].[Contracts]
    (
        [Id], [Code], [Title], [ClientName], [OutcomeSummary], [ScopeSummary],
        [EngagementTypeId], [WorkModeId], [DurationWeeks], [StartDate], [TargetDeliveryDate],
        [EstimatedContractValue], [EstimatedProfit], [EstimatedMarginPercent], [WinProbabilityPercent],
        [DeliveryRiskId], [StrategicValueId], [StaffingFte], [SpecialistStaffingNeeded],
        [IsDefault], [DemoSortOrder], [LastSelectedAt], [CreatedDate], [ModifiedDate]
    )
    VALUES
    (
        @id, N'NP-FLEET-2026', N'Nod Publishing Fleet Telemetry', N'Nod Publishers',
        N'Deploy fleet telemetry across distribution with high win probability and absolute profit.',
        N'Fixed-bid telemetry platform: device onboarding, routes, and exception alerting for logistics.',
        1, 2, 28, @startDate, @target,
        3500000.00, 630000.00, 14.00, 90.00,
        1, 3, 12.0, N'2 IoT platform engineers',
        0, 9, NULL, @now, @now
    );

    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 1 FROM [dbo].[Skills] WHERE [Name] IN (N'.NET', N'Azure', N'SQL Server', N'Kafka', N'Docker');
    INSERT INTO [dbo].[ContractSkills] ([ContractId], [SkillId], [PriorityId])
    SELECT @id, [Id], 2 FROM [dbo].[Skills] WHERE [Name] IN (N'System Design', N'Security', N'Metrics');
    INSERT INTO [dbo].[ContractConstraints] ([ContractId], [ConstraintTypeId]) VALUES (@id, 1), (@id, 3), (@id, 4);
    INSERT INTO [dbo].[ContractDeliverables] ([Id], [ContractId], [SortOrder], [Title], [Detail]) VALUES
    (NEWID(), @id, 1, N'Device onboarding', N'Secure provisioning for fleet units.'),
    (NEWID(), @id, 2, N'Exception alerting', N'Ops alerts for route and temperature breaches.');
    INSERT INTO [dbo].[ContractMilestones] ([Id], [ContractId], [SortOrder], [Name], [TargetDate], [Description]) VALUES
    (NEWID(), @id, 1, N'Pilot fleet', DATEADD(WEEK, 12, @startDate), N'One region streaming.'),
    (NEWID(), @id, 2, N'National coverage', @target, N'All regions live.');
END
GO
