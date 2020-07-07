using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaksNet.Models
{
    public class EmployeeContext : DbContext
    {
        public EmployeeContext()
            : base("DbConnection")
        { }

        public DbSet<Employee> Employees { get; set; }

    }
}
