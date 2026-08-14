CREATE TABLE [dbo].[CollaborationStateChangeLogs]
(
	[Id]            BIGINT           IDENTITY (1, 1) NOT NULL,
	[UserId]        UNIQUEIDENTIFIER NOT NULL,
	[TurnDigestId]  BIGINT           NULL,
	[Reason]        NVARCHAR(MAX)    NOT NULL,
	[CreatedAt]     DATETIME2(7)     NOT NULL,
	CONSTRAINT [PK_CollaborationStateChangeLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [FK_CollaborationStateChangeLogs_Users]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION,
	CONSTRAINT [FK_CollaborationStateChangeLogs_TurnDigest]
		FOREIGN KEY ([TurnDigestId]) REFERENCES [dbo].[CollaborationTurnDigests] ([Id]) ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_CollaborationStateChangeLogs_UserId_CreatedAt]
	ON [dbo].[CollaborationStateChangeLogs] ([UserId] ASC, [CreatedAt] ASC);
GO
