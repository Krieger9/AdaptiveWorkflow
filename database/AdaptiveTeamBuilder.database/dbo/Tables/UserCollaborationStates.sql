CREATE TABLE [dbo].[UserCollaborationStates]
(
    [UserId]                 UNIQUEIDENTIFIER NOT NULL,
    [TendencyProse]          NVARCHAR(MAX)    NULL,
    [TendencySource]         NVARCHAR(32)     NOT NULL CONSTRAINT [DF_UserCollaborationStates_TendencySource] DEFAULT (N'stub'),
    [RecentTurnDigestsJson]  NVARCHAR(MAX)    NULL,
    [UpdatedAt]              DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_UserCollaborationStates] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_UserCollaborationStates_Users]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);
GO
