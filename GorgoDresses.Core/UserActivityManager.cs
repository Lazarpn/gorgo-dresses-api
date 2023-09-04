using GorgoDresses.Common.Helpers;
using GorgoDresses.Data;
using GorgoDresses.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Core;
public class UserActivityManager
{
    private readonly GorgoDressesDbContext db;

    public UserActivityManager(GorgoDressesDbContext db)
    {
        this.db = db;
    }

    public async Task<User> GetUser(Guid userId)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<string> GetUserRole(Guid userId)
    {
        var roleRow = await db.UserRoles.FirstOrDefaultAsync(row => row.UserId == userId);
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleRow.RoleId);
        return role.Name;
    }
}
