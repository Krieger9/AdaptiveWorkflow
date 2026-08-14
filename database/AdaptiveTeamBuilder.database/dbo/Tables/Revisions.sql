CREATE TABLE [dbo].[Revisions]
(
	[Id]           BIGINT           IDENTITY (1, 1) NOT NULL,
	[UserId]       UNIQUEIDENTIFIER NOT NULL,
	[SurfacePath]  NVARCHAR(512)    NOT NULL,
	[Kind]         NVARCHAR(32)     NOT NULL CONSTRAINT [DF_Revisions_Kind] DEFAULT (N'revised'),
	[Reason]       NVARCHAR(MAX)    NOT NULL,
	[TurnDigestId] BIGINT           NULL,
	[CreatedAt]    DATETIME2(7)     NOT NULL,
	CONSTRAINT [PK_Revisions] PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [FK_Revisions_Users]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION,
	CONSTRAINT [FK_Revisions_TurnDigest]
		FOREIGN KEY ([TurnDigestId]) REFERENCES [dbo].[TurnDigests] ([Id]) ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_Revisions_UserId_CreatedAt]
	ON [dbo].[Revisions] ([UserId] ASC, [CreatedAt] ASC);
GO
