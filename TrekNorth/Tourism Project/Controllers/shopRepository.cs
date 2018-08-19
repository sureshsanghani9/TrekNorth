using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tourism_Project.Models;
using Trg.Data;

namespace Tourism_Project.Controllers
{
    public class shopRepository : EntityRepository<shop>
    {
        public shopRepository() : base(new VoucherEntities()) { }
    }
}