namespace NuGetGhostDetector.Infrastructure;

internal static class WellKnownPackageRules
{
    private static readonly Dictionary<string, string[]> PackageHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Newtonsoft.Json"] = ["Newtonsoft.Json", "Newtonsoft"],
        ["Serilog"] = ["Serilog", "Log.Logger"],
        ["Serilog.AspNetCore"] = ["Serilog", "UseSerilog"],
        ["Dapper"] = ["Dapper"],
        ["AutoMapper"] = ["AutoMapper"],
        ["AutoMapper.Extensions.Microsoft.DependencyInjection"] = ["AutoMapper", "AddAutoMapper"],
        ["FluentValidation"] = ["FluentValidation"],
        ["MediatR"] = ["MediatR"],
        ["Swashbuckle.AspNetCore"] = ["AddSwaggerGen", "UseSwagger", "SwaggerUI", "OpenApi"],
        ["Microsoft.EntityFrameworkCore"] = ["DbContext", "DbSet", "EntityFrameworkCore"],
        ["Microsoft.EntityFrameworkCore.SqlServer"] = ["UseSqlServer"],
        ["Microsoft.EntityFrameworkCore.Sqlite"] = ["UseSqlite"],
        ["Npgsql.EntityFrameworkCore.PostgreSQL"] = ["UseNpgsql", "Npgsql"],
        ["Pomelo.EntityFrameworkCore.MySql"] = ["UseMySql"],
        ["Microsoft.Extensions.DependencyInjection"] = ["IServiceCollection", "AddSingleton", "AddScoped", "AddTransient"],
        ["Microsoft.Extensions.Configuration"] = ["IConfiguration", "ConfigurationBuilder"],
        ["Microsoft.Extensions.Logging"] = ["ILogger", "ILoggerFactory"],
        ["Microsoft.AspNetCore.Authentication.JwtBearer"] = ["JwtBearer", "AddJwtBearer"],
        ["Polly"] = ["Polly", "Policy"],
        ["StackExchange.Redis"] = ["StackExchange.Redis", "ConnectionMultiplexer"],
        ["RabbitMQ.Client"] = ["RabbitMQ.Client", "ConnectionFactory"],
        ["Confluent.Kafka"] = ["Confluent.Kafka", "ProducerBuilder", "ConsumerBuilder"]
    };

    private static readonly string[] WeakTerms =
    [
        "Core",
        "Common",
        "Extensions",
        "Abstractions",
        "DependencyInjection",
        "Microsoft",
        "System",
        "Net",
        "Runtime",
        "Tools",
        "Build",
        "Hosting"
    ];

    public static IEnumerable<string> GetHints(string packageId)
    {
        var hints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (PackageHints.TryGetValue(packageId, out var mappedHints))
        {
            foreach (var hint in mappedHints)
            {
                hints.Add(hint);
            }
        }

        hints.Add(packageId);

        foreach (var part in packageId.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!WeakTerms.Contains(part, StringComparer.OrdinalIgnoreCase) && part.Length > 2)
            {
                hints.Add(part);
            }
        }

        return hints;
    }

    public static bool IsWeakPackage(string packageId)
        => packageId.Contains("Extensions", StringComparison.OrdinalIgnoreCase) ||
           packageId.Contains("DependencyInjection", StringComparison.OrdinalIgnoreCase) ||
           packageId.Contains("Hosting", StringComparison.OrdinalIgnoreCase);

    public static bool IsIgnoredInfrastructure(string packageId, IReadOnlySet<string> ignoredPackages)
    {
        if (ignoredPackages.Contains(packageId))
        {
            return true;
        }

        return packageId.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
               packageId.StartsWith("coverlet.", StringComparison.OrdinalIgnoreCase) ||
               packageId.EndsWith(".Analyzers", StringComparison.OrdinalIgnoreCase) ||
               packageId.EndsWith(".Analyzer", StringComparison.OrdinalIgnoreCase) ||
               packageId.EndsWith(".Build", StringComparison.OrdinalIgnoreCase) ||
               packageId.EndsWith(".Targets", StringComparison.OrdinalIgnoreCase) ||
               packageId.Contains("SourceGenerator", StringComparison.OrdinalIgnoreCase) ||
               packageId.Contains("SourceGenerators", StringComparison.OrdinalIgnoreCase) ||
               packageId.StartsWith("Microsoft.CodeAnalysis.", StringComparison.OrdinalIgnoreCase) ||
               packageId.StartsWith("Microsoft.VisualStudio.", StringComparison.OrdinalIgnoreCase);
    }
}
