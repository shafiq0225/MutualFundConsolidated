-- Composite index for historical NAV retrieval in Scheme API & Investment API queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DetailedSchemes_SchemeCode_NavDate' AND object_id = OBJECT_ID('DetailedSchemes'))
BEGIN
    CREATE INDEX IX_DetailedSchemes_SchemeCode_NavDate 
    ON DetailedSchemes (SchemeCode, NavDate DESC) 
    INCLUDE (NAV);
END;

-- Composite index for latest portfolio snapshot queries in Investment API
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PortfolioSnapshots_HoldingId_SnapshotDate' AND object_id = OBJECT_ID('PortfolioSnapshots'))
BEGIN
    CREATE INDEX IX_PortfolioSnapshots_HoldingId_SnapshotDate 
    ON PortfolioSnapshots (HoldingId, SnapshotDate DESC)
    INCLUDE (CurrentNAV, CurrentValue);
END;
