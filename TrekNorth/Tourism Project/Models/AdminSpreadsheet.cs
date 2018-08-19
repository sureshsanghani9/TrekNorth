using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tourism_Project.Models
{
    public class AdminSpreadsheet
    {
        public int id { get; set; }
        public string row { get; set; }
        public string cols { get; set; }
        [AllowHtml]
        public string data { get; set; }
        public string colors { get; set; }
    }

    public class AdminSaveFormat
    {
        public string row { get; set; }
        public string cols { get; set; }
        public string colors { get; set; }
    }
}