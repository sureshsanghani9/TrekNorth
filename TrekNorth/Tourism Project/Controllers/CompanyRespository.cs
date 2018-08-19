using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using Tourism_Project.Models;
using Trg.Data;

namespace Tourism_Project.Controllers
{
    public class CompanyRespository : EntityRepository<Company>
    {
        public CompanyRespository() : base(new VoucherEntities()) { }
    }
}