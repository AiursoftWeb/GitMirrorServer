using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Aiursoft.GitMirrorServer.Entities;

public class MirrorRepoExecution
{
    [Key]
    public Guid Id { get; init; }

    public required Guid JobExecutionId { get; set; }

    [ForeignKey(nameof(JobExecutionId))]
    [JsonIgnore]
    [NotNull]
    public MirrorJobExecution? JobExecution { get; set; }

    [MaxLength(100)]
    public required string FromOrg { get; set; }
    
    [MaxLength(100)]
    public required string RepoName { get; set; }
    
    [MaxLength(100)]
    public required string TargetOrg { get; set; }

    public bool IsSuccess { get; set; }
    
    [MaxLength(100000)] 
    public string? Log { get; set; }

    [MaxLength(5000)]
    public string? ErrorMessage { get; set; }
}
