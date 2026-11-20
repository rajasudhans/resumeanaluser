using System.Text;
using Microsoft.Extensions.AI;
using OllamaSharp;
using UglyToad.PdfPig;

//Connecting with Ollama Client
var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11434"),
    Timeout = TimeSpan.FromMinutes(10) // 100 வினாடிகளுக்குப் பதிலாக 10 நிமிடங்கள்
};

IChatClient chatClient = new OllamaApiClient(httpClient, "llama3.2");

string jobRequirement = """
    Role: Senior .NET Backend Developer
    Required Skills: C#, .NET 10, ASP.NET Core Web API, SQL Server, Entity Framework, Azure, Microservices.
    Experience: Minimum 4 years of backend development experience.
    """;


string resumesFolderPath = @"C:\Users\Rajasudhan\Desktop\Resumes";

if (!Directory.Exists(resumesFolderPath))
{
    Console.WriteLine("Resumes folder not found!");
    return;
}

string[] pdfFiles = Directory.GetFiles(resumesFolderPath, "*.pdf");
var results = new List<ResumeAnalysisResult>();

Console.WriteLine($"{pdfFiles.Length} resumes found.\n");

foreach (var filePath in pdfFiles)
{
    string fileName = Path.GetFileName(filePath);
    Console.WriteLine($"Reading: {fileName}...");

    string resumeText = ExtractTextFromPdf(filePath);
    var analysis = await AnalyzeResumeAsync(chatClient, jobRequirement, resumeText, fileName);
    results.Add(analysis);
}


var rankedResumes = results.OrderByDescending(r => r.Score).ToList();

Console.WriteLine("\n==================================================");
Console.WriteLine("   Matching Resource's profiles (Ranked)");
Console.WriteLine("==================================================\n");

foreach (var item in rankedResumes)
{
    Console.WriteLine($"Resume Name (File) : {item.FileName}");
    Console.WriteLine($"Score  : {item.Score} / 100");
    Console.WriteLine($"Reason    : {item.Reason}");
}



static string ExtractTextFromPdf(string pdfPath)
{
    var text = new StringBuilder();
    using PdfDocument document = PdfDocument.Open(pdfPath);
    foreach (var page in document.GetPages())
    {
        text.AppendLine(page.Text);
    }
    return text.ToString();
}

static async Task<ResumeAnalysisResult> AnalyzeResumeAsync(
    IChatClient client, string jobDesc, string resumeContent, string fileName)
{
    string prompt = $"""
        You are an expert technical recruiter. Compare the following Resume against the Job Requirement.
        Provide a matching score from 0 to 100 and a short 1-sentence reason in Tamil.

        Job Requirement:
        {jobDesc}

        Resume Content:
        {resumeContent}
        """;

    try
    {
        // .NET 10 Structured Output: AI பதிலை நேரடியாக C# Record-ஆக மாற்றுதல்
        var response = await client.GetResponseAsync<AiScoreResponse>(prompt);
        var result = response.Result;

        return new ResumeAnalysisResult(
            fileName,
            result?.Score ?? 0,
            result?.Reason ?? "Unable to analyze"
        );
    }
    catch (Exception ex)
    {
        return new ResumeAnalysisResult(fileName, 0, $"Error: {ex.Message}");
    }
}

// C# Records (தரவுகளை எளிமையாகக் கையாள)
public record AiScoreResponse(int Score, string Reason);
public record ResumeAnalysisResult(string FileName, int Score, string Reason);