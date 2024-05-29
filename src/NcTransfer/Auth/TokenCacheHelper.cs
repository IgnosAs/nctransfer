using Microsoft.Identity.Client;
using System.Security.Cryptography;

namespace NcTransfer.Auth;

static class TokenCacheHelper
{
    /// <summary>
    /// Path to the token cache
    /// </summary>
    public static readonly string CacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".ignosmsalcache.bin3");

    private static readonly object FileLock = new object();

    public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
    {
        lock (FileLock)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                    ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath),
                        null,
                        DataProtectionScope.CurrentUser)
                    : null);
            }
        }
    }

    public static void AfterAccessNotification(TokenCacheNotificationArgs args)
    {
        // if the access operation resulted in a cache update
        if (args.HasStateChanged)
        {
            lock (FileLock)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // reflect changesgs in the persistent store
                    File.WriteAllBytes(CacheFilePath, ProtectedData.Protect(args.TokenCache.SerializeMsalV3(), null, DataProtectionScope.CurrentUser));
                }
            }
        }
    }

    internal static void EnableSerialization(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(BeforeAccessNotification);
        tokenCache.SetAfterAccess(AfterAccessNotification);
    }
}
