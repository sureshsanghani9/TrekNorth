//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class Company
    {
        public Company()
        {
            this.Vouchers = new HashSet<Voucher>();
        }
    
        public int CompanyID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Nullable<decimal> Commission { get; set; }
        public string Create_By { get; set; }
        public System.DateTime Create_Date { get; set; }
        public string Modify_By { get; set; }
        public Nullable<System.DateTime> Modify_Date { get; set; }
        public bool IsActive { get; set; }
    
        public virtual ICollection<Voucher> Vouchers { get; set; }
    }
}
