CREATE TABLE [dbo].[TeamHiddenProfiles]
(
    [TeamId]              UNIQUEIDENTIFIER NOT NULL,
    [EmployeeProfileId]   UNIQUEIDENTIFIER NOT NULL,
    [HiddenDate]          DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_TeamHiddenProfiles] PRIMARY KEY CLUSTERED ([TeamId] ASC, [EmployeeProfileId] ASC),
    CONSTRAINT [FK_TeamHiddenProfiles_Teams]
        FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Teams] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TeamHiddenProfiles_EmployeeProfiles]
        FOREIGN KEY ([EmployeeProfileId]) REFERENCES [dbo].[EmployeeProfiles] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_TeamHiddenProfiles_EmployeeProfileId]
    ON [dbo].[TeamHiddenProfiles] ([EmployeeProfileId] ASC);
GO
