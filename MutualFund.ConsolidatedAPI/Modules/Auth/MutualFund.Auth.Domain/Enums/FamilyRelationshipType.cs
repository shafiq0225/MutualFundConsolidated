namespace MutualFund.Auth.Domain.Enums
{
    /// <summary>
    /// A family member's relationship to the Head of Family.
    ///
    /// Self is reserved for the Head's own entry — it is never stored on
    /// a FamilyMember row. The Head is tracked separately via
    /// FamilyGroup.HeadUserId; "Self" is synthesized at query time so API
    /// consumers get one unified list (Head + dependents) instead of
    /// having to special-case the Head separately.
    ///
    /// Fixed set by design — add new values here as needed (requires a
    /// code change + migration, per confirmed design decision).
    /// </summary>
    public enum FamilyRelationshipType
    {
        Self = 0,
        Spouse = 1,
        Father = 2,
        Mother = 3,
        Son = 4,
        Daughter = 5,
        Brother = 6,
        Sister = 7,
        Other = 8
    }
}
