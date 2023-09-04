using AutoMapper;
using GorgoDresses.Common.Models.Dress;
using GorgoDresses.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Core;
public class DressManager
{
    private readonly GorgoDressesDbContext db;
    private readonly IMapper mapper;

    public DressManager(GorgoDressesDbContext db, IMapper mapper)
    {
        this.db = db;
        this.mapper = mapper;
    }

    public async Task<List<DressModel>> GetDresses()
    {
        var dresses = await db.Dresses.ToListAsync();
        var dressList =  mapper.Map<List<DressModel>>(dresses);
        return dressList;
    }

    public async Task CreateDress(DressCreateModel model)
    {

    }
}
