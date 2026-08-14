CREATE TABLE [dbo].[BeliefDocuments]
(
	[UserId]     UNIQUEIDENTIFIER NOT NULL,
	[Tier]       NVARCHAR(32)     NOT NULL CONSTRAINT [DF_BeliefDocuments_Tier] DEFAULT (N'control'),
	[Document]   NVARCHAR(MAX)    NOT NULL,
	[Source]     NVARCHAR(32)     NOT NULL CONSTRAINT [DF_BeliefDocuments_Source] DEFAULT (N'app'),
	[Version]    INT              NOT NULL CONSTRAINT [DF_BeliefDocuments_Version] DEFAULT (0),
	[UpdatedAt]  DATETIME2(7)     NOT NULL,
	CONSTRAINT [PK_BeliefDocuments] PRIMARY KEY CLUSTERED ([UserId] ASC, [Tier] ASC),
	CONSTRAINT [FK_BeliefDocuments_Users]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);
GO
