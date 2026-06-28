using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence
{
    public class ChainDegreeDbContext : DbContext
    {
        public ChainDegreeDbContext(DbContextOptions<ChainDegreeDbContext> options)
            : base(options)
        {
        }
    }
}
