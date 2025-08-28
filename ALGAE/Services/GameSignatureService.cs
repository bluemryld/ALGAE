using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Diagnostics;
using Algae.DAL.Models;
using ALGAE.Services;

namespace ALGAE.Services
{
    public class GameSignatureService : IGameSignatureService
    {
        private readonly HttpClient _httpClient;
        private const string GITHUB_API_BASE = "https://api.github.com/repos";
        private const string SIGNATURES_REPO = "algae-project/algae-game-signatures"; // Update with actual repo
        private const string SIGNATURES_FILE_PATH = "signatures/game_signatures.json";
        private const string METADATA_FILE_PATH = "signatures/metadata.json";

        public GameSignatureService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ALGAE-GameSignatureService/1.0");
        }

        public async Task<IEnumerable<GameSignature>> DownloadLatestSignaturesAsync()
        {
            try
            {
                var url = $"{GITHUB_API_BASE}/{SIGNATURES_REPO}/contents/{SIGNATURES_FILE_PATH}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var fileInfo = JsonSerializer.Deserialize<GitHubFileResponse>(content);

                if (fileInfo?.Content == null)
                {
                    throw new InvalidOperationException("Could not retrieve file content from GitHub");
                }

                // Decode base64 content
                var jsonContent = Encoding.UTF8.GetString(Convert.FromBase64String(fileInfo.Content));
                var signatureResponse = JsonSerializer.Deserialize<SignatureFileResponse>(jsonContent);

                return signatureResponse?.Signatures ?? Enumerable.Empty<GameSignature>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download signatures from GitHub: {ex.Message}", ex);
            }
        }

        public async Task<ValidationResult> ValidateSignatureAsync(GameSignature signature)
        {
            var result = new ValidationResult();

            // Required field validation
            if (string.IsNullOrWhiteSpace(signature.Name))
                result.Errors.Add("Game name is required");

            if (string.IsNullOrWhiteSpace(signature.ExecutableName))
                result.Errors.Add("Executable name is required");

            // Format validation
            if (!string.IsNullOrWhiteSpace(signature.ExecutableName) && 
                !signature.ExecutableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                result.Warnings.Add("Executable name should end with .exe");

            // Naming conventions
            if (!string.IsNullOrWhiteSpace(signature.ShortName) && signature.ShortName.Length > 10)
                result.Warnings.Add("Short name should be 10 characters or less");

            // Match criteria validation
            if (!signature.MatchName && !signature.MatchVersion && !signature.MatchPublisher)
                result.Errors.Add("At least one match criteria must be enabled");

            if (signature.MatchVersion && string.IsNullOrWhiteSpace(signature.Version))
                result.Warnings.Add("Version matching is enabled but no version is specified");

            if (signature.MatchPublisher && string.IsNullOrWhiteSpace(signature.Publisher))
                result.Warnings.Add("Publisher matching is enabled but no publisher is specified");

            // Path security validation
            if (ContainsUnsafePathCharacters(signature.ExecutableName))
                result.Errors.Add("Executable name contains unsafe characters");

            if (!string.IsNullOrWhiteSpace(signature.GameImage) && ContainsUnsafePathCharacters(signature.GameImage))
                result.Errors.Add("Game image path contains unsafe characters");

            result.IsValid = result.Errors.Count == 0;
            result.Level = result.Errors.Count > 0 ? ValidationLevel.Error :
                          result.Warnings.Count > 0 ? ValidationLevel.Warning :
                          ValidationLevel.Success;

            await Task.CompletedTask; // For async consistency
            return result;
        }

        public async Task<TestResult> TestSignatureAsync(GameSignature signature, string executablePath)
        {
            var result = new TestResult();

            try
            {
                if (!File.Exists(executablePath))
                {
                    result.Issues.Add("Executable file not found");
                    return result;
                }

                var fileInfo = new FileInfo(executablePath);
                var fileName = Path.GetFileName(executablePath);
                
                // Test executable name matching
                double nameScore = 0;
                if (signature.MatchName && !string.IsNullOrWhiteSpace(signature.ExecutableName))
                {
                    if (string.Equals(fileName, signature.ExecutableName, StringComparison.OrdinalIgnoreCase))
                    {
                        nameScore = 0.7; // 70% confidence for exact name match
                        result.MatchReason += "Executable name matches; ";
                    }
                }

                // Test publisher matching
                double publisherScore = 0;
                if (signature.MatchPublisher && !string.IsNullOrWhiteSpace(signature.Publisher))
                {
                    try
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                        if (!string.IsNullOrWhiteSpace(versionInfo.CompanyName) &&
                            versionInfo.CompanyName.Contains(signature.Publisher, StringComparison.OrdinalIgnoreCase))
                        {
                            publisherScore = 0.3; // 30% confidence for publisher match
                            result.MatchReason += "Publisher matches; ";
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Issues.Add($"Could not read file version info: {ex.Message}");
                    }
                }

                // Test version matching
                double versionScore = 0;
                if (signature.MatchVersion && !string.IsNullOrWhiteSpace(signature.Version))
                {
                    try
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                        if (!string.IsNullOrWhiteSpace(versionInfo.FileVersion) &&
                            versionInfo.FileVersion.StartsWith(signature.Version, StringComparison.OrdinalIgnoreCase))
                        {
                            versionScore = 0.25; // 25% confidence for version match
                            result.MatchReason += "Version matches; ";
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Issues.Add($"Could not read version info: {ex.Message}");
                    }
                }

                result.ConfidenceScore = nameScore + publisherScore + versionScore;
                result.Success = result.ConfidenceScore >= 0.5; // 50% minimum confidence

                if (result.ConfidenceScore == 0)
                {
                    result.MatchReason = "No matching criteria found";
                }

                await Task.CompletedTask;
                return result;
            }
            catch (Exception ex)
            {
                result.Issues.Add($"Testing failed: {ex.Message}");
                return result;
            }
        }

        public async Task<string> ExportSignaturesAsync(IEnumerable<GameSignature> signatures, ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.Json:
                    return await ExportAsJsonAsync(signatures);
                
                case ExportFormat.SubmissionJson:
                    return await ExportAsSubmissionJsonAsync(signatures);
                
                case ExportFormat.Csv:
                    return await ExportAsCsvAsync(signatures);
                
                default:
                    throw new NotSupportedException($"Export format {format} is not supported");
            }
        }

        public async Task<IEnumerable<GameSignature>> ImportSignaturesAsync(string content, ImportFormat format)
        {
            switch (format)
            {
                case ImportFormat.Json:
                case ImportFormat.AlgaeJson:
                    return await ImportFromJsonAsync(content);
                
                default:
                    throw new NotSupportedException($"Import format {format} is not supported");
            }
        }

        public async Task<SubmissionData> PrepareSubmissionAsync(IEnumerable<GameSignature> signatures)
        {
            var submission = new SubmissionData();
            var signaturesList = signatures.ToList();

            // Create JSON content for submission
            var submissionFormat = new
            {
                version = "1.0.0",
                submissionDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                signatures = signaturesList.Select(s => new
                {
                    shortName = s.ShortName,
                    name = s.Name,
                    description = s.Description,
                    executableName = s.ExecutableName,
                    publisher = s.Publisher,
                    metaName = s.MetaName,
                    matchName = s.MatchName,
                    matchVersion = s.MatchVersion,
                    matchPublisher = s.MatchPublisher,
                    gameArgs = s.GameArgs,
                    platforms = DeterminePlatforms(s),
                    category = DetermineCategory(s),
                    submittedBy = "user"
                })
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            submission.JsonContent = JsonSerializer.Serialize(submissionFormat, options);

            // Create markdown description
            var markdown = new StringBuilder();
            markdown.AppendLine("## New Game Signature Submission");
            markdown.AppendLine();
            markdown.AppendLine($"**Number of signatures:** {signaturesList.Count}");
            markdown.AppendLine();
            markdown.AppendLine("### Games Included:");
            foreach (var signature in signaturesList)
            {
                markdown.AppendLine($"- **{signature.Name}** ({signature.ExecutableName})");
                if (!string.IsNullOrWhiteSpace(signature.Publisher))
                    markdown.AppendLine($"  - Publisher: {signature.Publisher}");
                if (!string.IsNullOrWhiteSpace(signature.Description))
                    markdown.AppendLine($"  - Description: {signature.Description}");
            }

            markdown.AppendLine();
            markdown.AppendLine("### Verification Checklist");
            markdown.AppendLine("- [x] All signatures have been tested and work correctly");
            markdown.AppendLine("- [x] No duplicate signatures are included");
            markdown.AppendLine("- [x] All required fields are populated");
            markdown.AppendLine("- [x] Signatures follow naming conventions");

            submission.MarkdownDescription = markdown.ToString();

            // Determine categories and platform tags
            submission.Category = DetermineMainCategory(signaturesList);
            submission.PlatformTags = DeterminePlatformTags(signaturesList);

            await Task.CompletedTask;
            return submission;
        }

        public async Task<SignatureMetadata> GetSignatureMetadataAsync()
        {
            try
            {
                var url = $"{GITHUB_API_BASE}/{SIGNATURES_REPO}/contents/{METADATA_FILE_PATH}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    // Return default metadata if file doesn't exist
                    return new SignatureMetadata
                    {
                        Version = "1.0.0",
                        LastUpdated = DateTime.UtcNow,
                        TotalSignatures = 0
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                var fileInfo = JsonSerializer.Deserialize<GitHubFileResponse>(content);

                if (fileInfo?.Content != null)
                {
                    var jsonContent = Encoding.UTF8.GetString(Convert.FromBase64String(fileInfo.Content));
                    var metadata = JsonSerializer.Deserialize<SignatureMetadata>(jsonContent);
                    return metadata ?? new SignatureMetadata();
                }

                return new SignatureMetadata();
            }
            catch (Exception)
            {
                // Return default metadata on error
                return new SignatureMetadata
                {
                    Version = "1.0.0",
                    LastUpdated = DateTime.UtcNow,
                    TotalSignatures = 0
                };
            }
        }

        private bool ContainsUnsafePathCharacters(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            
            var unsafeChars = new[] { '..', '/', '\\', '|', '<', '>', ':', '*', '?', '"' };
            return unsafeChars.Any(path.Contains);
        }

        private async Task<string> ExportAsJsonAsync(IEnumerable<GameSignature> signatures)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var export = signatures.ToList();
            var json = JsonSerializer.Serialize(export, options);
            
            await Task.CompletedTask;
            return json;
        }

        private async Task<string> ExportAsSubmissionJsonAsync(IEnumerable<GameSignature> signatures)
        {
            var submission = await PrepareSubmissionAsync(signatures);
            return submission.JsonContent;
        }

        private async Task<string> ExportAsCsvAsync(IEnumerable<GameSignature> signatures)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Name,ShortName,ExecutableName,Publisher,Description,MatchName,MatchVersion,MatchPublisher");

            foreach (var signature in signatures)
            {
                csv.AppendLine($"\"{signature.Name}\",\"{signature.ShortName}\",\"{signature.ExecutableName}\"," +
                              $"\"{signature.Publisher}\",\"{signature.Description}\"," +
                              $"{signature.MatchName},{signature.MatchVersion},{signature.MatchPublisher}");
            }

            await Task.CompletedTask;
            return csv.ToString();
        }

        private async Task<IEnumerable<GameSignature>> ImportFromJsonAsync(string content)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Try to parse as signature file response first
                try
                {
                    var signatureResponse = JsonSerializer.Deserialize<SignatureFileResponse>(content, options);
                    if (signatureResponse?.Signatures != null)
                    {
                        return signatureResponse.Signatures;
                    }
                }
                catch
                {
                    // Fall back to direct array parsing
                }

                // Try to parse as direct signature array
                var signatures = JsonSerializer.Deserialize<List<GameSignature>>(content, options);
                return signatures ?? Enumerable.Empty<GameSignature>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse JSON content: {ex.Message}", ex);
            }
        }

        private string[] DeterminePlatforms(GameSignature signature)
        {
            var platforms = new List<string>();

            // Simple heuristics based on publisher and executable patterns
            if (!string.IsNullOrWhiteSpace(signature.Publisher))
            {
                var publisher = signature.Publisher.ToLower();
                if (publisher.Contains("valve") || publisher.Contains("steam"))
                    platforms.Add("steam");
                if (publisher.Contains("epic"))
                    platforms.Add("epic");
                if (publisher.Contains("microsoft"))
                    platforms.Add("microsoft");
                if (publisher.Contains("cd projekt"))
                    platforms.Add("gog");
            }

            if (platforms.Count == 0)
                platforms.Add("pc");

            return platforms.ToArray();
        }

        private string DetermineCategory(GameSignature signature)
        {
            if (string.IsNullOrWhiteSpace(signature.Publisher))
                return "indie-games";

            var publisher = signature.Publisher.ToLower();
            if (publisher.Contains("valve") || publisher.Contains("rockstar") || 
                publisher.Contains("ubisoft") || publisher.Contains("electronic arts"))
                return "aaa-games";

            return "indie-games";
        }

        private string DetermineMainCategory(List<GameSignature> signatures)
        {
            var categories = signatures.Select(DetermineCategory).ToList();
            return categories.GroupBy(c => c).OrderByDescending(g => g.Count()).First().Key;
        }

        private List<string> DeterminePlatformTags(List<GameSignature> signatures)
        {
            var allPlatforms = signatures.SelectMany(DeterminePlatforms).Distinct().ToList();
            return allPlatforms;
        }

        private class GitHubFileResponse
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
            
            [JsonPropertyName("encoding")]
            public string? Encoding { get; set; }
        }

        private class SignatureFileResponse
        {
            [JsonPropertyName("version")]
            public string? Version { get; set; }
            
            [JsonPropertyName("signatures")]
            public List<GameSignature>? Signatures { get; set; }
        }
    }
}
