namespace MutualFund.Auth.Domain.Enums
{
    /// <summary>
    /// Master list of all permission codes in the system.
    /// Add new permission codes here as new features are added.
    /// </summary>
    public static class PermissionType
    {
        // SchemeEnrollment feature (includes fund approval) — single
        // blanket permission by design: an Employee either has access to
        // manage scheme enrollment or doesn't, no separate read/write tiers.
        public const string SchemeManage = "scheme.manage";

        // User Management — Admin only
        public const string UserManage = "user.manage";

        // Family Group Management
        public const string FamilyManage = "family.manage";

        // Orders Management
        public const string OrderView = "order.view";
        public const string OrderAdd = "order.add";

        // Investor & Snapshot Management
        public const string InvestorView = "investor.view";
        public const string InvestorSnapshot = "investor.snapshot";

        public static IEnumerable<string> GetAll() =>
        [
            SchemeManage,
            UserManage,
            FamilyManage,
            OrderView,
            OrderAdd,
            InvestorView,
            InvestorSnapshot
        ];
    }
}