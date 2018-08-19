using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Tourism_Project.Models
{
    public class TourCodePrice
    {
        public int TourCodeID { get; set; }
        public string TourCodeName { get; set; }
        public float? Price { get; set; }
        public float? PriceChild { get; set; }
        public float? PriceFamilyChild { get; set; }
        public float? GoldPrice { get; set; }
        public float? GoldPriceChild { get; set; }
        public float? GoldPriceFamilyChild { get; set; }
        public string TourName { get; set; }
    }

    public class TourCodePriceContext : DbContext
    {
        public DbSet<TourCodePrice> TourCodePrices { get; set; }
    }
}