-- Reference data and sample contractor profiles (idempotent).

IF NOT EXISTS (SELECT 1 FROM [dbo].[PositionTypes])
BEGIN
    INSERT INTO [dbo].[PositionTypes] ([Id], [Code], [Name]) VALUES
    (1, N'Developer', N'Developer'),
    (2, N'UxDesigner', N'UX Designer'),
    (3, N'Product', N'Product'),
    (4, N'QualityAssurance', N'Quality Assurance');
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
