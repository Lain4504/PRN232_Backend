namespace AISAM.Data.Enumeration
{
    public enum ProfileStatusEnum
    {
        Pending = 0,    // Profile created but no active subscription
        Active = 1,     // Profile has active subscription
        Suspended = 2,  // Profile suspended due to payment issues
        Cancelled = 3   // Profile cancelled/deleted by user
    }
}
