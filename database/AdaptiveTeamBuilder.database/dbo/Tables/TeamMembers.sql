CREATE TABLE [dbo].[TeamMembers]
(
    [TeamId]              UNIQUEIDENTIFIER NOT NULL,
    [EmployeeProfileId]   UNIQUEIDENTIFIER NOT NULL,
    [AddedDate]           DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_TeamMembers] PRIMARY KEY CLUSTERED ([TeamId] ASC, [EmployeeProfileId] ASC),
    CONSTRAINT [FK_TeamMembers_Teams]
        FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Teams] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TeamMembers_EmployeeProfiles]
        FOREIGN KEY ([EmployeeProfileId]) REFERENCES [dbo].[EmployeeProfiles] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_TeamMembers_EmployeeProfileId]
    ON [dbo].[TeamMembers] ([EmployeeProfileId] ASC);
GO
