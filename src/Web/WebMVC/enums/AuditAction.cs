namespace WebMVC.enums
{
    public enum AuditAction
    {
        Unknown = 0, // failure scenario in a sense
        PlateUpdated = 1,
        PlateReserved = 2,
        PlateUnreserved = 3,
        PlateSold = 4
    }
}
