CREATE TABLE [dbo].[ContractDeliveryRiskLevels]
(
    [Id]                INT             NOT NULL,
    [Code]              NVARCHAR(32)    NOT NULL,
    [Name]              NVARCHAR(100)   NOT NULL,
    [SortOrder]         INT             NOT NULL,
    [ConfidenceFactor]  DECIMAL(4, 2)   NOT NULL,
    CONSTRAINT [PK_ContractDeliveryRiskLevels] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ContractDeliveryRiskLevels_Code] UNIQUE NONCLUSTERED ([Code] ASC),
    CONSTRAINT [CK_ContractDeliveryRiskLevels_ConfidenceFactor]
        CHECK ([ConfidenceFactor] > 0 AND [ConfidenceFactor] <= 1)
);
GO
