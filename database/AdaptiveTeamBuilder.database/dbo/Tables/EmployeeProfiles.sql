CREATE TABLE [dbo].[EmployeeProfiles]
(
    [Id]                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_EmployeeProfiles_Id] DEFAULT (NEWSEQUENTIALID()),
    [FirstName]           NVARCHAR(100)    NOT NULL,
    [LastName]            NVARCHAR(100)    NOT NULL,
    [DisplayName]         NVARCHAR(200)    NOT NULL,
    [PositionTypeId]      INT              NOT NULL,
    [ExperienceLevelId]   INT              NULL,
    [RoleSpecialtyId]     INT              NULL,
    [Title]               NVARCHAR(200)    NOT NULL,
    [Summary]             NVARCHAR(2000)   NULL,
    [YearsExperience]     INT              NULL,
    [Location]            NVARCHAR(200)    NULL,
    [Availability]        NVARCHAR(64)     NULL,
    [CreatedDate]         DATETIME2(7)     NOT NULL,
    [ModifiedDate]        DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_EmployeeProfiles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_EmployeeProfiles_PositionTypes]
        FOREIGN KEY ([PositionTypeId]) REFERENCES [dbo].[PositionTypes] ([Id]),
    CONSTRAINT [FK_EmployeeProfiles_ExperienceLevels]
        FOREIGN KEY ([ExperienceLevelId]) REFERENCES [dbo].[ExperienceLevels] ([Id]),
    CONSTRAINT [FK_EmployeeProfiles_RoleSpecialties]
        FOREIGN KEY ([RoleSpecialtyId]) REFERENCES [dbo].[RoleSpecialties] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_EmployeeProfiles_PositionTypeId]
    ON [dbo].[EmployeeProfiles] ([PositionTypeId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_EmployeeProfiles_DisplayName]
    ON [dbo].[EmployeeProfiles] ([DisplayName] ASC);
GO
