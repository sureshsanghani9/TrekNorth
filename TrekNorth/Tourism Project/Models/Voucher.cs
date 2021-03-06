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
    
    public partial class Voucher
    {
        public int VoucherBookingID { get; set; }
        public string VoucherID { get; set; }
        public int CompanyID { get; set; }
        public Nullable<System.DateTime> TravelDate { get; set; }
        public string Tour { get; set; }
        public string FareBasis { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RoomNumber { get; set; }
        public Nullable<int> AdultCount { get; set; }
        public Nullable<int> ChildrenCount { get; set; }
        public Nullable<int> InfantCount { get; set; }
        public string PickupLocation { get; set; }
        public Nullable<int> UserID { get; set; }
        public string Comments { get; set; }
        public Nullable<decimal> Price { get; set; }
        public Nullable<decimal> Levy { get; set; }
        public Nullable<decimal> Discount { get; set; }
        public Nullable<decimal> Commission { get; set; }
        public string Create_By { get; set; }
        public System.DateTime Create_Date { get; set; }
        public string Modify_By { get; set; }
        public Nullable<System.DateTime> Modify_Date { get; set; }
        public bool IsActive { get; set; }
        public string ConfirmationNumber { get; set; }
        public string paymenttype { get; set; }
        public string salesfrom { get; set; }
        public Nullable<decimal> cashPaid { get; set; }
        public Nullable<decimal> cardPaid { get; set; }
    
        public virtual Company Company { get; set; }
    }
}
