using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aiursoft.GitMirrorServer.Entities;

public class MirrorJobExecution
{
    [Key]
    public Guid Id { get; init; }

    [Display(Name = "Start time")]
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
    
    [Display(Name = "End time")]
    public DateTime? EndTime { get; set; }

    [Display(Name = "Success count")]
    public int SuccessCount { get; set; }
    
    [Display(Name = "Failure count")]
    public int FailureCount { get; set; }
    
    [Display(Name = "Total count")]
    public int TotalCount { get; set; }

    [Display(Name = "Is success")]
    public bool IsSuccess { get; set; } = true;

    [MaxLength(2000)]
    [Display(Name = "Error message")]
    public string? ErrorMessage { get; set; }

    [InverseProperty(nameof(MirrorRepoExecution.JobExecution))]
    [Display(Name = "Repo executions")]
    public IEnumerable<MirrorRepoExecution> RepoExecutions { get; init; } = new List<MirrorRepoExecution>();
}
