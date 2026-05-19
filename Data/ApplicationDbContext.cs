using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBaguet.API.Data;
using SmartBaguet.API.Models;

using System.Collections.Generic;

namespace SmartBaguet.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
    }

    public DbSet<Baguet> Baguets => Set<Baguet>();

    public DbSet<Plant> Plants => Set<Plant>();

    public DbSet<BaguetHistory> BaguetHistories => Set<BaguetHistory>();
}