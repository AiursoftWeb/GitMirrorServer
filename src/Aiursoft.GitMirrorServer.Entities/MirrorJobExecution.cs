using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aiursoft.GitMirrorServer.Entities;

public class MirrorJobExecution
{
    [Key]
    public Guid Id { get; init; }

    public DateTime StartTime { get; init; } = DateTime.UtcNow;
    
    public DateTime? EndTime { get; set; }

    public int SuccessCount { get; set; }
    
    public int FailureCount { get; set; }
    
    public int TotalCount { get; set; }

    public bool IsSuccess { get; set; } = true;

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    [InverseProperty(nameof(MirrorRepoExecution.JobExecution))]
    public IEnumerable<MirrorRepoExecution> RepoExecutions { get; init; } = new List<MirrorRepoExecution>();
}
