using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Tourism_Project.Models
{
    public class AgentTourCommission
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public int TourCodeID { get; set; }
        public string TourCodeName { get; set; }
        public float? Commission { get; set; }
        public int active_f { get; set; }
    }

    public class AgentTourContext : DbContext
    {
        public DbSet<AgentTourCommission> AgentTourCommissions { get; set; }
    }
}