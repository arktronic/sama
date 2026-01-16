namespace SAMA.Web.Constants;

public static class AlertConstants
{
    public const int MaxFailureThreshold = 10;

    public const int MinFailureThreshold = 1;

    internal const int ConsecutiveFailureQueryLimit = MaxFailureThreshold * 2;
}
