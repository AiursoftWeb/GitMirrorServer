using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Aiursoft.GitMirrorServer.Entities;

public class MirrorRepoExecution
{
    [Key]
    public Guid Id { get; init; }

    [Required(ErrorMessage = "Job execution ID is required.")]
    [Display(Name = "Job execution ID")]
    public required Guid JobExecutionId { get; set; }

    [ForeignKey(nameof(JobExecutionId))]
    [JsonIgnore]
    [NotNull]
    [Display(Name = "Job execution")]
    public MirrorJobExecution? JobExecution { get; set; }

    [MaxLength(100)]
    [Required(ErrorMessage = "From organization is required.")]
    [Display(Name = "From organization")]
    public required string FromOrg { get; set; }
    
    [MaxLength(100)]
    [Required(ErrorMessage = "Repo name is required.")]
    [Display(Name = "Repo name")]
    public required string RepoName { get; set; }
    
    [MaxLength(100)]
    [Required(ErrorMessage = "Target organization is required.")]
    [Display(Name = "Target organization")]
    public required string TargetOrg { get; set; }

    [Display(Name = "Is success")]
    public bool IsSuccess { get; set; }
    
    [MaxLength(100000)] 
    [Display(Name = "Log")]
    public string? Log { get; set; }

    [MaxLength(5000)]
    [Display(Name = "Error message")]
    public string? ErrorMessage { get; set; }
}
