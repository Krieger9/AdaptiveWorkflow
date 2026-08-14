CREATE TABLE [dbo].[CollaborationTurnDigests]
(
	[Id]          BIGINT           IDENTITY (1, 1) NOT NULL,
	[UserId]      UNIQUEIDENTIFIER NOT NULL,
	[Sequence]    INT              NOT NULL,
	[CreatedAt]   DATETIME2(7)     NOT NULL,
	[DigestText]  NVARCHAR(MAX)    NOT NULL,
	CONSTRAINT [PK_CollaborationTurnDigests] PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [FK_CollaborationTurnDigests_Users]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_CollaborationTurnDigests_UserId_Sequence]
	ON [dbo].[CollaborationTurnDigests] ([UserId] ASC, [Sequence] ASC);
GO
