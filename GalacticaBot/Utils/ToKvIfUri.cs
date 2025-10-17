namespace GalacticaBot.Utils;

public static class ToKvIfUri
{
    // Normalize DATABASE_URL: accept both URI form and key/value form
    public static string Convert(string cs)
    {
        if (
            !Uri.TryCreate(cs, UriKind.Absolute, out var uri)
            || !(
                uri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            // Assume it's already a regular Npgsql connection string
            return cs;
        }

        // Username and password may be URL-encoded
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var host = uri.Host;
        var port = uri.IsDefaultPort ? 5432 : uri.Port;
        var database = uri.AbsolutePath.TrimStart('/');

        // Map common query parameters (currently sslmode only)
        var kv = new List<string>
        {
            $"Host={host}",
            $"Port={port}",
            $"Database={database}",
            $"Username={username}",
        };
        if (!string.IsNullOrEmpty(password))
            kv.Add($"Password={password}");

        string? GetQueryValue(Uri u, string key)
        {
            if (string.IsNullOrEmpty(u.Query))
                return null;
            var parts = u.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            return parts
                .Select(part => part.Split('=', 2))
                .Where(kvp =>
                    kvp.Length == 2 && kvp[0].Equals(key, StringComparison.OrdinalIgnoreCase)
                )
                .Select(kvp => Uri.UnescapeDataString(kvp[1].Replace('+', ' ')))
                .FirstOrDefault();
        }

        var sslmode = GetQueryValue(uri, "sslmode");
        if (!string.IsNullOrWhiteSpace(sslmode))
        {
            kv.Add($"SSL Mode={sslmode}");
        }

        return string.Join(';', kv);
    }
}
