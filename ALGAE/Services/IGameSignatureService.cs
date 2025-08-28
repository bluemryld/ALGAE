using Algae.DAL.Models;

namespace ALGAE.Services
{
    public interface IGameSignatureService
    {
        /// <summary>
        /// Downloads the latest game signatures from the community repository
        /// </summary>
        Task<IEnumerable<GameSignature>> DownloadLatestSignaturesAsync();
        
        /// <summary>
        /// Validates a game signature for correctness and completeness
        /// </summary>
        Task<ValidationResult> ValidateSignatureAsync(GameSignature signature);
        
        /// <summary>
        /// Tests a signature against a specific executable file to verify it works
        /// </summary>
        Task<TestResult> TestSignatureAsync(GameSignature signature, string executablePath);
        
        /// <summary>
        /// Exports signatures to various formats (JSON, CSV, etc.)
        /// </summary>
        Task<string> ExportSignaturesAsync(IEnumerable<GameSignature> signatures, ExportFormat format);
        
        /// <summary>
        /// Imports signatures from various formats
        /// </summary>
        Task<IEnumerable<GameSignature>> ImportSignaturesAsync(string content, ImportFormat format);
        
        /// <summary>
        /// Prepares signature data for submission to the community repository
        /// </summary>
        Task<SubmissionData> PrepareSubmissionAsync(IEnumerable<GameSignature> signatures);
        
        /// <summary>
        /// Gets signature statistics and metadata
        /// </summary>
        Task<SignatureMetadata> GetSignatureMetadataAsync();
    }
    
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public ValidationLevel Level { get; set; }
    }
    
    public class TestResult
    {
        public bool Success { get; set; }
        public double ConfidenceScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
    }
    
    public class SubmissionData
    {
        public string JsonContent { get; set; } = string.Empty;
        public string MarkdownDescription { get; set; } = string.Empty;
        public List<string> PlatformTags { get; set; } = new();
        public string Category { get; set; } = string.Empty;
    }
    
    public class SignatureMetadata
    {
        public string Version { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public int TotalSignatures { get; set; }
        public Dictionary<string, int> Categories { get; set; } = new();
    }
    
    public enum ValidationLevel
    {
        Error,
        Warning,
        Info,
        Success
    }
    
    public enum ExportFormat
    {
        Json,
        Csv,
        Xml,
        SubmissionJson
    }
    
    public enum ImportFormat
    {
        Json,
        Csv,
        AlgaeJson
    }
}
