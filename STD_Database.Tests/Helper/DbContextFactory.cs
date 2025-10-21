using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STD_Database.Database;
using Microsoft.EntityFrameworkCore;

namespace STD_Database.Tests.Helper
{
    public static class DbContextFactory
    {
        public static StudentDB CreateInMemoryDB()
        {
            var options = new DbContextOptionsBuilder<StudentDB>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            return new StudentDB(options);
        }
    }
}
