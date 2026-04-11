using FitTracker.Models;

namespace FitTracker.Services;

public static class ProfileSetupHelper
{
    public static bool RequiresSetup(ApplicationUser? user)
    {
        return user != null && string.IsNullOrWhiteSpace(user.Goals);
    }
}
