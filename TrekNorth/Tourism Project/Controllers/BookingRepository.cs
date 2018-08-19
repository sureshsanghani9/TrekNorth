using System.Collections.Generic;
using System.Globalization;
using System.Web.Configuration;
using MySql.Data.MySqlClient;
using Tourism_Project.Models;
using System;

namespace Tourism_Project.Controllers
{
    public class BookingRepository
    {
        private readonly MySqlConnection _connection;
        private MySqlCommand _cmd;
        private MySqlDataReader _dr;
        static readonly string ConnString = WebConfigurationManager.AppSettings["ServerDBConnection"].ToString(CultureInfo.InvariantCulture);

        public BookingRepository()
        {
            _connection = new MySqlConnection(ConnString);
            _cmd = new MySqlCommand();
            _dr = null;
        }

        public float GetCommission(int userId)
        {
            float commission = 0;
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select * from users where id='" + userId + "';";
            _dr = _cmd.ExecuteReader();

            while (_dr.Read())
            {
                commission = _dr.GetFloat(5);
            }
            _dr.Close();
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            return commission;
        }

        public List<RegisterModel> GetUsers()
        {
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select * from users where active = 1 OR active is null";
            _dr = _cmd.ExecuteReader();
            var models = new List<RegisterModel>();
            while (_dr.Read())
            {
                models.Add(new RegisterModel
                {
                    ID = _dr.GetInt32(0),
                    Name = _dr.GetString(1),
                    Address = _dr.GetString(2),
                    Phone = _dr.GetString(3),
                    Email = _dr.GetString(4),
                    Commission = _dr.GetFloat(5),
                    Credit = _dr.GetInt32(6),
                    UserName = _dr.GetString(7),
                    Password = _dr.GetString(8),
                    UserType = _dr.GetInt32(9),
                    PaymentType = _dr.GetInt32(10),
                    Comments = _dr.GetString(11),
                    Showvouchers = _dr.GetBoolean(13),
                    showaddbooking = _dr.GetBoolean(14)//new add 30-3 by yummi
                });
            }
            _dr.Close();
            return models;
        }

        public List<RegisterModel> GetUsers(int id)
        {
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select * from users where usertype = " + id + " AND active = 1 OR active is null";
            _dr = _cmd.ExecuteReader();
            var models = new List<RegisterModel>();
            while (_dr.Read())
            {
                models.Add(new RegisterModel
                {
                    ID = _dr.GetInt32(0),
                    Name = _dr.GetString(1),
                    Address = _dr.GetString(2),
                    Phone = _dr.GetString(3),
                    Email = _dr.GetString(4),
                    Commission = _dr.GetFloat(5),
                    Credit = _dr.GetInt32(6),
                    UserName = _dr.GetString(7),
                    Password = _dr.GetString(8),
                    UserType = _dr.GetInt32(9),
                    PaymentType = _dr.GetInt32(10),
                    Comments = _dr.GetString(11),
                    Showvouchers = _dr.GetBoolean(13),
                    showaddbooking = _dr.GetBoolean(14)//new add 30-3 by yummi
                });
            }
            _dr.Close();
            return models;
        }

        public RegisterModel GetUser(int id)
        {
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select * from users where id = " + id.ToString(CultureInfo.InvariantCulture);
            _dr = _cmd.ExecuteReader();
            var models = new RegisterModel();
            while (_dr.Read())
            {
                models.ID = _dr.GetInt32(0);
                models.Name = _dr.GetString(1);
                models.Address = _dr.GetString(2);
                models.Phone = _dr.GetString(3);
                models.Email = _dr.GetString(4);
                models.Commission = _dr.GetFloat(5);
                models.Credit = _dr.GetInt32(6);
                models.UserName = _dr.GetString(7);
                models.Password = _dr.GetString(8);
                models.UserType = _dr.GetInt32(9);
                models.PaymentType = _dr.GetInt32(10);
                models.Comments = _dr.GetString(11);

            }
            _dr.Close();
            _connection.Close();
            return models;
        }

        public List<RegisterModel> GetUserNamesAndComments()
        {
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select id,name from users where (usertype = 3) and active = 1 OR active is null";
            _dr = _cmd.ExecuteReader();
            var models = new List<RegisterModel>();
            while (_dr.Read())
            {
                models.Add(new RegisterModel
                {
                    ID = _dr.GetInt32(0),
                    Name = _dr.GetString(1)
                });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }

        public List<RegisterModel> GetFullPaymentAgents()
        {
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select * from users where (active = 1 OR active is null) and  paymenttype=2";
            _dr = _cmd.ExecuteReader();
            var models = new List<RegisterModel>();
            while (_dr.Read())
            {
                models.Add(new RegisterModel
                {
                    ID = _dr.GetInt32(0),
                    Name = _dr.GetString(1),
                    Address = _dr.GetString(2),
                    Phone = _dr.GetString(3),
                    Email = _dr.GetString(4),
                    Commission = _dr.GetFloat(5),
                    Credit = _dr.GetInt32(6),
                    UserName = _dr.GetString(7),
                    Password = _dr.GetString(8),
                    UserType = _dr.GetInt32(9),
                    PaymentType = _dr.GetInt32(10),
                    Comments = _dr.GetString(11),
                    Showvouchers = _dr.GetBoolean(13),
                    showaddbooking = _dr.GetBoolean(14)//new add 30-3 by yummi
                });
            }
            _dr.Close();
            return models;
        }

        public List<BookingModel> GetPickupLocation(int tourId)
        {
            var models = new List<BookingModel>();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            if (tourId != 0)
                _cmd.CommandText = "Select * from locations where tourid='" + tourId + "';";
            else
                _cmd.CommandText = "Select * from locations;";

            _dr = _cmd.ExecuteReader();
            while (_dr.Read())
            {
                models.Add(new BookingModel { pickuplocation = _dr.GetInt32(0), location = _dr.GetString(2) });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }

        public List<Location> GetTimes(int pickupId)
        {

            var models = new List<Location>();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            //_cmd.CommandText = "Select * from times where pickupid ='" + pickupId + "';";
            _cmd.CommandText = "Select * from times where locationid ='" + pickupId + "';";

            _dr = _cmd.ExecuteReader();

            while (_dr.Read())
            {
                models.Add(new Location { Tour = _dr.GetInt32(0), time = _dr.GetString(3) });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }

        public List<BookingModel> GetTourNames()
        {
            var models = new List<BookingModel>();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select * from tournames;";
            _dr = _cmd.ExecuteReader();

            while (_dr.Read())
            {
                models.Add(new BookingModel { Tour = _dr.GetInt32(0), tourname = _dr.GetString(1) });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }

        public List<BookingModel> GetTourCodes(int tourid)
        {
            var models = new List<BookingModel>();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select * from tourcode where tourid='" + tourid + "';";
            _dr = _cmd.ExecuteReader();

            while (_dr.Read())
            {
                models.Add(new BookingModel { TourCode = _dr.GetInt32(0), tourcodevalues = _dr.GetString(2) });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }


        public List<BookingModel> GetTourCodes()
        {
            var models = new List<BookingModel>();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select * from tourcode;";
            _dr = _cmd.ExecuteReader();

            while (_dr.Read())
            {
                models.Add(new BookingModel { TourCode = _dr.GetInt32(0), tourcodevalues = _dr.GetString(2) });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }

        //Adding Code on 2016-05-13
        public List<BookingModel> GetTourCodesData()
        {
            var models = new List<BookingModel>();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "SELECT t.id,t.tourid,t.tourcodevalues,tn.tourname FROM tourcode t INNER JOIN tournames tn ON t.tourid = tn.id where t.isDeleted=0";
            _dr = _cmd.ExecuteReader();

            while (_dr.Read())
            {
                models.Add(new BookingModel { TourCode = _dr.GetInt32(0), tourcodevalues = _dr.GetString(2), tourname = _dr.GetString(3) });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }


        //End Code
        public int GetCreditStatus(int id)
        {
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select credit from users where id=" + id.ToString();
            _dr = _cmd.ExecuteReader();
            var models = new List<RegisterModel>();
            int credit = -1;
            while (_dr.Read())
            {
                credit = _dr.GetInt32(0);
            }
            _dr.Close();
            _connection.Close();
            return credit;
        }

        public List<BookingModel> GetBookingsByDate(string year, string month, string date, int TourCodeID)
        {
            var models = new List<BookingModel>();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select b.*, tc.tourcodevalues, tn.tourname, t.time from bookings b LEFT JOIN tourcode tc on b.tourcode = tc.id LEFT JOIN tournames tn on b.tourid = tn.id LEFT JOIN times t on b.time = t.timeid where b.isDeleted = 0 and b.date='" + year + "-" + String.Format("{0:00}", Convert.ToInt32(month)) + "-" + String.Format("{0:00}", Convert.ToInt32(date)) + "' and b.tourid = " + TourCodeID.ToString() + ";";
            //_cmd.CommandText = "Select b.*, tc.tourcodevalues, tn.tourname, t.time from bookings b LEFT JOIN tourcode tc on b.tourcode = tc.id LEFT JOIN tournames tn on b.tourid = tn.id LEFT JOIN times t on b.time = t.timeid where b.isDeleted = 0 and b.date='" + year + "-" + String.Format("{0:00}", Convert.ToInt32(month)) + "-" + String.Format("{0:00}", Convert.ToInt32(date)) + "' and tc.tourcodevalues = " + tc.tourcodevalues.ToString() + ";";
            _dr = _cmd.ExecuteReader();
            while (_dr.Read())
            {
                models.Add(new BookingModel
                {
                    BookingID = _dr.GetInt32("id"),
                    Agent = _dr.GetString("agent"),
                    Voucher = _dr["voucher"] == DBNull.Value ? null : Convert.ToString(_dr["voucher"]),
                    Reference = _dr["reference"] == DBNull.Value ? null : Convert.ToString(_dr["reference"]),
                    Date = _dr.GetString("date"),
                    Tour = _dr.GetInt32("tourid"),
                    TourCode = _dr.GetInt32("tourcode"),
                    pickuplocation = _dr.GetInt32("pickuplocation"),
                    time = _dr.GetString("time"),
                    PassengerName = _dr.GetString("passenger"),
                    Adults = _dr.GetInt32("adults"),
                    FamilyChildren = _dr.GetInt32("familychildren"),
                    Infant = _dr.GetInt32("infant"),
                    Children = _dr.GetInt32("children"),
                    Price = _dr.GetInt32("price"),
                    Discount = _dr.GetInt32("discount"),
                    Commission = _dr.GetFloat("commission"),
                    TotalPrice = _dr.GetFloat("totalprice"),
                    ContactDetails = _dr.GetString("contact"),
                    Comments = _dr.GetString("comments"),
                    tourcodevalues = _dr.GetString("tourcodevalues"),
                    tourname = _dr.GetString("tourname")

                });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }
        //Changes made on 21-06-2016
        public List<BookingModel> GetBookingsByDateData(string year, string month, string date)
        {
            var models = new List<BookingModel>();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select b.*, tc.tourcodevalues, tn.tourname, t.time from bookings b LEFT JOIN tourcode tc on b.tourcode = tc.id LEFT JOIN tournames tn on b.tourid = tn.id LEFT JOIN times t on b.time = t.timeid where b.isDeleted = 0 and b.date='" + year + "-" + String.Format("{0:00}", Convert.ToInt32(month)) + "-" + String.Format("{0:00}", Convert.ToInt32(date)) + "';";
            //_cmd.CommandText = "Select b.*, tc.tourcodevalues, tn.tourname, t.time from bookings b LEFT JOIN tourcode tc on b.tourcode = tc.id LEFT JOIN tournames tn on b.tourid = tn.id LEFT JOIN times t on b.time = t.timeid where b.isDeleted = 0 and b.date='" + year + "-" + String.Format("{0:00}", Convert.ToInt32(month)) + "-" + String.Format("{0:00}", Convert.ToInt32(date)) + "' and tc.tourcodevalues = " + tc.tourcodevalues.ToString() + ";";
            _dr = _cmd.ExecuteReader();
            while (_dr.Read())
            {
                models.Add(new BookingModel
                {
                    BookingID = _dr.GetInt32("id"),
                    Agent = _dr.GetString("agent"),
                    Voucher = _dr["voucher"] == DBNull.Value ? null : Convert.ToString(_dr["voucher"]),
                    Reference = _dr["reference"] == DBNull.Value ? null : Convert.ToString(_dr["reference"]),
                    Date = _dr.GetString("date"),
                    Tour = _dr.GetInt32("tourid"),
                    TourCode = _dr.GetInt32("tourcode"),
                    pickuplocation = _dr.GetInt32("pickuplocation"),
                    time = _dr.GetString("time"),
                    PassengerName = _dr.GetString("passenger"),
                    Adults = _dr.GetInt32("adults"),
                    FamilyChildren = _dr.GetInt32("familychildren"),
                    Infant = _dr.GetInt32("infant"),
                    Children = _dr.GetInt32("children"),
                    Price = _dr.GetInt32("price"),
                    Discount = _dr.GetInt32("discount"),
                    Commission = _dr.GetFloat("commission"),
                    TotalPrice = _dr.GetFloat("totalprice"),
                    ContactDetails = _dr.GetString("contact"),
                    Comments = _dr.GetString("comments"),
                    tourcodevalues = _dr.GetString("tourcodevalues"),
                    tourname = _dr.GetString("tourname")

                });
            }
            _dr.Close();
            _connection.Close();
            return models;
        }

        private string ConvertDateForClient(string dbformat)
        {
            string month, day;
            DateTime date = Convert.ToDateTime(dbformat);
            month = date.Month < 10 ? "0" + date.Month : date.Month + "";
            day = date.Day < 10 ? "0" + date.Day : date.Day + "";

            return string.Format("{0}/{1}/{2}", day, month, date.Year);
        }

    }
}