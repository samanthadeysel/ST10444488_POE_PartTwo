using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ST10444488_POE.Models;

namespace ST10444488_POE.Data
{
    public class ST10444488_POEContext : DbContext
    {
        public ST10444488_POEContext (DbContextOptions<ST10444488_POEContext> options)
            : base(options)
        {
        }

        public DbSet<ST10444488_POE.Models.Customer> Customer { get; set; } = default!;
        public DbSet<ST10444488_POE.Models.Product> Product { get; set; } = default!;
        public DbSet<ST10444488_POE.Models.Order> Order { get; set; } = default!;
    }
}
