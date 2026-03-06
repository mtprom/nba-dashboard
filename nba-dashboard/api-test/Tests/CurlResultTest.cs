namespace ApiTest.Tests;

/// <summary>
/// Unit-style tests for block detection heuristics.
/// Defines a local CurlResult struct and validates classification of
/// HTTP status codes, Akamai HTML responses, and curl transport errors.
/// No network calls — pure in-memory assertions.
/// </summary>
public static class CurlResultTest
{
    // ── Local CurlResult (mirrors what will go into Infrastructure) ─────

    private readonly record struct CurlResult(string Body, int HttpStatusCode, int CurlExitCode)
    {
        public bool IsSuccess => HttpStatusCode >= 200 && HttpStatusCode < 300 && CurlExitCode == 0
                              && !IsAkamaiBlock;

        public bool IsRateLimited => HttpStatusCode == 429;

        public bool IsBlocked => HttpStatusCode == 403
                              || HttpStatusCode == 503
                              || IsAkamaiBlock;

        public bool IsTransientFailure => HttpStatusCode >= 500
                                       || HttpStatusCode == 429
                                       || IsBlocked;

        public bool IsAkamaiBlock
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Body)) return false;
                var trimmed = Body.TrimStart();
                return trimmed.StartsWith("<!")
                    || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("Reference #", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── Test runner ─────────────────────────────────────────────────────

    public static async Task RunAsync()
    {
        Console.WriteLine("=== CurlResult Block Detection Tests ===\n");

        // 1. Normal 200 with JSON → success
        Console.Write("200 OK + JSON body → IsSuccess... ");
        var ok = new CurlResult("{\"resultSets\": []}", 200, 0);
        Assert(ok.IsSuccess, "Should be success");
        Assert(!ok.IsBlocked, "Should not be blocked");
        Assert(!ok.IsRateLimited, "Should not be rate limited");
        Assert(!ok.IsTransientFailure, "Should not be transient");
        Console.WriteLine("PASS");

        // 2. HTTP 403 → blocked
        Console.Write("HTTP 403 → IsBlocked... ");
        var forbidden = new CurlResult("Forbidden", 403, 0);
        Assert(!forbidden.IsSuccess, "Should not be success");
        Assert(forbidden.IsBlocked, "Should be blocked");
        Assert(forbidden.IsTransientFailure, "Should be transient");
        Console.WriteLine("PASS");

        // 3. HTTP 429 → rate limited
        Console.Write("HTTP 429 → IsRateLimited... ");
        var limited = new CurlResult("", 429, 0);
        Assert(!limited.IsSuccess, "Should not be success");
        Assert(limited.IsRateLimited, "Should be rate limited");
        Assert(limited.IsTransientFailure, "Should be transient");
        Console.WriteLine("PASS");

        // 4. HTTP 503 → blocked (service unavailable)
        Console.Write("HTTP 503 → IsBlocked... ");
        var unavailable = new CurlResult("Service Unavailable", 503, 0);
        Assert(unavailable.IsBlocked, "Should be blocked");
        Assert(unavailable.IsTransientFailure, "Should be transient");
        Console.WriteLine("PASS");

        // 5. 200 with Akamai HTML "Access Denied" → blocked
        Console.Write("200 + Akamai 'Access Denied' HTML → IsAkamaiBlock... ");
        var akamaiDenied = new CurlResult(
            "<html><body>Access Denied - Reference #abc123</body></html>", 200, 0);
        Assert(akamaiDenied.IsAkamaiBlock, "Should detect Akamai block");
        Assert(akamaiDenied.IsBlocked, "Should be blocked");
        Assert(!akamaiDenied.IsSuccess, "Should NOT be success despite HTTP 200");
        Console.WriteLine("PASS");

        // 6. 200 with <!DOCTYPE html> → Akamai block
        Console.Write("200 + <!DOCTYPE html> → IsAkamaiBlock... ");
        var doctype = new CurlResult("<!DOCTYPE html><html><head></head><body>Error</body></html>", 200, 0);
        Assert(doctype.IsAkamaiBlock, "Should detect HTML doctype as block");
        Assert(!doctype.IsSuccess, "Should NOT be success");
        Console.WriteLine("PASS");

        // 7. 200 with <html> tag (no doctype) → Akamai block
        Console.Write("200 + <html> tag → IsAkamaiBlock... ");
        var htmlTag = new CurlResult("<html>some error page</html>", 200, 0);
        Assert(htmlTag.IsAkamaiBlock, "Should detect <html> as block");
        Console.WriteLine("PASS");

        // 8. 200 with "Reference #" → Akamai block
        Console.Write("200 + 'Reference #' in body → IsAkamaiBlock... ");
        var refHash = new CurlResult("Error. Reference #18.abc123.def456", 200, 0);
        Assert(refHash.IsAkamaiBlock, "Should detect Reference # as Akamai");
        Console.WriteLine("PASS");

        // 9. curl transport error (non-zero exit) → not success
        Console.Write("curl exit 28 (timeout) → not success... ");
        var timeout = new CurlResult("", 0, 28);
        Assert(!timeout.IsSuccess, "Should not be success");
        Console.WriteLine("PASS");

        // 10. curl exit 7 (connection refused) → not success
        Console.Write("curl exit 7 (conn refused) → not success... ");
        var connRefused = new CurlResult("", 0, 7);
        Assert(!connRefused.IsSuccess, "Should not be success");
        Console.WriteLine("PASS");

        // 11. HTTP 500 → transient failure (not blocked)
        Console.Write("HTTP 500 → IsTransientFailure but not IsBlocked... ");
        var server500 = new CurlResult("Internal Server Error", 500, 0);
        Assert(server500.IsTransientFailure, "Should be transient");
        Assert(!server500.IsBlocked, "500 is not a block");
        Assert(!server500.IsRateLimited, "500 is not rate limited");
        Console.WriteLine("PASS");

        // 12. Empty body with 200 → success (some endpoints return empty)
        Console.Write("200 + empty body → IsSuccess... ");
        var empty200 = new CurlResult("", 200, 0);
        Assert(empty200.IsSuccess, "Empty body with 200 should still be success");
        Console.WriteLine("PASS");

        // 13. Normal JSON shouldn't false-positive as Akamai
        Console.Write("Normal JSON body → NOT IsAkamaiBlock... ");
        var normalJson = new CurlResult(
            "{\"resource\":\"boxscoretraditionalv3\",\"parameters\":{\"GameID\":\"0022400001\"}}", 200, 0);
        Assert(!normalJson.IsAkamaiBlock, "JSON should not trigger Akamai detection");
        Assert(normalJson.IsSuccess, "Should be success");
        Console.WriteLine("PASS");

        Console.WriteLine($"\nAll CurlResult tests passed.");

        await Task.CompletedTask;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"ASSERTION FAILED: {message}");
    }
}
