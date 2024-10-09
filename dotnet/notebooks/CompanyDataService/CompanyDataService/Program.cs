using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

// Helper method to parse the records in the CSV file
static string[] ParseCsvLine(string line)
{
    var fields = new List<string>();
    var currentField = string.Empty;
    var inQuotes = false;

    foreach (var ch in line)
    {
        if (ch == ',' && !inQuotes)
        {
            fields.Add(currentField);
            currentField = string.Empty;
        }
        else if (ch == '"')
        {
            inQuotes = !inQuotes;
        }
        else
        {
            currentField += ch;
        }
    }

    fields.Add(currentField); // Add the last field
    return fields.ToArray();
}
// Helper method to read the CSV file and load the records into memory. This will be our 'database'.
static List<CompanyRecord> LoadCsv(string filePath)
{
    var records = new List<CompanyRecord>();
    var lines = File.ReadAllLines(filePath);

    foreach (var line in lines)
    {
        var fields = ParseCsvLine(line);
        var record = new CompanyRecord
        {
            Id = int.Parse(fields[0]),
            Rank = int.Parse(fields[1]),
            Name = fields[2],
            Industry = fields[3],
            City = fields[4],
            State = fields[5],
            Zip = fields[6],
            Website = fields[7],
            Employees = !string.IsNullOrEmpty(fields[8]) ? int.Parse(fields[8].Replace(",", "")) : 0,
            RevenueInMillions = !string.IsNullOrEmpty(fields[9]) ? decimal.Parse(fields[9].Replace("$", "").Replace(",", "")) : 0,
            ValuationInMillions = !string.IsNullOrEmpty(fields[10]) ? decimal.Parse(fields[10].Replace("$", "").Replace(",", "")) : 0,
            ProfitInMillions = !string.IsNullOrEmpty(fields[11]) ? decimal.Parse(fields[11].Replace("$", "").Replace(",", "")) : 0,
            Ticker = fields[13],
            CEO = fields[14]
        };
        records.Add(record);
    }

    return records;
}

// In-memory database for the company data
List<CompanyRecord> _companyData = new List<CompanyRecord>();

// Load the in-memory database, if it hasn't been loaded already.
void LoadCompanyData(List<CompanyRecord> companyData)
{
    // Lock this peice of code to ensure single-threaded access.

    lock (app)
    {
        if (companyData.Count == 0)
        {
            var filePath = "company_data.csv";
            _companyData = LoadCsv(Path.Combine(app.Environment.ContentRootPath, filePath));
        }
    }
}

// REST API to query the in-memory database
var companyDataApi = app.MapGroup("/company_data");

// GET method to find a company
companyDataApi.MapGet("/find/{company_text}", (string company_text) => {
    LoadCompanyData(_companyData);
    var company = _companyData.FirstOrDefault(c => (!string.IsNullOrEmpty(c.Ticker) && c.Ticker.ToUpper() == company_text.ToUpper()) || (!string.IsNullOrEmpty(c.Name) && c.Name.ToUpper().Contains(company_text.ToUpper())));
    if (company is { })
    {
        return Results.Ok<int>(company.Id);
    }
    return Results.NotFound();
});

// GET method to get the details of a company based on ID
companyDataApi.MapGet("/{id}", (int id) =>
{
    LoadCompanyData(_companyData);
    if (_companyData.FirstOrDefault(a => a.Id == id) is { } companyRecord)
    {
        return Results.Ok(companyRecord);
    }
    return Results.NotFound();
});

app.Run();

public record CompanyRecord
{
    /// <summary>
    /// Unique ID of this record
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// The Rank of the company in the Fortune 500 list
    /// </summary>
    public int Rank { get; set; }
    /// <summary>
    /// Name of the company
    /// </summary>
    public string ?Name { get; set; }
    /// <summary>
    /// The industry of the copany
    /// </summary>
    public string ?Industry { get; set; }
    /// <summary>
    /// City where the HQ is located.
    /// </summary>
    public string ?City { get; set; }
    /// <summary>
    /// State where the HQ is located
    /// </summary>
    public string ?State { get; set; }
    /// <summary>
    /// ZIP code of the HQ
    /// </summary>
    public string ?Zip { get; set; }
    /// <summary>
    /// Website URL
    /// </summary>
    public string ?Website { get; set; }
    /// <summary>
    /// Number of employees
    /// </summary>
    public int Employees { get; set; }
    /// <summary>
    /// Revenue in millions of US$
    /// </summary>
    public decimal RevenueInMillions { get; set; }
    /// <summary>
    /// Profit in millions of US$
    /// </summary>
    public decimal ProfitInMillions { get; set; }
    /// <summary>
    ///  Valuation in millions of US$
    /// </summary>
    public decimal ValuationInMillions { get; set; }
    /// <summary>
    ///    Stock ticker symbol
    /// </summary>
    public string ?Ticker { get; set; }
    /// <summary>
    /// Name of the CEO
    /// </summary>
    public string ?CEO { get; set; }
}

[JsonSerializable(typeof(CompanyRecord[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
