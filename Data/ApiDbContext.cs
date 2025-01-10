using Microsoft.EntityFrameworkCore;
using BetterX_API.Models;

namespace BetterX_API.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
}
