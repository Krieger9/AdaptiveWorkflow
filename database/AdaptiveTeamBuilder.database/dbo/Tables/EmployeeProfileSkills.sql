CREATE TABLE [dbo].[EmployeeProfileSkills]
(
    [EmployeeProfileId]  UNIQUEIDENTIFIER NOT NULL,
    [SkillId]            INT              NOT NULL,
    CONSTRAINT [PK_EmployeeProfileSkills] PRIMARY KEY CLUSTERED ([EmployeeProfileId] ASC, [SkillId] ASC),
    CONSTRAINT [FK_EmployeeProfileSkills_EmployeeProfiles]
        FOREIGN KEY ([EmployeeProfileId]) REFERENCES [dbo].[EmployeeProfiles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EmployeeProfileSkills_Skills]
        FOREIGN KEY ([SkillId]) REFERENCES [dbo].[Skills] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_EmployeeProfileSkills_SkillId]
    ON [dbo].[EmployeeProfileSkills] ([SkillId] ASC);
GO
