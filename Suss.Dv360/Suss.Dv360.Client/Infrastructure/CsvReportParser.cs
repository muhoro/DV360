using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Models;

namespace Suss.Dv360.Client.Infrastructure;

/// <summary>
/// Parses Bid Manager CSV report files into strongly-typed row data.
/// <para>
/// Handles the mapping between CSV column headers and <see cref="CampaignReportRow"/>
/// properties. Supports both in-memory parsing and streaming for large files.
/// </para>
/// </summary>
/// <param name="logger">Logger for diagnostic output.</param>
internal sealed class CsvReportParser(ILogger<CsvReportParser> logger) : IReportParser
{
    // Column header mappings (Bid Manager CSV headers to property setters)
    private static readonly Dictionary<string, Action<CampaignReportRow, string>> ColumnMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Dimensions (Bid Manager uses these column names)
        ["Date"] = (row, value) => row.Date = ParseDate(value),
        ["Advertiser ID"] = (row, value) => row.AdvertiserId = ParseLong(value),
        ["Advertiser"] = (row, value) => row.AdvertiserName = value,
        ["Campaign ID"] = (row, value) => row.CampaignId = ParseLong(value),
        ["Campaign"] = (row, value) => row.CampaignName = value,
        ["Insertion Order ID"] = (row, value) => row.InsertionOrderId = ParseLong(value),
        ["Insertion Order"] = (row, value) => row.InsertionOrderName = value,
        ["Line Item ID"] = (row, value) => row.LineItemId = ParseLong(value),
        ["Line Item"] = (row, value) => row.LineItemName = value,
        ["Creative ID"] = (row, value) => row.CreativeId = ParseLong(value),
        ["Creative"] = (row, value) => row.CreativeName = value,
        ["Exchange ID"] = (row, value) => row.ExchangeId = ParseLong(value),
        ["Exchange"] = (row, value) => row.ExchangeName = value,
        ["Advertiser Currency"] = (row, value) => row.AdvertiserCurrency = value,  // FILTER_ADVERTISER_CURRENCY
        ["Domain"] = (row, value) => row.Domain = value,                            // FILTER_DOMAIN
        ["App/URL"] = (row, value) => row.AppUrl = value,                           // FILTER_APP_URL

        // Delivery metrics
        ["Impressions"] = (row, value) => row.Impressions = ParseLong(value) ?? 0,
        ["Clicks"] = (row, value) => row.Clicks = ParseLong(value) ?? 0,
        ["Media Cost (Advertiser Currency)"] = (row, value) => row.MediaCostMicros = ParseMicros(value),  // METRIC_MEDIA_COST_ADVERTISER
        ["Media Cost (USD)"] = (row, value) => row.MediaCostMicros = ParseMicros(value),                  // METRIC_MEDIA_COST_USD (placement-level reports)
        ["Revenue (Advertiser Currency)"] = (row, value) => row.RevenueMicros = ParseMicros(value),

        // Performance metrics (Bid Manager calculated fields)
        ["Click Rate (CTR)"] = (row, value) => row.Ctr = ParseDecimal(value) ?? 0,  // METRIC_CTR actual header
        ["CTR"] = (row, value) => row.Ctr = ParseDecimal(value) ?? 0,
        ["Click Through Rate"] = (row, value) => row.Ctr = ParseDecimal(value) ?? 0,
        ["CPC (Advertiser Currency)"] = (row, value) => row.CpcMicros = ParseMicros(value),
        ["CPM (Advertiser Currency)"] = (row, value) => row.CpmMicros = ParseMicros(value),

        // Conversion metrics
        ["Total Conversions"] = (row, value) => row.TotalConversions = ParseLong(value) ?? 0,
        ["Post-Click Conversions"] = (row, value) => row.PostClickConversions = ParseLong(value) ?? 0,
        ["Post-View Conversions"] = (row, value) => row.PostViewConversions = ParseLong(value) ?? 0,
        ["CPA (Advertiser Currency)"] = (row, value) => row.CpaMicros = ParseMicros(value),
        ["Post-Click Revenue"] = (row, value) => row.PostClickRevenueMicros = ParseMicros(value),
        ["Post-View Revenue"] = (row, value) => row.PostViewRevenueMicros = ParseMicros(value),

        // Video metrics
        ["TrueView Views"] = (row, value) => row.VideoViews = ParseLong(value) ?? 0,
        ["Video Completions"] = (row, value) => row.VideoCompletions = ParseLong(value) ?? 0,
    };

    /// <inheritdoc />
    public IReadOnlyList<CampaignReportRow> Parse(string csvContent, ReportGrouping groupBy)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
            return [];

        var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return [];

        // Parse footer metadata first so we can backfill rows that lack dimension columns
        // e.g. when a campaign is applied as a filter rather than a GroupBy dimension,
        // the campaign name only appears in the footer as "Filter by Campaign ID:,Name (ID)"
        var footer = ParseFooterMetadata(lines);

        // Parse header row to get column indices
        var headers = ParseCsvLine(lines[0]);
        var columnSetters = MapColumnsToSetters(headers);

        var rows = new List<CampaignReportRow>(lines.Length - 1);

        // Parse data rows (stop at the first footer/metadata line)
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];

            // Stop when we hit the report footer (totals row or metadata lines)
            if (string.IsNullOrWhiteSpace(line) ||
                line.StartsWith("Grand Total", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Report Time:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Date Range:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Group By:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Filter by", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("MRC Accredited", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Reporting numbers", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip summary/total rows (first column is empty = totals row in Bid Manager CSVs)
            var values = ParseCsvLine(line);
            if (values.Length > 0 && string.IsNullOrWhiteSpace(values[0]))
                continue;

            var row = ParseRow(values, columnSetters);

            // Backfill dimension values from footer metadata when not present as columns
            if (row.CampaignId is null && footer.CampaignId.HasValue)
                row.CampaignId = footer.CampaignId;
            if (row.CampaignName is null && footer.CampaignName is not null)
                row.CampaignName = footer.CampaignName;
            if (row.AdvertiserId is null && footer.AdvertiserId.HasValue)
                row.AdvertiserId = footer.AdvertiserId;
            if (row.AdvertiserName is null && footer.AdvertiserName is not null)
                row.AdvertiserName = footer.AdvertiserName;

            CalculateDerivedMetrics(row);
            rows.Add(row);
        }

        logger.LogDebug("Parsed {RowCount} rows from CSV report", rows.Count);
        return rows;
    }

    /// <summary>
    /// Parses footer metadata lines to extract filter context (e.g. campaign name/ID
    /// applied as filters rather than GroupBy dimensions).
    /// Footer lines follow the format: "Filter by Campaign ID:,Name (ID)"
    /// </summary>
    private static ReportFooterMetadata ParseFooterMetadata(string[] lines)
    {
        var meta = new ReportFooterMetadata();

        foreach (var line in lines)
        {
            if (line.StartsWith("Filter by Campaign ID:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = ParseCsvLine(line);
                if (parts.Length > 1)
                    ParseNameAndId(parts[1], out meta.CampaignName, out meta.CampaignId);
            }
            else if (line.StartsWith("Filter by Advertiser ID:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = ParseCsvLine(line);
                if (parts.Length > 1)
                    ParseNameAndId(parts[1], out meta.AdvertiserName, out meta.AdvertiserId);
            }
        }

        return meta;
    }

    /// <summary>
    /// Parses a footer value in the format "Name (ID)" into its name and numeric ID components.
    /// </summary>
    private static void ParseNameAndId(string value, out string? name, out long? id)
    {
        name = null;
        id = null;

        var trimmed = value.Trim();
        var parenOpen = trimmed.LastIndexOf('(');
        var parenClose = trimmed.LastIndexOf(')');

        if (parenOpen > 0 && parenClose > parenOpen)
        {
            name = trimmed[..parenOpen].Trim();
            var idStr = trimmed[(parenOpen + 1)..parenClose].Trim();
            if (long.TryParse(idStr, out var parsed))
                id = parsed;
        }
        else
        {
            name = trimmed;
        }
    }

    private sealed class ReportFooterMetadata
    {
        public string? CampaignName;
        public long? CampaignId;
        public string? AdvertiserName;
        public long? AdvertiserId;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CampaignReportRow> ParseStreamAsync(
        Stream csvStream,
        ReportGrouping groupBy,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream);

        // Read and parse header row
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
            yield break;

        var headers = ParseCsvLine(headerLine);
        var columnSetters = MapColumnsToSetters(headers);
        var rowCount = 0;

        // Stream data rows
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip empty lines and totals
            if (string.IsNullOrWhiteSpace(line) || 
                line.StartsWith("Grand Total", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Total", StringComparison.OrdinalIgnoreCase))
                continue;

            var values = ParseCsvLine(line);
            var row = ParseRow(values, columnSetters);
            CalculateDerivedMetrics(row);

            rowCount++;
            yield return row;
        }

        logger.LogDebug("Streamed {RowCount} rows from CSV report", rowCount);
    }

    /// <summary>
    /// Maps CSV column headers to property setter functions.
    /// </summary>
    private List<Action<CampaignReportRow, string>?> MapColumnsToSetters(string[] headers)
    {
        var setters = new List<Action<CampaignReportRow, string>?>(headers.Length);

        foreach (var header in headers)
        {
            if (ColumnMappings.TryGetValue(header.Trim(), out var setter))
            {
                setters.Add(setter);
            }
            else
            {
                logger.LogTrace("Unknown CSV column: {Column}", header);
                setters.Add(null);
            }
        }

        return setters;
    }

    /// <summary>
    /// Parses a single data row using the mapped setters.
    /// </summary>
    private static CampaignReportRow ParseRow(
        string[] values,
        List<Action<CampaignReportRow, string>?> columnSetters)
    {
        var row = new CampaignReportRow();

        for (var i = 0; i < values.Length && i < columnSetters.Count; i++)
        {
            var setter = columnSetters[i];
            if (setter is not null && !string.IsNullOrWhiteSpace(values[i]))
            {
                try
                {
                    setter(row, values[i]);
                }
                catch
                {
                    // Ignore parsing errors for individual cells
                }
            }
        }

        return row;
    }

    /// <summary>
    /// Calculates derived metrics that may not be directly in the CSV.
    /// </summary>
    private static void CalculateDerivedMetrics(CampaignReportRow row)
    {
        // Calculate CTR if not provided
        if (row.Ctr == 0 && row.Impressions > 0)
        {
            row.Ctr = (decimal)row.Clicks / row.Impressions;
        }

        // Calculate CPC if not provided
        if (row.CpcMicros == 0 && row.Clicks > 0)
        {
            row.CpcMicros = row.MediaCostMicros / row.Clicks;
        }

        // Calculate CPM if not provided
        if (row.CpmMicros == 0 && row.Impressions > 0)
        {
            row.CpmMicros = row.MediaCostMicros * 1000 / row.Impressions;
        }

        // Calculate CPA if not provided
        if (row.CpaMicros == 0 && row.TotalConversions > 0)
        {
            row.CpaMicros = row.MediaCostMicros / row.TotalConversions;
        }
    }

    /// <summary>
    /// Parses a CSV line handling quoted fields with commas.
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var fieldStart = 0;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(ExtractField(line, fieldStart, i));
                fieldStart = i + 1;
            }
        }

        // Add the last field
        fields.Add(ExtractField(line, fieldStart, line.Length));

        return [.. fields];
    }

    /// <summary>
    /// Extracts and cleans a CSV field value.
    /// </summary>
    private static string ExtractField(string line, int start, int end)
    {
        var field = line[start..end].Trim();

        // Remove surrounding quotes and unescape double quotes
        if (field.Length >= 2 && field[0] == '"' && field[^1] == '"')
        {
            field = field[1..^1].Replace("\"\"", "\"");
        }

        return field;
    }

    /// <summary>
    /// Parses a date string in various formats.
    /// </summary>
    private static DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Try common date formats used by Bid Manager
        string[] formats = ["yyyy-MM-dd", "yyyy/MM/dd", "M/d/yyyy", "MM/dd/yyyy"];

        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
        }

        // Fallback to general parsing
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        return null;
    }

    /// <summary>
    /// Parses a numeric string to a nullable long.
    /// </summary>
    private static long? ParseLong(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Remove commas and currency symbols
        var cleaned = value.Replace(",", "").Replace("$", "").Trim();

        if (long.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Parses a decimal string.
    /// </summary>
    private static decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Remove percentage sign and commas
        var cleaned = value.Replace("%", "").Replace(",", "").Trim();

        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            // If original value had %, convert to decimal
            if (value.Contains('%'))
                result /= 100;

            return result;
        }

        return null;
    }

    /// <summary>
    /// Parses a currency value to micros.
    /// </summary>
    private static long ParseMicros(string value)
    {
        var decimalValue = ParseDecimal(value);
        if (decimalValue is null)
            return 0;

        // Convert to micros (multiply by 1,000,000)
        return (long)(decimalValue.Value * 1_000_000);
    }
}
