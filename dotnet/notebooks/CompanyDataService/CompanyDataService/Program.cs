using Microsoft.AspNetCore.Hosting.Server;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

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

List<CompanyRecord> _companyData = new List<CompanyRecord>();

void LoadCompanyData(List<CompanyRecord> companyData)
{
    if (companyData.Count == 0)
    {
        var filePath = "company_data.csv";
        _companyData = LoadCsv(Path.Combine(app.Environment.ContentRootPath ,  filePath));
    }
}

var companyDataApi = app.MapGroup("/company_data");

companyDataApi.MapGet("/find/{company_text}", (string company_text) => {
    LoadCompanyData(_companyData);
    var company = _companyData.FirstOrDefault(c => (!string.IsNullOrEmpty(c.Ticker) && c.Ticker.ToUpper() == company_text.ToUpper()) || (!string.IsNullOrEmpty(c.Name) && c.Name.ToUpper().Contains(company_text.ToUpper())));
    if (company is { }) 
        return Results.Ok<int>(company.Id); 
    else 
        return Results.NotFound();
});

companyDataApi.MapGet("/{id}", (int id) => {
    LoadCompanyData(_companyData);
    if (_companyData.FirstOrDefault(a => a.Id == id) is { } companyRecord)
        return Results.Ok(companyRecord);
    else
        return Results.NotFound();
});

app.Run();

public record CompanyRecord
{
    public int Id { get; set; }
    public int Rank { get; set; }
    public string ?Name { get; set; }
    public string ?Industry { get; set; }
    public string ?City { get; set; }
    public string ?State { get; set; }
    public string ?Zip { get; set; }
    public string ?Website { get; set; }
    public int Employees { get; set; }
    public decimal RevenueInMillions { get; set; }
    public decimal ProfitInMillions { get; set; }
    public decimal ValuationInMillions { get; set; }
    public string ?Ticker { get; set; }
    public string ?CEO { get; set; }
}

[JsonSerializable(typeof(CompanyRecord[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
