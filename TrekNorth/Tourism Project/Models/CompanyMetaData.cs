using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace Tourism_Project.Models
{
    [MetadataType(typeof(CompanyMetadata))]
    public partial class Company
    {
    }
    public class CompanyMetadata
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name;
        [Required(ErrorMessage = "Email is required.")]
        [DataType(DataType.EmailAddress)]
        public string Email;
        [Required(ErrorMessage = "Phone is required.")]
        public string Phone;
        [Range(0, 100, ErrorMessage = "Commission percentage must be between 0 and 100.")]
        public string Commission;
    }
}