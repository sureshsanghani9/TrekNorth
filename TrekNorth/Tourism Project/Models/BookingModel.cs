using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Tourism_Project.Models
{
    public class BookingModel
    {
        //uname on 2015-08-24
        //public string Username { get; set; }
        public int Children { get; set; }
        public int FamilyChildren { get; set; }
        public int Infant { get; set; }
        public int BookingID { get; set; }
        public string tourname { get; set; }
        public string tourcodevalues { get; set; }
        public string location { get; set; }
        public string name { get; set; }

        public int tid { get; set; }
        public int tc { get; set; }
        public int pl { get; set; }

        public int AgentId { get; set; }

        public bool isGoldClass { get; set; }
        public bool isDeleted { get; set; }
        public string PaymentMethod { get; set; }

        public float saleprice { get; set; }

        [Display(Name = "Agent")]
        public string Agent { get; set; }

        //[Required]
        
        [Display(Name = "Payment type")]
        public int PaymentType { get; set; }

        //[RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "The Voucher is alphanumeric only.")]
        [Display(Name = "Voucher")]
        public string Voucher { get; set; }

        //[Required]
        [Display(Name = "Reference")]
        public string Reference { get; set; }

        //[Required]
        [Display(Name = "Date")]
        public string Date { get; set; }

        //[Required]
        [Display(Name = "Tour")]
        public int Tour { get; set; }

        //[Required]
        [Display(Name = "Tour Code")]
        public int TourCode { get; set; }

        //[Required]
        [Display(Name = "Pick Up Location")]
        public int pickuplocation { get; set; }

        //[Required]
        //[Display(Name = "Pick Up Time")]
        public string time { get; set; }

        public int timeid { get; set; }

        //[Required]
        [Display(Name = "Passenger")]
        [RegularExpression(@"^[a-zA-Z0-9\s]*$", ErrorMessage = "The passenger name should contains letters only.")]
        public string PassengerName { get; set; }

        //[Required]
        [Display(Name = "Adults")]
        public int Adults { get; set; }

        //[Required]
        [Display(Name = "Tour Price")]
        [DisplayFormat(DataFormatString = "{0:0.00}")]
        public float Price { get; set; }

        [Display(Name = "Payable")]
        public float TotalPrice { get; set; }

        //[Required]
        [Display(Name = "Deposit")]
        public float Discount { get; set; }

        [Display(Name = "Deposit")]
        public float Commission { get; set; }

        [Display(Name = "Contact")]
        public string ContactDetails { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Display(Name = "Confirmation Number")]
        public string ConfirmationNumber { get; set; }

        [Display(Name = "CardPaid")]
        public float? CardPaid { get; set; }

        [Display(Name = "CashPaid")]
        public float? CashPaid { get; set; }


        public float? InvoiceAgent { get; set; }
        public float? POB { get;set; }


        public string voucherId { get; set; }
        // [Required]
        public int shopId { get; set; }
        //[Required(ErrorMessage = "The Customer Paymenttype field is required.")]
        public string custo_paymenttype { get; set; }

        // [Required]
        public string salesfrom { get; set; }

        public IEnumerable<lunch> AvailableLunch { get; set; }
        public IEnumerable<lunch> SelectedLunch { get; set; }
        public string Lunch { get; set; }
        public Postedlunchs Postedlunchs { get; set; } //commented on 14-09-2015
        public int Fish { get; set; }
        public int Steak { get; set; }
        public int Vegetarian { get; set; }
        public int totalTourist { get; set; }
        
        //public DateTime? dateReceived { get; set; }
        public string dateReceived { get; set; }
        public string staff { get; set; }

        [Display(Name = "Booking Person")]
        public string BookingPerson { get; set; }

        [Display(Name = "Booking Date")]
        public string BookingDate { get; set; }
        
    }
    /// <summary>
    /// model for lunch
    /// </summary>

    public class lunch
    {
        //Integer value of a checkbox
        public int Id { get; set; }

        //String name of a checkbox
        public string Name { get; set; }

        //Boolean value to select a checkbox
        //on the list
        public bool IsSelected { get; set; }

        //Object of html tags to be applied
        //to checkbox, e.g.:'new{tagName = "tagValue"}'
        public object Tags { get; set; }

    }
    /// <summary>
    /// for Helper class to make posting back selected values easier
    /// </summary>
    public class Postedlunchs
    {
        //this array will be used to POST values from the form to the controller
        public string[] LunchIds { get; set; }
    }

    public class Mshop
    {
        //[Required]
        //[Display(Name = "Date")]
        public int shopId { get; set; }

        [Display(Name = "Shop Name")]
        public string shopName { get; set; }

    }


    public class Seat
    {
        [Required]
        [Display(Name = "Date")]
        public string Date { get; set; }

        [Required]
        [Display(Name = "Available Seats")]
        public int available { get; set; }

        [Required]
        [Display(Name = "Tour")]
        public int Tour { get; set; }

        public string ToDate { get; set; }

        public DateTime ForSorting { get; set; }

        public string username { get; set; }
        public string email { get; set; }
    }

    public class Location
    {
        public int ID { get; set; }
        [Display(Name = "Pickup Location")]
        public string pickuplocation { get; set; }

        [Display(Name = "Tour")]
        public int Tour { get; set; }

        [Display(Name = "TourName")]
        public string TourName { get; set; }

        [Display(Name = "Pick Up Time")]
        public string time { get; set; }
    }

    public class LocationWiseTime
    {
        public int ID { get; set; }
        [Display(Name = "Pickup Location")]
        public string pickuplocation { get; set; }

        [Display(Name = "Tour")]
        public int Tour { get; set; }

        [Display(Name = "Pick Up Time")]
        public List<string> time { get; set; }
        //public string[] time { get; set; }
    }

    public class Time
    {
        public int timeId { get; set; }
        public string time { get; set; }
    }

}