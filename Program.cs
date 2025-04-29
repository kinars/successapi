using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace API;

public static class HtmlGenerator
{
    public static string GenerateHtmlTable(List<Time> timestamps)
    {
        var htmlBuilder = new StringBuilder();
        
        htmlBuilder.Append(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>Timestamp Database</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
        h1 { color: #333; text-align: center; }
    </style>
</head>
<body>
    <h1>Timestamp Database</h1>
    <table>
        <thead>
            <tr>
                <th>ID</th>
                <th>Timestamp</th>
                <th>Created At</th>
                <th>Time Difference</th>
                <th>Suggested Study Time (min/10)</th>
            </tr>
        </thead>
        <tbody>");

        Time previousTimestamp = null;
        
        foreach (var timestamp in timestamps)
        {
            string timeDiff = "N/A";
            string timeDiffMinutesDividedByTen = "N/A";
            
            if (previousTimestamp != null)
            {
                // Calculate time difference
                TimeSpan difference = timestamp.CreatedAt - previousTimestamp.CreatedAt;
                timeDiff = FormatTimeSpan(difference);
                
                // Calculate minutes and divide by 10
                double minutesDividedByTen = difference.TotalMinutes / 10;
                timeDiffMinutesDividedByTen = minutesDividedByTen.ToString("F2");
            }
            
            htmlBuilder.Append($@"
            <tr>
                <td>{timestamp.Id}</td>
                <td>{timestamp.TimeString}</td>
                <td>{timestamp.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}</td>
                <td>{timeDiff}</td>
                <td>{timeDiffMinutesDividedByTen}</td>
            </tr>");
            
            previousTimestamp = timestamp;
        }

        htmlBuilder.Append(@"
        </tbody>
    </table>
</body>
</html>");

        return htmlBuilder.ToString();
    }
    
    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
    }
}

// Time Data model
public class Time
{
    public int Id { get; init; }
    public required string TimeString { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

// Database context
public class TimeDbContext : DbContext
{
    public TimeDbContext(DbContextOptions<TimeDbContext> options) 
        : base(options) {}

    public DbSet<Time> TimeStamps { get; set; }
    
}

// Main Program
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddDbContext<TimeDbContext>(options =>
            options.UseInMemoryDatabase("ProductDb"));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        // Save Data to HTML
        app.MapGet("/export/html", async (TimeDbContext context) =>
            {
                var timestamps = await context.TimeStamps.OrderBy(t => t.CreatedAt).ToListAsync();
                var htmlContent = HtmlGenerator.GenerateHtmlTable(timestamps);
            
                return Results.Content(htmlContent, "text/html");
            })
            .WithName("ExportTimestampsToHtml");

        // POST create a new timestamp
        app.MapPost("/time/{apikey}", async (string apikey, TimeDbContext context) =>
            {
                // Validate API key
                if (apikey != "PRIVATEKEY_33314")
                {
                    return Results.Unauthorized();
                }

                // Create timestamp object
                var timestamp = new Time 
                { 
                    TimeString = DateTime.UtcNow.ToString("o") // ISO 8601 format
                };

                // Add to context and save
                context.TimeStamps.Add(timestamp);
                await context.SaveChangesAsync();

                return Results.Created($"/time/{timestamp.Id}", timestamp);
            })
            .WithName("LogSleep");
        app.Run();
    }
}