using GorgoDresses.Common.Helpers;
using GorgoDresses.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Data;

public class GorgoDressesDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public DbSet<Dress> Dresses { get; set; }

    public GorgoDressesDbContext(DbContextOptions options) : base(options)
    {
        Database.Migrate();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //modelBuilder.ApplyConfiguration(new RoleConfiguration());
        //modelBuilder.ApplyConfiguration(new MealsConfiguration());
    }

    public async Task SeedData()
    {
        var rolesExist = await Roles.AnyAsync();

        if (!rolesExist)
        {
            await Roles.AddRangeAsync(new List<IdentityRole<Guid>> {
                new IdentityRole<Guid>
                {
                    Name = UserRoleConstants.Administrator,
                    NormalizedName = "ADMINISTRATOR"
                },
                new IdentityRole<Guid>
                {
                    Name = UserRoleConstants.User,
                    NormalizedName = "USER"
                }
            });

            await SaveChangesAsync();
        }
    }
}
