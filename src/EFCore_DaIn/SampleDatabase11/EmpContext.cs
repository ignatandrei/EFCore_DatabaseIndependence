using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SampleDatabase11;

[RSCG_DBContext.GenerateDbContextExists]
public partial class EmpContext : DbContext
{
    public EmpContext()
    {
    }

    public EmpContext(DbContextOptions<EmpContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Department> Department { get; set; }
    public virtual DbSet<Employee> Employee { get; set; }
}
