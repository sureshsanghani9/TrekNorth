using System;
using System.Collections.Generic;

namespace Tourism_Project.Models
{
    public class AvailabilityModel
    {
        public int TourID { get; set; }
        public string TourName { get; set; }
        public DateTime Date { get; set; }
        public int Seat { get; set; }
        public List<DateTime> Dates { get; set; }
        public List<string> TourNames { get; set; }
        public List<string> Names { get; set; }

        public List<int> Seats { get; set; }

        public string Date1 { get; set; }

        /////////////////////////////////////////
        //NEW FIELDS//////
        public int TotalSeats { get; set; }
        public int OccupiedSeats { get; set; }
        public int RemainingSeats { get; set; }
      

        /////////////////////////////////////////
    }
}