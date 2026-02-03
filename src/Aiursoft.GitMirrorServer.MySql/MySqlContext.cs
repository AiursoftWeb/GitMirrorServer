using System.Diagnostics.CodeAnalysis;
using Aiursoft.GitMirrorServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.MySql;

[ExcludeFromCodeCoverage]

public class MySqlContext(DbContextOptions<MySqlContext> options) : GitMirrorServerDbContext(options);
