using System.Text.Json;

namespace Source.Core;

public static class AuditLogger
{
    public static void LogAuditToConsole(string stage, string transactionId, Dictionary<string, object> details)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ AUDIT LOG: {stage,-63} ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ Transaction ID: {transactionId,-56} ║");
        Console.WriteLine($"║ Timestamp:      {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC{"",-38} ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════╣");
        
        foreach (var kvp in details)
        {
            var value = kvp.Value?.ToString() ?? "null";
            var displayValue = value.Length > 56 ? value.Substring(0, 53) + "..." : value;
            Console.WriteLine($"║ {kvp.Key,-15}: {displayValue,-56} ║");
        }
        
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");
    }

    public static void LogAuditSuccess(string operation, string transactionId, string message)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ ✅ SUCCESS: {operation,-60} ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ Transaction ID: {transactionId,-56} ║");
        Console.WriteLine($"║ Message:        {message,-56} ║");
        Console.WriteLine($"║ Timestamp:      {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC{"",-38} ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");
    }

    public static void LogAuditFailure(string operation, string transactionId, string reason)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ ❌ FAILURE: {operation,-60} ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ Transaction ID: {transactionId,-56} ║");
        Console.WriteLine($"║ Reason:         {reason,-56} ║");
        Console.WriteLine($"║ Timestamp:      {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC{"",-38} ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");
    }

    public static void LogAuditWarning(string operation, string transactionId, string warning)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ ⚠️  WARNING: {operation,-59} ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ Transaction ID: {transactionId,-56} ║");
        Console.WriteLine($"║ Warning:        {warning,-56} ║");
        Console.WriteLine($"║ Timestamp:      {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC{"",-38} ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");
    }
}
