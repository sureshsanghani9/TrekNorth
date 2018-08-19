using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Tourism_Project.Controllers;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tourism_Project.Models
{
    [MetadataType(typeof(VoucherMetadata))]
    public partial class Voucher
    {
        [NotMapped]
        public string p_CompanyName { get; set; }
        public List<Company> Companies;
        public string CompanyName
        {
            get
            {
                if (this.CompanyID != 0)
                {
                    CompanyRespository rep = new CompanyRespository();
                    Company c = rep.Get(x => x.CompanyID == this.CompanyID);
                    if (c != null)
                        return c.Name;
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
            set { }
        }
        public string FullName_Report;
        public string FullName
        {
            get { return this.FirstName + " " + this.LastName; }
            set { FullName_Report = value; }
        }

        public decimal BalanceToPay
        {
            get { return IsNull(this.Price) + IsNull(this.Levy) - IsNull(this.Commission); }
        }
        public decimal AgentCollects
        {
            get { return IsNull(this.Commission) - IsNull(this.Discount); }
        }

        private decimal IsNull(decimal? aDecimal)
        {
            if (aDecimal == null)
                return 0;
            else
            {
                return Convert.ToDecimal(aDecimal);
            }
        }

        public string TravelDateString
        {
            get { return this.TravelDate.Value.ToString("dd, MMMM, yyyy (dddd)"); }
        }


        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        [DataType(DataType.Date)]
        public DateTime? TravelDateFrom_Report { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        [DataType(DataType.Date)]
        public DateTime? TravelDateTo_Report { get; set; }

        //[DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        //[DataType(DataType.Date)]
        public DateTime? EnteredDateFrom_Report { get; set; }


        //[DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        //[DataType(DataType.Date)]
        public DateTime? EnteredDateTo_Report { get; set; }

        public string CompanyNumber { get; set; }

        [NotMapped]
        public Nullable<decimal> totalCreditPaid { get; set; }
        [NotMapped]
        public Nullable<decimal> totalCashPaid { get; set; }
        //  public Nullable<int> shopId { get; set; }
    }
    public class VoucherMetadata
    {


        [Required(ErrorMessage = "Payment Type is required.")]
        public string paymenttype { get; set; }
        // [Required(ErrorMessage = "Sales From is required.")]
        public string salesfrom { get; set; }
        [Required(ErrorMessage = "First Name is required.")]
        public string FirstName;
        [Required(ErrorMessage = "Price is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Price must be between a valid price over 1$.")]
        [DataType(DataType.Currency)]
        public string Price;
        [DataType(DataType.Currency)]
        public string Levy;
        [DataType(DataType.Currency)]
        public string Commission;
        [DataType(DataType.Currency)]
        public string Discount;

        //[Required(ErrorMessage = "Travel Date is required.")]
        //[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        //[DataType(DataType.Date)]
        //public DateTime? TravelDate { get; set; }
        public string TravelDate { get; set; }
        [DataType(DataType.Currency)]
        public decimal AgentCollects;
        [DataType(DataType.Currency)]
        public decimal BalanceToPay;

    }

    public class mixclass_voucher_booking
    {
        public IEnumerable<Voucher> vou { get; set; }
        public List<BookingModel> book { get; set; }
    }

    public class voucherStaffList
    {
        public string createdBy { get; set; }
        public Nullable<decimal> totalCardPaid { get; set; }
        public Nullable<decimal> totalCashPaid { get; set; }
        public Nullable<decimal> totalPrice { get; set; }
    }

    public partial class VoucherTemporary
    {
        public Nullable<int> AdultCount { get; set; }
        public Nullable<int> ChildrenCount { get; set; }
        public Nullable<int> InfantCount { get; set; }
        public Nullable<decimal> Price { get; set; }
        public Nullable<decimal> Levy { get; set; }
        public Nullable<decimal> Commission { get; set; }
        public Nullable<decimal> Discount { get; set; }
        public string Name { get; set; }
    }

    public class voucherGraph
    {
        public int Count { get; set; }
        public int Hour { get; set; }
    }
}