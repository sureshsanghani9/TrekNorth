﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Tourism_Project.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Objects;
    
    public partial class VoucherEntities : ObjectContext
    {
        public VoucherEntities()
            : base("name=VoucherEntities")
        {
        }
    
        public DbSet<Company> Companies { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<demo> demoes { get; set; }
    }
}
