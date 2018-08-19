using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Tourism_Project.Models;
using MySql.Data.MySqlClient;
using System.Web.Security;
using System.Globalization;
using RazorPDF;
using System.Web.Configuration;
using System.Data;
using System.Net.Mail;
using System.IO;
using System.Net.Mime;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using System.Text;
using System.Net;
using Tourism_Project.Helper;
using System.Runtime.Serialization.Formatters.Binary;
using System.Configuration;

namespace Tourism_Project.Controllers
{
    public enum USERTYPE
    {
        ADMIN = 1,
        STAFF = 2,
        AGENT = 3,
        DRIVER = 4,
        Satalite =5

    }

    public enum TOUR
    {
        CAPE_TRIBULATION = 1,
        KURANDA_8AM = 2,
        KURANDA_9AM = 3,
        KURANDA_10AM = 4,
        KURANDA_11AM = 5
    }

    public enum PAYMENT_TYPE
    {
        CASH_CARD = 1,
        AGENT_INVOICE = 2,
        DEPOSIT_TAKEN = 3
    }

    public enum CREDIT_STATUS
    {
        Deposit = 0,
        FULL_AMOUNT = 1
    }

    [Authorize]
    public class BookingController : Controller
    {
        #region Connection String
        static string connString = WebConfigurationManager.AppSettings["ServerDBConnection"].ToString();
        #endregion

        #region Properties
        private MySqlConnection connection;
        private MySqlCommand cmd;
        private MySqlDataReader dr;
        private int UserId;
        private int UserType;
        #endregion
        protected static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private MySqlCommand cmd1;
        private MySqlDataReader dr1;

        public BookingController()
        {
            connection = new MySqlConnection(connString);
            cmd = new MySqlCommand();
            dr = null;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AddBooking()
        {

            string[] user = User.Identity.Name.Split(','); //userid,username,usertype
            UserId = Int32.Parse(user[0]);
            UserType = Int32.Parse(user[2]);

            ViewBag.Status = "false";
            ViewBag.User = string.Empty;

            if (UserType == (int)USERTYPE.AGENT)
            {
                var repository = new BookingRepository();
                ViewBag.User = "agent";
                ViewBag.Commission = repository.GetCommission(UserId);

                return View(new BookingModel { Commission = ViewBag.Commission });
            }
            else
                return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBooking(BookingModel model)
        {
            string[] user = User.Identity.Name.Split(','); //userid,username,usertype
            UserId = Int32.Parse(user[0]);
            UserType = Int32.Parse(user[2]);

            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            bool check = false, isError = false;
            int commission = 0, creditStatus = -1, limit = 0;

            if (UserType == (int)USERTYPE.AGENT)
                ModelState.Remove("Voucher");

            ModelState.Remove("Discount");
            ModelState.Remove("ContactDetails");
            ModelState.Remove("Comments");
            ModelState.Remove("Children");
            ModelState.Remove("FamilyChildren");
            ModelState.Remove("Infant");

            if (ModelState.IsValid)
            {
                try
                {
                    var dateTime = DateTime.ParseExact(model.Date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
                    connection.Open();
                    cmd = connection.CreateCommand();
                    cmd.CommandText = "Select * from available_seats where date='" + dateTime + "' and tourid='" + model.Tour + "';";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        check = true;
                        limit = dr.GetInt32(3);
                    }
                    dr.Close();
                    if (!check)
                    {
                        if (model.Tour == (int)TOUR.CAPE_TRIBULATION)
                            limit = 16; // seats limit per day only for cape tribulation tour
                        else
                            limit = 21; // seats limit per day for kuranda tours 
                        cmd.CommandText = "INSERT INTO available_seats(tourid,date,available) VALUES(@tourid,@date,@available)";
                        cmd.Parameters.AddWithValue("@tourid", model.Tour);
                        cmd.Parameters.AddWithValue("@date", dateTime);
                        cmd.Parameters.AddWithValue("@available", limit);
                        cmd.ExecuteNonQuery();

                    }

                    if (limit < model.Adults + model.FamilyChildren + model.Children + model.Infant || limit == 0)
                    {
                        ModelState.AddModelError("", "Seats are not enough for this booking.");
                        ViewBag.Status = "seatserror";
                        ViewBag.Commission = model.Commission;
                        if (UserType == (int)USERTYPE.AGENT)
                        {
                            ViewBag.User = "agent";
                        }
                        else
                        {
                            ViewBag.User = string.Empty;
                        }
                        return View();
                    }
                    else
                    {
                        if (UserType == (int)USERTYPE.ADMIN || UserType == (int)USERTYPE.STAFF)
                        {
                            cmd.CommandText = "Select * from users where id='" + model.AgentId + "';";
                        }
                        else
                            cmd.CommandText = "Select * from users where id='" + UserId + "';";

                        dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            commission = dr.GetInt32(5);
                            creditStatus = dr.GetInt32(6);
                            if (UserType == (int)USERTYPE.AGENT)
                                model.PaymentType = dr.GetInt32(10);
                        }
                        dr.Close();

                        cmd = connection.CreateCommand();
                        cmd.CommandText = "INSERT INTO bookings(uid,bookinguserid,agent,voucher,reference,date,tourid,tourcode,pickuplocation,time,passenger,adults,familychildren,children,infant,price,discount,commission,totalprice,contact,comments,paymenttype,agentid,confirmationnumber) VALUES(@uid,@bookinguserid,@agent,@voucher,@reference,@date,@tourid,@tourcode,@pickuplocation,@time,@passenger,@adults,@familychildren,@children,@infant,@price,@discount,@commission,@totalprice,@contact,@comments,@paymenttype,@agentid,@confirmationnumber)";

                        cmd.Parameters.AddWithValue("@uid", UserId.ToString());
                        cmd.Parameters.AddWithValue("@bookinguserid", UserId.ToString());

                        if ((int)USERTYPE.ADMIN == UserType || (int)USERTYPE.STAFF == UserType)
                        {
                            cmd.Parameters.AddWithValue("@agent", model.Agent.Trim());
                            cmd.Parameters.AddWithValue("@voucher", model.Voucher);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@agent", string.Empty);
                            cmd.Parameters.AddWithValue("@voucher", string.Empty);
                        }
                        cmd.Parameters.AddWithValue("@reference", Convert.ToString(model.Reference));
                        cmd.Parameters.AddWithValue("@date", dateTime);
                        cmd.Parameters.AddWithValue("@tourid", model.Tour);
                        cmd.Parameters.AddWithValue("@tourcode", model.TourCode);
                        cmd.Parameters.AddWithValue("@pickuplocation", model.pickuplocation);
                        cmd.Parameters.AddWithValue("@time", model.time);
                        cmd.Parameters.AddWithValue("@passenger", model.PassengerName);
                        cmd.Parameters.AddWithValue("@adults", model.Adults);
                        cmd.Parameters.AddWithValue("@familychildren", model.FamilyChildren);
                        cmd.Parameters.AddWithValue("@children", model.Children);
                        cmd.Parameters.AddWithValue("@infant", model.Infant);
                        cmd.Parameters.AddWithValue("@price", model.Price);
                        cmd.Parameters.AddWithValue("@discount", model.Discount);
                        cmd.Parameters.AddWithValue("@confirmationnumber", model.ConfirmationNumber);

                        if (UserType == (int)USERTYPE.ADMIN || UserType == (int)USERTYPE.STAFF)
                            cmd.Parameters.AddWithValue("@commission", model.Commission);
                        else
                            cmd.Parameters.AddWithValue("@commission", commission);

                        float price = model.Price;
                        float discount = model.Discount;
                        float comm = 0;
                        if (UserType == (int)USERTYPE.ADMIN || UserType == (int)USERTYPE.STAFF)
                            comm = model.Commission;
                        else
                            comm = commission;

                        float temp = price * (discount / 100);
                        float tempPrice = price - temp;
                        temp = tempPrice * (comm / 100);
                        float totalPrice = tempPrice - temp;

                        float CommCal = 0;
                        if (model.Commission != 0)
                            CommCal = model.Commission;
                        else
                            CommCal = commission;

                        if (model.PaymentType == (int)PAYMENT_TYPE.AGENT_INVOICE && creditStatus == (int)CREDIT_STATUS.FULL_AMOUNT)
                            cmd.Parameters.AddWithValue("@totalprice", model.Price - (model.Price * CommCal) / 100);
                        else
                            cmd.Parameters.AddWithValue("@totalprice", totalPrice);

                        if (string.IsNullOrEmpty(model.ContactDetails))
                            cmd.Parameters.AddWithValue("@contact", string.Empty);
                        else
                            cmd.Parameters.AddWithValue("@contact", model.ContactDetails);

                        if (string.IsNullOrEmpty(model.Comments))
                            cmd.Parameters.AddWithValue("@comments", string.Empty);
                        else
                            cmd.Parameters.AddWithValue("@comments", model.Comments);

                        cmd.Parameters.AddWithValue("@paymenttype", model.PaymentType);

                        if ((int)USERTYPE.ADMIN == UserType || (int)USERTYPE.STAFF == UserType)
                        {
                            cmd.Parameters.AddWithValue("@agentid", model.AgentId);
                        }
                        else
                            cmd.Parameters.AddWithValue("@agentid", UserId);

                        cmd.ExecuteNonQuery();

                        limit = limit - (model.Adults + model.FamilyChildren + model.Children + model.Infant);  // on 2015-06-27

                        //cmd.CommandText = "UPDATE available_seats SET available = '" + limit + "' where date='" + dateTime + "' and tourid='" + model.Tour + "';";
                        //cmd.ExecuteNonQuery();

                        if (model.PaymentType == (int)PAYMENT_TYPE.AGENT_INVOICE)//&& creditStatus == (int)CREDIT_STATUS.FULL_AMOUNT  change by kishor on 2015-06-25.. If someone overwrite the value from booking then we not need to check credit 
                        {
                            string tourcode = this.CheckTourCode(model.TourCode);
                            float commissionCalculation;
                            cmd = connection.CreateCommand();
                            cmd.CommandText = "INSERT INTO agents_bookings(date,agent_id,tour_money,passengername,adult,children,familychildren,infant,voucher,tourcode) VALUES(@date,@agent_id,@tour_money,@passengername,@adult,@children,@familychildren,@infant,@voucher,@tourcode)";
                            cmd.Parameters.AddWithValue("@date", dateTime);
                            cmd.Parameters.AddWithValue("@agent_id", model.AgentId);
                            if (model.Commission != 0)
                                commissionCalculation = model.Commission;
                            else
                                commissionCalculation = commission;

                            cmd.Parameters.AddWithValue("@tour_money", model.Price - (model.Price * commissionCalculation) / 100);
                            cmd.Parameters.AddWithValue("@passengername", model.PassengerName);
                            cmd.Parameters.AddWithValue("@adult", model.Adults);
                            cmd.Parameters.AddWithValue("@children", model.Children);
                            cmd.Parameters.AddWithValue("@familychildren", model.FamilyChildren);
                            cmd.Parameters.AddWithValue("@infant", model.Infant);
                            if (UserType == (int)USERTYPE.AGENT)
                                cmd.Parameters.AddWithValue("@voucher", string.Empty);
                            else
                                cmd.Parameters.AddWithValue("@voucher", model.Voucher);

                            cmd.Parameters.AddWithValue("@tourcode", tourcode);
                            cmd.ExecuteNonQuery();
                        }
                    }

                }
                catch (Exception e)
                {
                    isError = true;
                    ViewBag.Error = e.InnerException + " " + e.Message;
                    log.Error(e.Message, e);
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
                if (!isError)
                {
                    ViewBag.Status = "true";
                    connection.Open();
                    cmd.CommandText = "SELECT max(id) from bookings where isDeleted = 0";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        ViewBag.BookingId = dr.GetInt32(0);
                    }
                    dr.Close();
                    connection.Close();
                }
                else
                    ViewBag.Status = "error";
            }
            else
                ViewBag.Status = "false";

            if (UserType == (int)USERTYPE.AGENT)
            {
                @ViewBag.User = "agent";
                @ViewBag.Commission = commission;
            }
            else
            {
                @ViewBag.User = string.Empty;
            }

            return View();
        }

        public ActionResult EditBooking(int bookingid)
        {
            bool isError = false;
            BookingModel model = null;
            DataTable dt = new DataTable();
            try
            {
                string[] user = User.Identity.Name.Split(','); //userid,username,usertype
                ViewBag.UserId = Int32.Parse(user[0]);
                ViewBag.UserType = Int32.Parse(user[2]);
                ViewBag.User = string.Empty;
                ViewBag.Username = user[1].ToString();
                ViewBag.BookingID = bookingid;
                ViewBag.DisableTourPriceForStaff = false;
                if (ViewBag.UserType == (int)USERTYPE.STAFF)
                {
                    ViewBag.DisableTourPriceForStaff = true;
                }
                DateTime BookingDate = DateTime.Now;
                
                connection.Open();
                cmd = connection.CreateCommand();

                #region shopname in dropdown commented by yummy
                //cmd.CommandText = "Select * from shop;";
                //MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
                //DataSet ds = new DataSet();
                //adp.Fill(ds);
                //List<SelectListItem> selectListItems = new List<SelectListItem>();
                //SelectListItem item = new SelectListItem();

                //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                //{
                //    item = new SelectListItem();
                //    item.Value = ds.Tables[0].Rows[i]["shopId"].ToString();
                //    item.Text = ds.Tables[0].Rows[i]["shopName"].ToString();
                //    selectListItems.Add(item);
                //}
                //TempData["Mshops"] = selectListItems;
                //TempData.Keep();
                #endregion

                cmd.CommandText = "Select b.*, name from bookings b,users u where b.bookinguserid=u.id and isDeleted = 0 and b.id='" + bookingid + "';";

                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var repository = new BookingRepository();
                    ViewBag.Users = PrepareUsersDropDown(repository.GetUserNamesAndComments());
                    ViewBag.Tours = PrepareToursDropDown(repository.GetTourNames());
                    ViewBag.TourCodes = PrepareTourCodesDropDown(repository.GetTourCodes(dr.GetInt32("tourid")));
                    ViewBag.PickupTimes = PrepareTimesDropDown(repository.GetTimes(dr.GetInt32("pickuplocation")));
                    
                    /*commented on 14-09-2015
                    string[] lunchitems = dr["lunch"].ToString().Split('~');
                    var selectedlunch = new List<lunch>();

                    selectedlunch = GetAll()
                     .Where(x => lunchitems.Any(s => x.Name.ToString().Equals(s)))
                     .ToList();
                    */

                    BookingDate = Convert.ToDateTime((dr["date"] == DBNull.Value ? string.Empty : dr.GetString("date")));

                    if (BookingDate < DateTime.Now && Int32.Parse(user[2]) == (int)USERTYPE.STAFF)
                    {
                        ViewBag.DisablePaymentDetails = true;
                    }
                    else
                    {
                        ViewBag.DisablePaymentDetails = false;
                    }
                    if (BookingDate < DateTime.Now)
                    {
                        ViewBag.DisablePaymentFields = true;
                    }
                    else
                    {
                        ViewBag.DisablePaymentFields = false;
                    }

                    model = new BookingModel
                    {
                        BookingID = dr.GetInt32("id"),
                        Agent = (dr["agent"] == DBNull.Value ? string.Empty : dr.GetString("agent")),
                        AgentId = dr.GetInt32("AgentId"),
                        Voucher = (dr["voucher"] == DBNull.Value ? string.Empty : dr.GetString("voucher")),
                        Reference = (dr["reference"] == DBNull.Value ? string.Empty : dr.GetString("reference")),
                        Date = this.ConvertDateForClient((dr["date"] == DBNull.Value ? string.Empty : dr.GetString("date"))),
                        Tour = dr.GetInt32("tourid"),
                        tid = dr.GetInt32("tourid"),
                        TourCode = dr.GetInt32("tourcode"),
                        tc = dr.GetInt32("tourcode"),
                        pickuplocation = dr.GetInt32("pickuplocation"),
                        pl = dr.GetInt32("pickuplocation"),
                        time = (dr["time"] == DBNull.Value ? string.Empty : dr.GetString("time")),
                        PassengerName = (dr["passenger"] == DBNull.Value ? string.Empty : dr.GetString("passenger")),
                        Adults = dr.GetInt32("adults"),
                        FamilyChildren = dr.GetInt32("familychildren"),
                        Infant = dr.GetInt32("infant"),
                        Children = dr.GetInt32("children"),
                        Price = dr.GetFloat("price"),
                        Discount = dr.GetFloat("discount"),
                        Commission = dr.GetFloat("commission"),
                        TotalPrice = dr.GetFloat("totalprice"),
                        ContactDetails = (dr["contact"] == DBNull.Value ? string.Empty : dr.GetString("contact")),
                        Comments = (dr["comments"] == DBNull.Value ? string.Empty : dr.GetString("comments")),
                        PaymentType = dr.GetInt32("paymenttype"),
                        isGoldClass = dr.GetBoolean("isGoldClass"),
                        PaymentMethod = (dr["PaymentMethod"] == DBNull.Value ? string.Empty : dr.GetString("PaymentMethod")),

                        CashPaid = (dr["CashPaid"] == DBNull.Value ? 0 : dr.GetFloat("CashPaid")),
                        CardPaid = (dr["CardPaid"] == DBNull.Value ? 0 : dr.GetFloat("CardPaid")),

                        InvoiceAgent = (dr["agentInvoice"] == DBNull.Value ? 0 : dr.GetFloat("agentInvoice")),
                        POB = (dr["POB"] == DBNull.Value ? 0 : dr.GetFloat("POB")),

                        //newly added filed by yummy on 13-4-15  
                        //  (Convert.ToInt32(voucheridmax) + 1).ToString("D5");
                        voucherId = (dr["voucherId"] == DBNull.Value ? string.Empty : (Convert.ToInt32(dr.GetString("voucherId")).ToString("D5"))),
                        //shopId = dr.GetInt32("shopId"),
                        //salesfrom = dr.GetString("salesfrom"),
                        custo_paymenttype = (dr["custo_paymenttype"] == DBNull.Value ? string.Empty : dr.GetString("custo_paymenttype")),
                        ConfirmationNumber = (dr["confirmationnumber"] == DBNull.Value ? string.Empty : dr.GetString("confirmationnumber")), //(dr.GetString("confirmationnumber"))
                        // saleprice=(dr.GetFloat("saleprice")),
                        saleprice = (dr["saleprice"] == DBNull.Value ? 0 : dr.GetFloat("saleprice")),

                        AvailableLunch = GetAll().ToList(),
                        //SelectedLunch = selectedlunch, //on 14-09-2015
                        Fish = (dr["Fish"] == DBNull.Value ? 0 : dr.GetInt32("Fish")),
                        Steak = (dr["Steak"] == DBNull.Value ? 0 : dr.GetInt32("Steak")),
                        Vegetarian = (dr["Vegetarian"] == DBNull.Value ? 0 : dr.GetInt32("Vegetarian")),

                        // dateReceived =(dr["dateReceived"] == DBNull.Value) ? null : (DateTime)dr["dateReceived"],
                        dateReceived = Convert.ToString(dr["dateReceived"]), //(dr["dateReceived"] == DBNull.Value) ? (DateTime?)null : Convert.ToDateTime(dr["dateReceived"]),

                        staff = Convert.ToString(dr["staff"]),
                        totalTourist = Convert.ToInt32(dr.GetInt32("adults")) + Convert.ToInt32(dr.GetInt32("children")) + Convert.ToInt32(dr.GetInt32("familychildren")) + Convert.ToInt32(dr.GetInt32("Infant")),
                        BookingPerson = Convert.ToString(dr["name"]),
                        BookingDate = this.ConvertDateForClient((dr["createdDate"] == DBNull.Value ? string.Empty : dr.GetString("createdDate")))
                    
                    };
                }
                dr.Close();

            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
                ViewBag.errora += "e.Message: " + e.Message + "e " + e;
                TempData["modeldata"] = dr;
                TempData.Keep();
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "ok";
            else
                ViewBag.Status = "error";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult EditBooking(BookingModel model, Postedlunchs postedlunchs)
        public ActionResult EditBooking(BookingModel model)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };

            string[] user = User.Identity.Name.Split(','); //userid,username,usertype
            UserId = Int32.Parse(user[0]);
            UserType = Int32.Parse(user[2]);

            var repository = new BookingRepository();
            ViewBag.Tours = PrepareToursDropDown(repository.GetTourNames());
            ViewBag.TourCodes = PrepareTourCodesDropDown(repository.GetTourCodes(1));
            ViewBag.Users = PrepareUsersDropDown(repository.GetUserNamesAndComments());
            ViewBag.PickupLocations = PreparePickupLocationDropDown(repository.GetPickupLocation(1));
            ViewBag.PickupTimes = PrepareTimesDropDown(repository.GetTimes(31));
            ViewBag.UserId = Int32.Parse(user[0]);
            ViewBag.UserType = Int32.Parse(user[2]);
            ViewBag.User = string.Empty;
            ViewBag.Username = user[1].ToString();

            if (model.Tour != 1)
            {
                model.Fish = 0;
                model.Steak = 0;
                model.Vegetarian = 0;
            }

            /*commented on 14-09-2015
            if (model.Tour != 1)
            {
                postedlunchs = null;
            }

            //yummy for check box
            // setup properties

            var selectedlunch = new List<lunch>();
            var postedLunchIds = new string[0];
            if (postedlunchs == null) postedlunchs = new Postedlunchs();

            // if a view model array of posted fruits ids exists
            // and is not empty,save selected ids
            if (postedlunchs.LunchIds != null && postedlunchs.LunchIds.Any())
            {
                postedLunchIds = postedlunchs.LunchIds;
            }

            // if there are any selected ids saved, create a list of fruits
            if (postedLunchIds.Any())
            {
                selectedlunch = GetAll()
                 .Where(x => postedLunchIds.Any(s => x.Id.ToString().Equals(s)))
                 .ToList();
            }
            string lunchitems = null;
            foreach (var item in selectedlunch)
            {
                lunchitems += item.Name.ToString() + "~";
            }
            */
            //  end 14-09-2015

            //end yummy for checkbox

            string agentName = string.Empty;
            ModelState.Remove("Discount");
            ModelState.Remove("ContactDetails");
            ModelState.Remove("Comments");
            ModelState.Remove("Children");
            ModelState.Remove("FamilyChildren");
            ModelState.Remove("Infant");

            foreach (ModelState modelState in ViewData.ModelState.Values)
            {
                foreach (ModelError error in modelState.Errors)
                {
                    string str = error.ErrorMessage;
                }
            }


            if (ModelState.IsValid)
            {
                bool isError = false;
                int limit = 0;
                try
                {
                    connection.Open();
                    cmd = connection.CreateCommand();

                    
                    var dateTime = DateTime.ParseExact(model.Date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");

                    cmd.CommandText = "Select * from available_seats where date='" + dateTime + "' and tourid='" + model.Tour + "';";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        limit = dr.GetInt32(3);
                    }
                    dr.Close();
                    cmd.CommandText = "Select * from bookings where isDeleted = 0 and id='" + model.BookingID + "';";
                    dr = cmd.ExecuteReader();
                    int previousSeats = 0;
                    while (dr.Read())
                    {
                        previousSeats += dr.GetInt32(11);
                        previousSeats += dr.GetInt32(12);
                        previousSeats += dr.GetInt32(13);
                        previousSeats += dr.GetInt32(14);
                        break;
                    }
                    dr.Close();

                    //on 2015-06-30
                    var repositorysData = new BookingRepository();
                    var AgentUserData = (repositorysData.GetUser(model.AgentId));
                    if (AgentUserData != null)
                    {
                        agentName = AgentUserData.Name;
                    }
                    //end


                    //if (limit < model.Adults + model.FamilyChildren + model.Children + model.Infant || limit == 0)
                    if ((previousSeats + limit) < model.Adults + model.FamilyChildren + model.Children + model.Infant)
                    {
                        ModelState.AddModelError("", "Seats are not enough for this booking.");
                        ViewBag.Status = "error";

                        //on 2015-06-27
                        //repository = new BookingRepository();
                        //ViewBag.Tours = PrepareToursDropDown(repository.GetTourNames());
                        //ViewBag.TourCodes = PrepareTourCodesDropDown(repository.GetTourCodes(1));
                        //ViewBag.Users = PrepareUsersDropDown(repository.GetUserNamesAndComments());
                        //ViewBag.PickupLocations = PreparePickupLocationDropDown(repository.GetPickupLocation(1));
                        //ViewBag.PickupTimes = PrepareTimesDropDown(repository.GetTimes(31));
                        //ViewBag.UserId = Int32.Parse(user[0]);
                        //ViewBag.UserType = Int32.Parse(user[2]);
                        //ViewBag.User = string.Empty;
                        //ViewBag.Username = user[1].ToString();

                        var selectedFruits = new List<lunch>();
                        model.AvailableLunch = GetAll().ToList();
                        model.SelectedLunch = selectedFruits;

                        return View(model);

                    }
                    else
                    {
                        if (model.custo_paymenttype != "Reservation")
                        {
                            model.PaymentMethod = string.Empty;
                        }
                        cmd = connection.CreateCommand();

                        // DateTime? dateReceived =  model.dateReceived = true ? (DateTime?)null : new DateTime(0);

                        // cmd.CommandText = "UPDATE bookings SET agent = '" + agentName + "',voucher='" + model.Voucher + "', reference='" + model.Reference + "', date='" + dateTime + "', tourid='" + model.Tour + "', tourcode='" + model.TourCode + "', pickuplocation='" + model.pickuplocation + "', time='" + model.time + "', passenger='" + model.PassengerName + "', adults='" + model.Adults + "', familychildren='" + model.FamilyChildren + "', children='" + model.Children + "', infant='" + model.Infant + "', price='" + model.Price + "', discount='" + model.Discount + "', commission='" + model.Commission + "', totalprice='" + model.TotalPrice + "', contact='" + model.ContactDetails + "', comments='" + model.Comments + "', agentid=  '" + model.AgentId + "', custo_paymenttype=  '" + model.custo_paymenttype + "', isGoldClass =" + model.isGoldClass + ", ConfirmationNumber ='" + model.ConfirmationNumber + "', PaymentMethod ='" + model.PaymentMethod + "',paymenttype =" + model.PaymentType + ",saleprice=" + model.saleprice + ",Fish=" + model.Fish + ",Steak=" + model.Steak + ",Vegetarian=" + model.Vegetarian + ",cardPaid='" + Convert.ToDecimal(model.CardPaid) + "',cashPaid='" + Convert.ToDecimal(model.CashPaid) + "',agentInvoice='" + Convert.ToDecimal(model.InvoiceAgent) + "',POB='" + Convert.ToDecimal(model.POB) + "',dateReceived='" + Convert.ToDateTime(model.dateReceived) + "',staff='" + model.staff +  "',uid='" + TempData["UserId"] + "' where isDeleted = 0 and id='" + model.BookingID + "';";

                        if (model.dateReceived != null)
                        {
                            //var dtReceived = DateTime.ParseExact(model.dateReceived.ToString(), formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
                            cmd.CommandText = "UPDATE bookings SET agent = '" + agentName + "',voucher='" + model.Voucher + "', reference='" + model.Reference + "', date='" + dateTime + "', tourid='" + model.Tour + "', tourcode='" + model.TourCode + "', pickuplocation='" + model.pickuplocation + "', time='" + model.time + "', passenger='" + model.PassengerName + "', adults='" + model.Adults + "', familychildren='" + model.FamilyChildren + "', children='" + model.Children + "', infant='" + model.Infant + "', price='" + model.Price + "', discount='" + model.Discount + "', commission='" + model.Commission + "', totalprice='" + model.TotalPrice + "', contact='" + model.ContactDetails + "', comments='" + model.Comments + "', agentid=  '" + model.AgentId + "', custo_paymenttype=  '" + model.custo_paymenttype + "', isGoldClass =" + model.isGoldClass + ", ConfirmationNumber ='" + model.ConfirmationNumber + "',paymenttype =" + model.PaymentType + ",saleprice=" + model.saleprice + ",Fish=" + model.Fish + ",Steak=" + model.Steak + ",Vegetarian=" + model.Vegetarian + ",cardPaid='" + Convert.ToDecimal(model.CardPaid) + "',cashPaid='" + Convert.ToDecimal(model.CashPaid) + "',agentInvoice='" + Convert.ToDecimal(model.InvoiceAgent) + "',POB='" + Convert.ToDecimal(model.POB) + "',dateReceived='" + model.dateReceived + "',staff='" + model.staff + "',uid='" + TempData["UserId"] + "' where isDeleted = 0 and id='" + model.BookingID + "';";
                        }
                        else
                        {
                            cmd.CommandText = "UPDATE bookings SET agent = '" + agentName + "',voucher='" + model.Voucher + "', reference='" + model.Reference + "', date='" + dateTime + "', tourid='" + model.Tour + "', tourcode='" + model.TourCode + "', pickuplocation='" + model.pickuplocation + "', time='" + model.time + "', passenger='" + model.PassengerName + "', adults='" + model.Adults + "', familychildren='" + model.FamilyChildren + "', children='" + model.Children + "', infant='" + model.Infant + "', price='" + model.Price + "', discount='" + model.Discount + "', commission='" + model.Commission + "', totalprice='" + model.TotalPrice + "', contact='" + model.ContactDetails + "', comments='" + model.Comments + "', agentid=  '" + model.AgentId + "', custo_paymenttype=  '" + model.custo_paymenttype + "', isGoldClass =" + model.isGoldClass + ", ConfirmationNumber ='" + model.ConfirmationNumber + "',paymenttype =" + model.PaymentType + ",saleprice=" + model.saleprice + ",Fish=" + model.Fish + ",Steak=" + model.Steak + ",Vegetarian=" + model.Vegetarian + ",cardPaid='" + Convert.ToDecimal(model.CardPaid) + "',cashPaid='" + Convert.ToDecimal(model.CashPaid) + "',agentInvoice='" + Convert.ToDecimal(model.InvoiceAgent) + "',POB='" + Convert.ToDecimal(model.POB) + "',dateReceived='',staff='" + model.staff + "',uid='" + TempData["UserId"] + "' where isDeleted = 0 and id='" + model.BookingID + "';";
                        }
                        cmd.ExecuteNonQuery();

                        limit = (previousSeats + limit) - (model.Adults + model.FamilyChildren + model.Children + model.Infant);  //on 2015-06-27
                        if (limit < 0) limit = 0;

                        cmd.CommandText = "UPDATE available_seats SET available = '" + limit + "' where date='" + dateTime + "' and tourid='" + model.Tour + "';";
                        cmd.ExecuteNonQuery();

                        //on 2015-06-26   Agent_Booking
                        cmd = connection.CreateCommand();
                        cmd.CommandText = "select * from agents_bookings where voucherId=@voucherId";
                        cmd.Parameters.AddWithValue("@voucherId", Convert.ToInt32(model.voucherId));

                        dr = cmd.ExecuteReader();
                        int agentvoucherid = 0;
                        while (dr.Read())
                        {
                            agentvoucherid = dr.GetInt32("voucherid");
                        }
                        dr.Close();

                        // if (model.PaymentType == (int)PAYMENT_TYPE.AGENT_INVOICE) //Commented on 12-09-2016
                        {
                            cmd = connection.CreateCommand();
                            cmd.Parameters.AddWithValue("@Date", dateTime);
                            cmd.Parameters.AddWithValue("@agentid", model.AgentId);
                            cmd.Parameters.AddWithValue("@tourmoney", model.TotalPrice);
                            cmd.Parameters.AddWithValue("@tourCode", model.TourCode);
                            cmd.Parameters.AddWithValue("@passengername", model.PassengerName);
                            cmd.Parameters.AddWithValue("@voucher", model.Voucher);
                            cmd.Parameters.AddWithValue("@voucherId", Convert.ToInt32(model.voucherId));
                            cmd.Parameters.AddWithValue("@Adult", model.Adults);
                            cmd.Parameters.AddWithValue("@Children", model.Children);
                            cmd.Parameters.AddWithValue("@familychildren", model.FamilyChildren);
                            cmd.Parameters.AddWithValue("@infant", model.Infant);

                            if (agentvoucherid != 0)
                            {
                                //update 
                                cmd.CommandText = "Update agents_bookings set Date=@Date, agent_id=@agentid, tour_money=@tourmoney, tourCode=@tourCode,passengername=@passengername,voucher=@voucher,Adult=@Adult,Children=@Children,familychildren=@familychildren,infant=@infant where voucherid=@voucherid";
                                cmd.ExecuteNonQuery();
                            }
                            else
                            {
                                //insert
                                cmd.CommandText = @"Insert agents_bookings(Date,agent_id,tour_money,tourcode,passengername,voucher,voucherid,adult,children,familychildren,infant) 
                                                   values(@Date,@agentid,@tourmoney,@tourcode,@passengername,@voucher,@voucherid,@adult,@children,@familychildren,@infant)";
                                cmd.ExecuteNonQuery();
                            }
                        }
                        //else    //Commented on 12-09-2016
                        //{
                        //    if (agentvoucherid != 0)
                        //    {
                        //        //delete
                        //        cmd = connection.CreateCommand();
                        //        cmd.CommandText = "Delete from agents_bookings where voucherid=@voucherid";
                        //        cmd.Parameters.AddWithValue("@voucherId", Convert.ToInt32(model.voucherId));
                        //        cmd.ExecuteNonQuery();
                        //    }
                        //}
                        //end
                    }

                }
                catch (Exception e)
                {
                    isError = true;
                    log.Error(e.Message, e);
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
                if (!isError)
                    ViewBag.Status = "true";
                else
                    ViewBag.Status = "error";
            }
            else
            {
                ViewBag.Status = "false";
                var errors = ModelState.Values.SelectMany(v => v.Errors);
            }

            if (ViewBag.Status == "true")
                return RedirectToAction("ViewAllBookings");
            else
                return View(model);
        }


        public ActionResult ViewAgents()
        {
            bool isError = false;
            var models = new List<RegisterModel>();
            try
            {
                var repository = new BookingRepository();
                models = repository.GetUsers();
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            ViewBag.Status = !isError ? "true" : "error";

            return View(models);
        }
        //Changes Made on 2016-03-08
        public ActionResult ViewAdmins()
        {
            bool isError = false;
            var models = new List<RegisterModel>();
            try
            {
                var repository = new BookingRepository();
                models = repository.GetUsers(1);
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            ViewBag.Status = !isError ? "true" : "error";
            return View(models);
        }

        //End Changes
        public ActionResult ViewStaff()
        {
            bool isError = false;
            var models = new List<RegisterModel>();
            try
            {
                var repository = new BookingRepository();
                models = repository.GetUsers(2);
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            ViewBag.Status = !isError ? "true" : "error";

            return View(models);
        }

        public JsonResult getAgents(string name)
        {
            List<RegisterModel> models = new List<RegisterModel>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from users where name like '%" + name + "%' and (active=1 OR active=null)";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    models.Add(new RegisterModel
                    {
                        ID = dr.GetInt32(0),
                        Name = dr.GetString(1),
                        Commission = dr.GetFloat(5),
                        PaymentType = dr.GetInt32(10),
                        Comments = dr.GetString(11) ?? ""
                    });
                }
                dr.Close();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            return Json(models.OrderBy(x => x.Name));
        }

        public ActionResult ViewAgentsReports()
        {
            ViewBag.Status = "true";
            return View();
        }

        public ActionResult ViewFullPaymentAgents()
        {
            bool isError = false;
            var models = new List<RegisterModel>();
            try
            {
                var repository = new BookingRepository();
                models = repository.GetFullPaymentAgents();
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            ViewBag.Status = !isError ? "true" : "error";

            return View(models);
        }


        #region commented on 09-03-2016
        /*  Commented on 09-03-2013
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ViewAgentsReports(RegisterModel model)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var From = DateTime.ParseExact(model.FromDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            var To = DateTime.ParseExact(model.ToDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            RegisterModel agent = null;
            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                DataTable dt_agents_bookings = new DataTable();
                //comment on 04-07-2016
                //cmd.CommandText = "Select * from agents_bookings,users,bookings where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and bookings.voucherid=agents_bookings.voucherid and bookings.isdeleted = 0 and agents_bookings.voucherid != 0;";
                cmd.CommandText = "Select * from agents_bookings,users,bookings where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and bookings.voucherid=agents_bookings.voucherid and bookings.isdeleted =0;";
                // cmd.CommandText = "Select * from agents_bookings,users where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id";  
                //and users.paymenttype=2 ;";//2 for AGENT_INVOICE

                //cmd.CommandText = "Select * from agents_bookings,users,booking where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and booking.isDeleted=1";  //and users.paymenttype=2 ;";//2 for AGENT_INVOICE
                dr = cmd.ExecuteReader();
                dt_agents_bookings.Load(dr);
                // return dt_agents_bookings;
                dr.Close();
                #region commnted by yummi 30-3-15 due to mis undersatand project
                //DataTable dt_bookings = new DataTable();
                //cmd.CommandText = "Select * from bookings,users where bookings.date >='" + From + "' AND bookings.date <='" + To + "' and bookings.uid=users.id and users.paymenttype=2 ;";//2 for AGENT_INVOICE
                //dr = cmd.ExecuteReader();
                //dt_bookings.Load(dr);
                //dr.Close(); 
                #endregion

                if (model.ID == 300321) // 300321 means All Agents 
                {
                    List<RegisterModel> agents = new List<RegisterModel>();
                    cmd.CommandText = "Select * from users where active=1 or active is null;";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        agents.Add(new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) });
                    }
                    dr.Close();
                    int index = 0;
                    while (index < agents.Count)
                    {
                        //cmd.CommandText = "Select * from agents_bookings where agent_id='" + agents[index].ID + "'  and date >='" + From + "' AND date <='" + To + "' ;";
                        //dr = cmd.ExecuteReader();
                        //while (dr.Read())
                        //{
                        //    models.Add(new BookingModel { Agent = agents[index].Name, Date = this.ConvertDateForClient(dr.GetString(1)), AgentId = dr.GetInt32(2), Price = dr.GetFloat(3), PassengerName = dr.GetString(4), Adults = dr.GetInt32(5), Children = dr.GetInt32(6), FamilyChildren = dr.GetInt32(7), Infant = dr.GetInt32(8), Voucher = dr.IsDBNull(9) ? null : dr.GetString(9), tourname = dr.GetString(10) });
                        //}
                        //dr.Close();
                        List<DataRow> rows = new List<DataRow>();

                        rows = dt_agents_bookings.Select("agent_id = '" + agents[index].ID + "'").ToList();

                        foreach (DataRow row in rows)
                        {
                            models.Add(new BookingModel { Agent = agents[index].Name, Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), Price = Convert.ToSingle(row[3]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString() });
                        }

                        //============commented by yummi 30-3-15===========//
                        //if (UserType == 0)
                        //{
                        //    List<DataRow> rows_dt_bookings = new List<DataRow>();

                        //    rows_dt_bookings = dt_bookings.Select("uid = '" + agents[index].ID + "'").ToList();

                        //    foreach (DataRow row in rows_dt_bookings)
                        //    {

                        //        models.Add(new BookingModel { Agent = agents[index].Name, Date = this.ConvertDateForClient(row[5].ToString()), AgentId = Convert.ToInt32(row[1]), Price = Convert.ToSingle(row[18]), PassengerName = row[10].ToString(), Adults = Convert.ToInt32(row[11]), Children = Convert.ToInt32(row[14]), FamilyChildren = Convert.ToInt32(row[12]), Infant = Convert.ToInt32(row[13]), Voucher = row.IsNull(3) ? null : row[3].ToString(), tourname = row[7].ToString() });
                        //    }
                        //}
                        //============end commented by yummi 30-3-15===========//
                        index++;
                    }
                }
                else
                {
                    cmd.CommandText = "Select * from users where id='" + model.ID + "' ;";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        agent = new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) };
                    }
                    dr.Close();

                    //cmd.CommandText = "Select * from agents_bookings where agent_id='" + model.ID + "'  and date >='" + From + "' AND date <='" + To + "' ;";
                    //dr = cmd.ExecuteReader();
                    //while (dr.Read())
                    //{
                    //    models.Add(new BookingModel { Agent = agent.Name, Date = this.ConvertDateForClient(dr.GetString(1)), AgentId = dr.GetInt32(2), Price = dr.GetFloat(3), PassengerName = dr.GetString(4), Adults = dr.GetInt32(5), Children = dr.GetInt32(6), FamilyChildren = dr.GetInt32(7), Infant = dr.GetInt32(8), Voucher = dr.IsDBNull(9) ? null : dr.GetString(9), tourname = dr.GetString(10) });
                    //}
                    //dr.Close();
                    List<DataRow> rows = new List<DataRow>();

                    rows = dt_agents_bookings.Select("agentid = '" + model.ID + "'").ToList();

                    foreach (DataRow row in rows)
                    {
                        models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), Price = Convert.ToSingle(row[3]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString() });
                        //   models.Add(new BookingModel { Agent = agent.Name, Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), Price = Convert.ToSingle(row[3]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString() });

                    }
                    //============commented by yummi 30-3-15===========//
                    //if (UserType == 0)
                    //{
                    //    List<DataRow> rows_dt_bookings = new List<DataRow>();

                    //    rows_dt_bookings = dt_bookings.Select("uid = '" + model.ID + "'").ToList();

                    //    foreach (DataRow row in rows_dt_bookings)
                    //    {

                    //        models.Add(new BookingModel { Agent = agent.Name, Date = this.ConvertDateForClient(row[5].ToString()), AgentId = Convert.ToInt32(row[1]), Price = Convert.ToSingle(row[18]), PassengerName = row[10].ToString(), Adults = Convert.ToInt32(row[11]), Children = Convert.ToInt32(row[14]), FamilyChildren = Convert.ToInt32(row[12]), Infant = Convert.ToInt32(row[13]), Voucher = row.IsNull(3) ? null : row[3].ToString(), tourname = row[7].ToString() });
                    //    }
                    //}
                    //============end commented by yummi 30-3-15===========//
                }
                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();

                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();


                var pdf = new PdfResult(models, "CreateAgentsReport");
                if (model.ID == 300321)
                {
                    pdf.ViewBag.Title = "All Agents Reports";
                    pdf.ViewBag.Address = string.Empty;
                }
                else
                {
                    pdf.ViewBag.Title = agent.Name;
                    pdf.ViewBag.Address = agent.Address;
                }
                pdf.ViewBag.Invoice = invoice;
                if (model.ID != 300321)
                    pdf.ViewBag.Single = true;
                else
                {
                    pdf.ViewBag.Single = false;
                }
                return pdf;
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View("CreateAgentsReport");
        }
        */
        //
        #endregion

        #region commented on 28-03-2017 for Itexshap issue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ViewAgentsReports(RegisterModel model)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var From = DateTime.ParseExact(model.FromDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            var To = DateTime.ParseExact(model.ToDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            RegisterModel agent = null;
            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                DataTable dt_agents_bookings = new DataTable();
                //cmd.CommandText = "Select * from agents_bookings,users,bookings where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and bookings.voucherid=agents_bookings.voucherid and bookings.isdeleted =0;";


                cmd.CommandText = "Select b.*, t.tourname from bookings b join tournames t on b.tourid=t.id where date(b.date) >='" + From + "' AND date(b.date) <='" + To + "' and b.isdeleted =0;";
                dr = cmd.ExecuteReader();
                dt_agents_bookings.Load(dr);
                dr.Close();


                if (model.ID == 300321) // 300321 means All Agents 
                {

                    List<RegisterModel> agents = new List<RegisterModel>();
                    cmd.CommandText = "Select * from users where active=1 or active is null;";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        agents.Add(new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) });
                    }
                    dr.Close();
                    int index = 0;
                    while (index < agents.Count)
                    {
                        List<DataRow> rows = new List<DataRow>();

                        rows = dt_agents_bookings.Select("agentid = '" + agents[index].ID + "'").ToList();

                        foreach (DataRow row in rows)
                        {
                            float agent_invoice = row["agentinvoice"] == DBNull.Value ? default(float) : (float)row["agentinvoice"];
                            //  models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString(), InvoiceAgent = agent_invoice, Price = (float)(row["totalprice"]) });
                            //models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucherid") ? null : row["voucherid"].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]) });
                            models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucher") ? null : row["voucher"].ToString(), tourname = row["tourname"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]), TotalPrice = (float)(row["totalprice"]) });
                        }

                        index++;
                    }
                }
                else
                {
                    cmd.CommandText = "Select * from users where id='" + model.ID + "' ;";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        agent = new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) };
                    }
                    dr.Close();

                    List<DataRow> rows = new List<DataRow>();

                    rows = dt_agents_bookings.Select("agentid = '" + model.ID + "'").ToList();

                    foreach (DataRow row in rows)
                    {
                        float agent_invoice = row["agentinvoice"] == DBNull.Value ? default(float) : (float)row["agentinvoice"];
                        // models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString(), InvoiceAgent = agent_invoice, Price = (float)(row["totalprice"]) });
                        //models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucherid") ? null : row["voucherid"].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["totalprice"]) });
                        models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucher") ? null : row["voucher"].ToString(), tourname = row["tourname"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]), TotalPrice = (float)(row["totalprice"]) });
                    }
                }
                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();

                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();


                var pdf = new PdfResult(models, "CreateAgentsReport");
                if (model.ID == 300321)
                {
                    pdf.ViewBag.Title = "All Agents Reports";
                    pdf.ViewBag.Address = string.Empty;
                }
                else
                {
                    pdf.ViewBag.Title = agent.Name;
                    pdf.ViewBag.Address = agent.Address;
                }
                pdf.ViewBag.Invoice = invoice;
                if (model.ID != 300321)
                    pdf.ViewBag.Single = true;
                else
                {
                    pdf.ViewBag.Single = false;
                }
                return pdf;
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View("CreateAgentsReport");
        }
        #endregion


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadAgentsReports(RegisterModel model)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var From = DateTime.ParseExact(model.FromDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            var To = DateTime.ParseExact(model.ToDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            RegisterModel agent = null;
            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                DataTable dt_agents_bookings = new DataTable();
                //cmd.CommandText = "Select * from agents_bookings,users,bookings where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and bookings.voucherid=agents_bookings.voucherid and bookings.isdeleted =0;";


                cmd.CommandText = "Select b.*, t.tourname from bookings b join tournames t on b.tourid=t.id where date(b.date) >='" + From + "' AND date(b.date) <='" + To + "' and b.isdeleted =0;";
                dr = cmd.ExecuteReader();
                dt_agents_bookings.Load(dr);
                dr.Close();


                if (model.ID == 300321) // 300321 means All Agents 
                {

                    List<RegisterModel> agents = new List<RegisterModel>();
                    cmd.CommandText = "Select * from users where active=1 or active is null;";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        agents.Add(new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) });
                    }
                    dr.Close();
                    int index = 0;
                    while (index < agents.Count)
                    {
                        List<DataRow> rows = new List<DataRow>();

                        rows = dt_agents_bookings.Select("agentid = '" + agents[index].ID + "'").ToList();

                        foreach (DataRow row in rows)
                        {
                            float agent_invoice = row["agentinvoice"] == DBNull.Value ? default(float) : (float)row["agentinvoice"];
                            //  models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString(), InvoiceAgent = agent_invoice, Price = (float)(row["totalprice"]) });
                            //models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucherid") ? null : row["voucherid"].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]) });
                            models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucher") ? null : row["voucher"].ToString(), tourname = row["tourname"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]), TotalPrice = (float)(row["totalprice"]) });
                        }

                        index++;
                    }
                }
                else
                {
                    cmd.CommandText = "Select * from users where id='" + model.ID + "' ;";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        agent = new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) };
                    }
                    dr.Close();

                    List<DataRow> rows = new List<DataRow>();

                    rows = dt_agents_bookings.Select("agentid = '" + model.ID + "'").ToList();

                    foreach (DataRow row in rows)
                    {
                        float agent_invoice = row["agentinvoice"] == DBNull.Value ? default(float) : (float)row["agentinvoice"];
                        // models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString(), InvoiceAgent = agent_invoice, Price = (float)(row["totalprice"]) });
                        //models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucherid") ? null : row["voucherid"].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["totalprice"]) });
                        models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucher") ? null : row["voucher"].ToString(), tourname = row["tourname"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]), TotalPrice = (float)(row["totalprice"]) });
                    }
                }
                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();

                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();

                string title, address;
                bool single;
                if (model.ID == 300321)
                {
                    title = "All Agents Reports";
                    address = string.Empty;
                }
                else
                {
                    title = agent.Name;
                    address = agent.Address;
                }
                if (model.ID != 300321)
                {
                    single = true;
                }
                else
                {
                    single = false;
                }

                MemoryStream ms = CreatePDF(models, title, address, invoice.ToString(), single);

                return File(ms.ToArray(), "application/pdf", "AgentsReports_" + invoice.ToString() + ".pdf");
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
        }

        #region  commented on 09-02-2016
        //[HttpPost]
        //public ActionResult ViewAgentsReportDetails(string FromDate, string ToDate, int ID)
        //{
        //    string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
        //    var From = DateTime.ParseExact(FromDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
        //    var To = DateTime.ParseExact(ToDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
        //    bool isError = false;
        //    RegisterModel agent = null;
        //    try
        //    {
        //        List<BookingModel> models = new List<BookingModel>();
        //        connection.Open();
        //        cmd = connection.CreateCommand();
        //        DataTable dt_agents_bookings = new DataTable();
        //        //cmd.CommandText = "Select * from agents_bookings,users where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id";   // and users.paymenttype=2 ;"; comment on 26-06-2015 because we display need to display all booking entry  //2 for AGENT_INVOICE
        //        //Commented on 28-06-2016
        //        cmd.CommandText = "Select * from agents_bookings,users,bookings where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and bookings.voucherid=agents_bookings.voucherid and bookings.isdeleted =0;";   // and users.paymenttype=2 ;"; comment on 26-06-2015 because we display need to display all booking entry  //2 for AGENT_INVOICE
        //        //cmd.CommandText = "select * from bookings b,users u  where b.date >='" + From + "' and b.date <='" + To + "' and isDeleted = 0 and b.agentid = u.id ";
        //        dr = cmd.ExecuteReader();
        //        dt_agents_bookings.Load(dr);
        //        dr.Close();
        //        #region commneted by yummi 30-3-15 due to mis understand syatem
        //        //DataTable dt_bookings = new DataTable();
        //        //cmd.CommandText = "Select * from bookings,users where bookings.date >='" + From + "' AND bookings.date <='" + To + "' and bookings.uid=users.id and users.paymenttype=2 ;";//2 for AGENT_INVOICE
        //        //dr = cmd.ExecuteReader();
        //        //dt_bookings.Load(dr);
        //        //dr.Close(); 
        //        #endregion

        //        if (ID == 300321) // 300321 means All Agents 
        //        {
        //            List<RegisterModel> agents = new List<RegisterModel>();
        //            cmd.CommandText = "Select * from users;";
        //            dr = cmd.ExecuteReader();
        //            while (dr.Read())
        //            {
        //                agents.Add(new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) });
        //            }
        //            dr.Close();
        //            int index = 0;
        //            while (index < agents.Count)
        //            {
        //                //      cmd.CommandText = "Select * from agents_bookings where agent_id='" + agents[index].ID + "'  and date >='" + From + "' AND date <='" + To + "' ;";
        //                List<DataRow> rows = new List<DataRow>();

        //                rows = dt_agents_bookings.Select("agent_id = '" + agents[index].ID + "'").ToList();

        //                foreach (DataRow row in rows)
        //                {
        //                    // models.Add(new BookingModel { Agent = agents[index].Name, Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), Price = Convert.ToSingle(row[3]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString() });
        //                    models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), Price = Convert.ToSingle(row[3]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString() });
        //                }
        //                //commented on 30-3-15 by yummi 
        //                #region commented on 30-3-15 by yummi
        //                //if (UserType == 0)
        //                //{
        //                //    List<DataRow> rows_dt_bookings = new List<DataRow>();

        //                //    rows_dt_bookings = dt_bookings.Select("uid = '" + agents[index].ID + "'").ToList();

        //                //    foreach (DataRow row in rows_dt_bookings)
        //                //    {

        //                //        models.Add(new BookingModel { Agent = agents[index].Name, Date = this.ConvertDateForClient(row[5].ToString()), AgentId = Convert.ToInt32(row[1]), Price = Convert.ToSingle(row[18]), PassengerName = row[10].ToString(), Adults = Convert.ToInt32(row[11]), Children = Convert.ToInt32(row[14]), FamilyChildren = Convert.ToInt32(row[12]), Infant = Convert.ToInt32(row[13]), Voucher = row.IsNull(3) ? null : row[3].ToString(), tourname = row[7].ToString() });
        //                //    }
        //                //} 
        //                #endregion

        //                // ------------------
        //                //////////////DataRow[] results_dt_agents_bookings = dt_agents_bookings.Select("agent_id = '" + agents[index].ID + "'");


        //                //////////////foreach (var row in results_dt_agents_bookings)
        //                //////////////{
        //                //////////////    models.Add(new BookingModel { Agent = agents[index].Name, Date = this.ConvertDateForClient(dr.GetString(1)), AgentId = dr.GetInt32(2), Price = dr.GetFloat(3), PassengerName = dr.GetString(4), Adults = dr.GetInt32(5), Children = dr.GetInt32(6), FamilyChildren = dr.GetInt32(7), Infant = dr.GetInt32(8), Voucher = dr.IsDBNull(9) ? null : dr.GetString(9), tourname = dr.GetString(10) });

        //                //////////////}
        //                // ----------
        //                ////cmd.CommandText = "Select * from agents_bookings,users where agents_bookings.agent_id='" + agents[index].ID + "'  and agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and users.paymenttype=2 ;";//2 for AGENT_INVOICE

        //                ////dr = cmd.ExecuteReader();
        //                ////while (dr.Read())
        //                ////{
        //                ////    models.Add(new BookingModel { Agent = agents[index].Name, Date = this.ConvertDateForClient(dr.GetString(1)), AgentId = dr.GetInt32(2), Price = dr.GetFloat(3), PassengerName = dr.GetString(4), Adults = dr.GetInt32(5), Children = dr.GetInt32(6), FamilyChildren = dr.GetInt32(7), Infant = dr.GetInt32(8), Voucher = dr.IsDBNull(9) ? null : dr.GetString(9), tourname = dr.GetString(10) });
        //                ////}
        //                ////dr.Close();

        //                ////if (UserType == 0)
        //                ////{
        //                ////    cmd.CommandText = "Select * from bookings,users where bookings.uid='" + agents[index].ID + "'  and bookings.date >='" + From + "' AND bookings.date <='" + To + "' and bookings.uid=users.id and users.paymenttype=2 ;";//2 for AGENT_INVOICE

        //                ////    dr = cmd.ExecuteReader();
        //                ////    while (dr.Read())
        //                ////    {
        //                ////        models.Add(new BookingModel { Agent = agents[index].Name, Date = this.ConvertDateForClient(dr.GetString(5)), AgentId = dr.GetInt32(1), Price = dr.GetFloat(18), PassengerName = dr.GetString(10), Adults = dr.GetInt32(11), Children = dr.GetInt32(14), FamilyChildren = dr.GetInt32(11), Infant = dr.GetInt32(12), Voucher = dr.IsDBNull(3) ? null : dr.GetString(3), tourname = dr.GetString(7) });
        //                ////    }
        //                ////}
        //                ////dr.Close();
        //                index++;
        //            }
        //        }
        //        else
        //        {
        //            cmd.CommandText = "Select * from users where id='" + ID + "' ;";
        //            dr = cmd.ExecuteReader();
        //            while (dr.Read())
        //            {
        //                agent = new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) };
        //            }
        //            dr.Close();
        //            List<DataRow> rows = new List<DataRow>();
        //            rows = dt_agents_bookings.Select("agentid = '" + ID + "'").ToList();

        //            foreach (DataRow row in rows)
        //            {
        //                //models.Add(new BookingModel { Agent = agent.Name, Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), Price = Convert.ToSingle(row[3]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString() });
        //                models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), Price = Convert.ToSingle(row[3]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString() });
        //                //models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row[0].ToString()), Date = this.ConvertDateForClient(row[5].ToString()), AgentId = Convert.ToInt32(row[22]), Price = Convert.ToSingle(row[15]), PassengerName = row[10].ToString(), Adults = Convert.ToInt32(row[11]), Children = Convert.ToInt32(row[14]), FamilyChildren = Convert.ToInt32(row[12]), Infant = Convert.ToInt32(row[13]), Voucher = row.IsNull(24) ? null : row[24].ToString(), tourname = row[30].ToString() });
        //            }
        //            #region commented on 30-3-15 by yummi
        //            //if (UserType == 0)
        //            //{
        //            //    List<DataRow> rows_dt_bookings = new List<DataRow>();

        //            //    rows_dt_bookings = dt_bookings.Select("uid = '" + ID + "'").ToList();

        //            //    foreach (DataRow row in rows_dt_bookings)
        //            //    {

        //            //        models.Add(new BookingModel { Agent = agent.Name, Date = this.ConvertDateForClient(row[5].ToString()), AgentId = Convert.ToInt32(row[1]), Price = Convert.ToSingle(row[18]), PassengerName = row[10].ToString(), Adults = Convert.ToInt32(row[11]), Children = Convert.ToInt32(row[14]), FamilyChildren = Convert.ToInt32(row[12]), Infant = Convert.ToInt32(row[13]), Voucher = row.IsNull(3) ? null : row[3].ToString(), tourname = row[7].ToString() });
        //            //    }
        //            //} 
        //            #endregion
        //            ////original//   cmd.CommandText = "Select * from agents_bookings where agent_id='" + ID + "'  and date >='" + From + "' AND date <='" + To + "' ;";
        //            ////Data FROM agents_bookings TABLE

        //            //cmd.CommandText = "Select * from agents_bookings,users where agents_bookings.agent_id='" + ID + "'  and agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and users.paymenttype=2 ;";//2 for AGENT_INVOICE


        //            //dr = cmd.ExecuteReader();
        //            //while (dr.Read())
        //            //{
        //            //    models.Add(new BookingModel { Agent = agent.Name, Date = this.ConvertDateForClient(dr.GetString(1)), AgentId = dr.GetInt32(2), Price = dr.GetFloat(3), PassengerName = dr.GetString(4), Adults = dr.GetInt32(5), Children = dr.GetInt32(6), FamilyChildren = dr.GetInt32(7), Infant = dr.GetInt32(8), Voucher = dr.IsDBNull(9) ? null : dr.GetString(9), tourname = dr.GetString(10) });
        //            //}
        //            //dr.Close();


        //            ////Data FROM bookings TABLE
        //            //if (UserType == 0)
        //            //{
        //            //    cmd.CommandText = "Select * from bookings,users where bookings.uid='" + ID + "'  and bookings.date >='" + From + "' AND bookings.date <='" + To + "' and bookings.uid=users.id and users.paymenttype=2 ;";//2 for AGENT_INVOICE

        //            //    dr = cmd.ExecuteReader();
        //            //    while (dr.Read())
        //            //    {
        //            //        models.Add(new BookingModel { Agent = agent.Name, Date = this.ConvertDateForClient(dr.GetString(5)), AgentId = dr.GetInt32(1), Price = dr.GetFloat(18), PassengerName = dr.GetString(10), Adults = dr.GetInt32(11), Children = dr.GetInt32(14), FamilyChildren = dr.GetInt32(11), Infant = dr.GetInt32(12), Voucher = dr.IsDBNull(3) ? null : dr.GetString(3), tourname = dr.GetString(7) });
        //            //    }
        //            //}
        //            //dr.Close();
        //        }
        //        cmd.CommandText = "Select * from invoicegenerator;";
        //        dr = cmd.ExecuteReader();
        //        int invoice = 0;
        //        while (dr.Read())
        //        {
        //            invoice = dr.GetInt32(1);
        //        }
        //        dr.Close();
        //        cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
        //        cmd.ExecuteNonQuery();

        //        if (connection.State == System.Data.ConnectionState.Open)
        //            connection.Close();
        //        TempClass obj = new TempClass
        //        {
        //            models = models,
        //            single = ID != 300321
        //        };
        //        return View("CreateAgentsReportView", obj);

        //        //var pdf = new PdfResult(models, "CreateAgentsReport");
        //        //if (ID == 300321)
        //        //{
        //        //    pdf.ViewBag.Title = "All Agents Reports";
        //        //    pdf.ViewBag.Address = string.Empty;
        //        //}
        //        //else
        //        //{
        //        //    pdf.ViewBag.Title = agent.Name;
        //        //    pdf.ViewBag.Address = agent.Address;
        //        //}
        //        //pdf.ViewBag.Invoice = invoice;
        //        //if (ID != 300321)
        //        //    pdf.ViewBag.Single = true;
        //        //else
        //        //{
        //        //    pdf.ViewBag.Single = false;
        //        //}
        //        //return pdf;
        //    }
        //    catch (Exception e)
        //    {
        //        isError = true;
        //        log.Error(e.Message, e);
        //    }

        //    if (!isError)
        //        ViewBag.Status = "true";
        //    else
        //        ViewBag.Status = "error";

        //    return View("CreateAgentsReport");

        //}

        #endregion

        [HttpPost]
        public ActionResult ViewAgentsReportDetails(string FromDate, string ToDate, int ID)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };

            var From = DateTime.ParseExact(FromDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            var To = DateTime.ParseExact(ToDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            RegisterModel agent = null;
            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                DataTable dt_agents_bookings = new DataTable();

                //  cmd.CommandText = "Select * from agents_bookings,users,bookings where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and bookings.voucherid=agents_bookings.voucherid and bookings.isdeleted =0;";   // and users.paymenttype=2 ;"; comment on 26-06-2015 because we display need to display all booking entry  //2 for AGENT_INVOICE
                cmd.CommandText = "Select * from bookings where date(bookings.date) >='" + From + "' AND date(bookings.date) <='" + To + "' and bookings.isdeleted =0;";
                dr = cmd.ExecuteReader();
                dt_agents_bookings.Load(dr);
                dr.Close();

                if (ID == 300321) // 300321 means All Agents 
                {
                    List<RegisterModel> agents = new List<RegisterModel>();
                    cmd.CommandText = "Select * from users;";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        agents.Add(new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) });
                    }
                    dr.Close();
                    int index = 0;
                    while (index < agents.Count)
                    {
                        List<DataRow> rows = new List<DataRow>();

                        rows = dt_agents_bookings.Select("agentid = '" + agents[index].ID + "'").ToList();

                        foreach (DataRow row in rows)
                        {
                            float agent_invoice = row["agentinvoice"] == DBNull.Value ? default(float) : (float)row["agentinvoice"];
                            //models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]) ,PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["totalprice"]) });
                            models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucherid") ? null : row["voucherid"].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]), TotalPrice = (float)(row["totalprice"]) });
                        }
                        index++;
                    }
                }
                else
                {
                    cmd.CommandText = "Select * from users where id='" + ID + "' ;";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        agent = new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) };
                    }
                    dr.Close();
                    List<DataRow> rows = new List<DataRow>();
                    rows = dt_agents_bookings.Select("agentid = '" + ID + "'").ToList();

                    foreach (DataRow row in rows)
                    {
                        float agent_invoice = row["agentinvoice"] == DBNull.Value ? default(float) : (float)row["agentinvoice"];
                        //models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = agent_invoice, Price = (float)(row["totalprice"]) });
                        models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucherid") ? null : row["voucherid"].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]), TotalPrice = (float)(row["totalprice"]) });
                    }

                }

                /*  commented  on 2016-09-05 Think: not need to check invoice count
                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();

                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
                    */

                TempClass obj = new TempClass
                {
                    models = models,
                    single = ID != 300321
                };

                return View("CreateAgentsReportView", obj);
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View("CreateAgentsReport");

        }

        [HttpPost]
        public JsonResult SendViewAgentsReport(RegisterModel model)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var From = DateTime.ParseExact(model.FromDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            var To = DateTime.ParseExact(model.ToDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            RegisterModel agent = null;
            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                DataTable dt_agents_bookings = new DataTable();
                //cmd.CommandText = "Select * from agents_bookings,users,bookings where agents_bookings.date >='" + From + "' AND agents_bookings.date <='" + To + "' and agents_bookings.agent_id=users.id and bookings.voucherid=agents_bookings.voucherid and bookings.isdeleted =0;";


                cmd.CommandText = "Select b.*, t.tourname from bookings b join tournames t on b.tourid=t.id where date(b.date) >='" + From + "' AND date(b.date) <='" + To + "' and b.isdeleted =0;";
                dr = cmd.ExecuteReader();
                dt_agents_bookings.Load(dr);
                dr.Close();


                if (model.ID == 300321) // 300321 means All Agents 
                {

                    List<RegisterModel> agents = new List<RegisterModel>();
                    cmd.CommandText = "Select * from users where active=1 or active is null;";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        agents.Add(new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) });
                    }
                    dr.Close();
                    int index = 0;
                    while (index < agents.Count)
                    {
                        List<DataRow> rows = new List<DataRow>();

                        rows = dt_agents_bookings.Select("agentid = '" + agents[index].ID + "'").ToList();

                        foreach (DataRow row in rows)
                        {
                            float agent_invoice = row["agentinvoice"] == DBNull.Value ? default(float) : (float)row["agentinvoice"];
                            //  models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString(), InvoiceAgent = agent_invoice, Price = (float)(row["totalprice"]) });
                            //models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucherid") ? null : row["voucherid"].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]) });
                            models.Add(new BookingModel { Agent = agents[index].Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucher") ? null : row["voucher"].ToString(), tourname = row["tourname"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]), TotalPrice = (float)(row["totalprice"]) });
                        }

                        index++;
                    }
                }
                else
                {
                    cmd.CommandText = "Select * from users where id='" + model.ID + "' ;";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        agent = new RegisterModel { ID = dr.GetInt32(0), Name = dr.GetString(1), Address = dr.GetString(2) };
                    }
                    dr.Close();

                    List<DataRow> rows = new List<DataRow>();

                    rows = dt_agents_bookings.Select("agentid = '" + model.ID + "'").ToList();

                    foreach (DataRow row in rows)
                    {
                        float agent_invoice = row["agentinvoice"] == DBNull.Value ? default(float) : (float)row["agentinvoice"];
                        // models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row[27].ToString()), Date = this.ConvertDateForClient(row[1].ToString()), AgentId = Convert.ToInt32(row[2]), PassengerName = row[4].ToString(), Adults = Convert.ToInt32(row[5]), Children = Convert.ToInt32(row[6]), FamilyChildren = Convert.ToInt32(row[7]), Infant = Convert.ToInt32(row[8]), Voucher = row.IsNull(9) ? null : row[9].ToString(), tourname = row[10].ToString(), InvoiceAgent = agent_invoice, Price = (float)(row["totalprice"]) });
                        //models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucherid") ? null : row["voucherid"].ToString(), tourname = row["tourcode"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["totalprice"]) });
                        models.Add(new BookingModel { Agent = agent.Name, BookingID = Convert.ToInt32(row["id"].ToString()), Date = this.ConvertDateForClient(row["date"].ToString()), AgentId = Convert.ToInt32(row["agentid"]), PassengerName = row["passenger"].ToString(), Adults = Convert.ToInt32(row["adults"]), Children = Convert.ToInt32(row["children"]), FamilyChildren = Convert.ToInt32(row["familychildren"]), Infant = Convert.ToInt32(row["Infant"]), Voucher = row.IsNull("voucher") ? null : row["voucher"].ToString(), tourname = row["tourname"].ToString(), InvoiceAgent = (agent_invoice), Price = (float)(row["Price"]), TotalPrice = (float)(row["totalprice"]) });
                    }
                }
                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();

                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();

                string title, address;
                bool single;
                if (model.ID == 300321)
                {
                    title = "All Agents Reports";
                    address = string.Empty;
                }
                else
                {
                    title = agent.Name;
                    address = agent.Address;
                }
                if (model.ID != 300321)
                {
                    single = true;
                }
                else
                {
                    single = false;
                }

                MemoryStream ms = CreatePDF(models, title, address, invoice.ToString(), single);

                byte[] bytes = ms.ToArray(); 
                
                Stream stream = new MemoryStream(bytes);

                isError = EmailHelper.SendEmail("treknorth.com.au@gmail.com", model.Email, "Agent Invoice Report", "PFA", stream, "AgentsReports_" + invoice.ToString() + ".pdf", false);
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return Json(ViewBag.Status);
        }

        // Convert an object to a byte array
        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        public JsonResult getSeats(int tourid)
        {
            List<Seat> models = new List<Seat>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select distinct date,tourid,available from available_seats where tourid='" + tourid + "' and date > '" + DateTime.Now.ToString("yyyy-MM-dd") + "';";
                //cmd.CommandText = "select distinct available, date, tourid from available_seats where tourid='" + tourid + "' and date > '" + DateTime.Now.ToString("yyyy-MM-dd") + "';";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    models.Add(new Seat { Date = this.ConvertDateForClient(dr.GetString(0)), available = Int32.Parse(dr.GetString(2)), ForSorting = Convert.ToDateTime(dr.GetString(0)) });
                    //models.Add(new Seat { Date = this.ConvertDateForClient(dr.GetString(2)), available = Int32.Parse(dr.GetString(3)), ForSorting = Convert.ToDateTime(dr.GetString(2)) });
                }
                models = models.OrderBy(o => o.ForSorting).ToList();

                dr.Close();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }


            return Json(models);
        }

        public ActionResult ViewSeats()
        {
            ViewBag.Status = "true";
            return View();
        }

        public JsonResult getLocations(int tourid, bool check)
        {
            var repository = new BookingRepository();
            return Json(repository.GetPickupLocation(tourid));

        }

        public JsonResult getTimes(int pickupid)
        {
            var repository = new BookingRepository();
            return Json(repository.GetTimes(pickupid));

        }

        public string getTimess(int pickupid)
        {
            List<Time> lstTime = new List<Time>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from times where locationid ='" + pickupid + "';";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Time time = new Time();
                    time.time = dr.GetString("time");
                    time.timeId = dr.GetInt32("timeId");
                    lstTime.Add(time);
                }
                dr.Close();

                JavaScriptSerializer jscript = new JavaScriptSerializer();
                return jscript.Serialize(lstTime);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                return "NoResult";
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }


        }

        /*
        public string getTimess(int pickupid)
        {
            List<Time> lstTime = new List<Time>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from times where locationid ='" + pickupid + "';";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Time time = new Time();
                    time.time = dr.GetString("time");
                    time.timeId = dr.GetInt32("timeId");
                    lstTime.Add(time);
                }
                dr.Close();

                List<SelectListItem> listItems = new List<SelectListItem>();
                listItems.Clear();
                listItems.Add(new SelectListItem()
                {
                    Value = "-1",
                    Text = "--SELECT--"
                });
                foreach (Time t in lstTime)
                {
                    listItems.Add(new SelectListItem()
                    {
                        Value = Convert.ToString(t.timeId),
                        Text = t.time,
                    });
                }
                ViewBag.LoadCustomerContactDropDown = new SelectList(listItems, "Value", "Text", 0);
               // return Json(new { success = listItems });
                return "tes";
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                return null;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }*/

        public JsonResult getTourCodes(int tourid)
        {
            var repository = new BookingRepository();
            return Json(repository.GetTourCodes(tourid));
        }

        [HttpPost]
        public JsonResult getActiveTourCodes()
        {
            var repository = new BookingRepository();
            return Json(repository.GetTourCodes());

        }


        public JsonResult getTourName()
        {
            var repository = new BookingRepository();
            return Json(repository.GetTourNames());

        }

        public ActionResult disableBooking(string bid, int tId, string tdt, int tseat)
        {
            //connection.Open();
            //cmd = connection.CreateCommand();
            //cmd.CommandText = "UPDATE bookings SET isDeleted = 1 where id = " + bookingid;
            //cmd.ExecuteNonQuery();
            //if (connection.State == System.Data.ConnectionState.Open)
            //    connection.Close();
            //return RedirectToAction("ViewAllBookings", "Booking");


            string[] tdate = tdt.Split('/');

            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE bookings SET isDeleted = 1  where id = " + bid;
            if (cmd.ExecuteNonQuery() == 1)
            {
                cmd.CommandText = "select available from available_seats where tourid =" + tId + " and date='" + tdate[2] + "-" + tdate[1] + "-" + tdate[0] + "'";
                int totseatavailbe = Convert.ToInt16(cmd.ExecuteScalar().ToString()) + tseat;
                cmd.CommandText = "UPDATE available_seats SET available =" + totseatavailbe + " where tourid =" + tId + " and date='" + tdate[2] + "-" + tdate[1] + "-" + tdate[0] + "'";
                cmd.ExecuteNonQuery();
            }
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            return RedirectToAction("ViewAllBookings", "Booking");

        }

        public ActionResult DeleteBooking(string bid, int tId, string tdt, int tseat)
        {
            //connection.Open();
            //cmd = connection.CreateCommand();
            //cmd.CommandText = "UPDATE bookings SET isDeleted = 1 where id = " + bookingid;
            //// cmd.CommandText = "DELETE FROM bookings WHERE id=" + bookingid;
            //cmd.ExecuteNonQuery();
            //if (connection.State == System.Data.ConnectionState.Open)
            //    connection.Close();
            //return RedirectToAction("ViewAllBookings", "Booking");


            string[] tdate = tdt.Split('/');

            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE bookings SET isDeleted = 1  where id = " + bid;
            if (cmd.ExecuteNonQuery() == 1)
            {
                cmd.CommandText = "select available from available_seats where tourid =" + tId + " and date='" + tdate[2] + "-" + tdate[1] + "-" + tdate[0] + "'";
                //cmd.CommandText = "select available from available_seats where tourid =" + tId + " and date='" + tdt + "'";
                int totseatavailbe = Convert.ToInt16(cmd.ExecuteScalar().ToString()) + tseat;
                cmd.CommandText = "UPDATE available_seats SET available =" + totseatavailbe + " where tourid =" + tId + " and date='" + tdate[2] + "-" + tdate[1] + "-" + tdate[0] + "'";
                //cmd.CommandText = "UPDATE available_seats SET available =" + totseatavailbe + " where tourid =" + tId + " and date='" + tdt + "'";
                cmd.ExecuteNonQuery();
            }
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            return RedirectToAction("ViewAllBookings", "Booking");
        }


        //[HttpPost]
        //public string DeleteBookingFromSeats(string bid, int tId, string tdt, int tseat)
        //{

        //    connection.Open();
        //    cmd = connection.CreateCommand();
        //    cmd.CommandText = "UPDATE bookings SET isDeleted = 1  where id = " + bid;
        //    if (cmd.ExecuteNonQuery() == 1)
        //    {
        //        cmd.CommandText = "select available from available_seats where tourid =" + tId + " and date='" + tdt + "'";
        //        int totseatavailbe = Convert.ToInt16(cmd.ExecuteScalar().ToString()) + tseat;
        //        cmd.CommandText = "UPDATE available_seats SET available =" + totseatavailbe + " where tourid =" + tId + " and date='" + tdt + "'";
        //        cmd.ExecuteNonQuery();
        //    }
        //    if (connection.State == System.Data.ConnectionState.Open)
        //        connection.Close();

        //    return "Booking deleted Successfully";
        //}


        public ActionResult ViewAllBookings()
        {
            List<BookingModel> models = new List<BookingModel>();
            ViewBag.Status = "true";
            
            return View(models);
        }

        [HttpPost]
        public JsonResult ViewAllBookingsData()
        {
            var sEcho = Request.Form.GetValues("sEcho").FirstOrDefault();
            var start = Request.Form.GetValues("iDisplayStart").FirstOrDefault();
            var length = Request.Form.GetValues("iDisplayLength").FirstOrDefault();
            var sortColumnIndex = Request.Form.GetValues("iSortCol_0").FirstOrDefault();
            var sortColumn = "Id";
            var sortColumnDir = Request.Form.GetValues("sSortDir_0").FirstOrDefault();
            var searchValue = Request.Form.GetValues("sSearch").FirstOrDefault().ToLower();

            switch (sortColumnIndex)
            {
                case "0":
                    sortColumn = "Id";
                    break;
                case "1":
                    sortColumn = "Agent";
                    break;
                case "2":
                    sortColumn = "Voucher";
                    break;
                case "3":
                    sortColumn = "Date";
                    break;
                case "4":
                    sortColumn = "tourname";
                    break;
                case "5":
                    sortColumn = "Passenger";
                    break;
                case "6":
                    sortColumn = "Adults";
                    break;
                case "7":
                    sortColumn = "FamilyChildren";
                    break;
                case "8":
                    sortColumn = "Children";
                    break;
                case "9":
                    sortColumn = "Infant";
                    break;
                case "10":
                    sortColumn = "Price";
                    break;
                case "11":
                    sortColumn = "Discount";
                    break;
                case "12":
                    sortColumn = "Commission";
                    break;
                case "13":
                    sortColumn = "TotalPrice";
                    break;
                case "14":
                    sortColumn = "name";
                    break;
                default:
                    sortColumn = "Id";
                    break;
            }
            
            //Paging Size (10,20,50,100)    
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int recordsTotal = 0;

            List<BookingModel> models = new List<BookingModel>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                //Sorting    
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDir)))
                {
                    cmd.CommandText = "Select b.*,t.tourname,u.name from bookings b inner join tournames t on b.tourid= t.id inner join users u on b.uid = u.id ORDER BY `"+ sortColumn + "` "+ sortColumnDir + ";";
                }
                else
                {
                    cmd.CommandText = "Select b.*,t.tourname,u.name from bookings b inner join tournames t on b.tourid= t.id inner join users u on b.uid = u.id ORDER BY `id` DESC;";
                }
                
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    models.Add(new BookingModel
                    {
                        BookingID = dr.GetInt32(0),
                        Agent = dr.GetString(2),
                        Voucher = dr.IsDBNull(3) ? null : dr.GetString(3),
                        Reference = dr.IsDBNull(4) ? null : dr.GetString(4),
                        Date = this.ConvertDateForClient(dr.GetString(5)),
                        //  Tour = dr.GetInt32(6),
                        TourCode = dr.GetInt32(7),
                        pickuplocation = dr.GetInt32(8),
                        time = dr.GetString(9),
                        PassengerName = dr.GetString(10),
                        Adults = dr.GetInt32(11),
                        FamilyChildren = dr.GetInt32(12),
                        Infant = dr.GetInt32(13),
                        Children = dr.GetInt32(14),
                        Price = dr.GetInt32(15),
                        Discount = dr.GetInt32(16),
                        Commission = dr.GetFloat(17),
                        TotalPrice = dr.GetFloat(18),
                        ContactDetails = dr.GetString(19),
                        Comments = dr.GetString(20),
                        isDeleted = dr.GetBoolean("isDeleted"),
                        Fish = (dr["Fish"] == DBNull.Value ? 0 : dr.GetInt32("Fish")),
                        Steak = (dr["Steak"] == DBNull.Value ? 0 : dr.GetInt32("Steak")),
                        Vegetarian = (dr["Vegetarian"] == DBNull.Value ? 0 : dr.GetInt32("Vegetarian")),
                        tourname = dr.GetString("tourname"),
                        name = dr.GetString("name"),

                    });
                }
                dr.Close();

                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();

                //Search    
                if (!string.IsNullOrEmpty(searchValue))
                {
                    models = models.Where(
                        m => m.BookingID.ToString().Contains(searchValue) ||
                        ( !string.IsNullOrEmpty(m.Agent) && m.Agent.ToLower().Contains(searchValue)) ||
                        ( !string.IsNullOrEmpty(m.Voucher) && m.Voucher.ToLower().Contains(searchValue)) ||
                        ( !string.IsNullOrEmpty(m.tourname) && m.tourname.ToLower().Contains(searchValue)) ||
                        ( !string.IsNullOrEmpty(m.PassengerName) && m.PassengerName.ToLower().Contains(searchValue)) ||
                        (!string.IsNullOrEmpty(m.name) && m.name.ToLower().Contains(searchValue)) 
                    ).ToList();
                }

                //total number of rows count     
                recordsTotal = models.Count();
                //Paging     
                var data = models.Skip(skip).Take(pageSize).ToList();
                //Returning Json Data    
                return Json(new { sEcho = sEcho, iTotalRecords = recordsTotal, iTotalDisplayRecords = recordsTotal, aaData = data });
            }
            catch (Exception e)
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();

                log.Error(e.Message, e);
            }
            return Json(new { sEcho = sEcho, iTotalRecords = recordsTotal, iTotalDisplayRecords = recordsTotal, aaData = new List<BookingModel>() });
        }

        public ActionResult ViewAgentsBooking(int id)
        {
            bool isError = false;
            List<BookingModel> models = new List<BookingModel>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from bookings where isDeleted = 0 and agentid='" + id + "'order by date desc LIMIT 15";
                dr = cmd.ExecuteReader();
                if (dr.Read() != true)
                {
                    dr.Close();
                }
                else
                {
                    while (dr.Read())
                    {
                        models.Add(new BookingModel
                        {
                            Reference = dr.GetString(4),
                            Date = this.ConvertDateForClient(dr.GetString(5)),
                            Tour = dr.GetInt32(6),
                            TourCode = dr.GetInt32(7),
                            pickuplocation = dr.GetInt32(8),
                            PassengerName = dr.GetString(10),
                            Adults = dr.GetInt32(11),
                            FamilyChildren = dr.GetInt32(12),
                            Infant = dr.GetInt32(13),
                            Children = dr.GetInt32(14),
                            Price = dr.GetInt32(15),
                            Discount = dr.GetInt32(16),
                            Commission = dr.GetFloat(17),
                            ContactDetails = dr.GetString(19),
                            Comments = dr.GetString(20)
                        });
                    }
                    dr.Close();
                }

            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View(models);
        }

        //Changes made on 2016-03-08
        public ActionResult ViewAdminsBooking(int id)
        {
            bool isError = false;
            List<BookingModel> models = new List<BookingModel>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from bookings where isDeleted = 0 and uid='" + id + "';";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    models.Add(new BookingModel
                    {
                        Reference = dr.GetString(4),
                        Date = this.ConvertDateForClient(dr.GetString(5)),
                        Tour = dr.GetInt32(6),
                        TourCode = dr.GetInt32(7),
                        pickuplocation = dr.GetInt32(8),
                        PassengerName = dr.GetString(10),
                        Adults = dr.GetInt32(11),
                        FamilyChildren = dr.GetInt32(12),
                        Infant = dr.GetInt32(13),
                        Children = dr.GetInt32(14),
                        Price = dr.GetInt32(15),
                        Discount = dr.GetInt32(16),
                        Commission = dr.GetFloat(17),
                        ContactDetails = dr.GetString(19),
                        Comments = dr.GetString(20)
                    });
                }
                dr.Close();

            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View(models);
        }

        public string setVoucherPermission(string id, bool check)
        {
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                if (check)
                    cmd.CommandText = "UPDATE users SET showvouchers = 1 where id = " + id;
                else
                    cmd.CommandText = "UPDATE users SET showvouchers = 0 where id = " + id;

                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            return "success";
        }
        public string setAddBookingPermission(string id, bool check)//added by yummi on 30-3-15 due to add new feild in db
        {
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                if (check)
                    cmd.CommandText = "UPDATE users SET showaddbooking = 1 where id = " + id;
                else
                    cmd.CommandText = "UPDATE users SET showaddbooking = 0 where id = " + id;

                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            return "success";
        }

        public JsonResult SearchBookings(string search)
        {
            List<BookingModel> models = new List<BookingModel>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                //cmd.CommandText = "Select * from bookings where isDeleted = 0;";
                cmd.CommandText = "Select id,IFNULL(voucher,'')voucher,date,passenger from bookings where  id like '%" + search + "%' or passenger like '%" + search + "%' or voucher like '%" + search + "%'  or date like '%" + search + "%'  and isDeleted = 0 and voucher is not null;";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    models.Add(new BookingModel
                    {
                        //BookingID = dr.GetInt32(0),
                        //Voucher = dr.IsDBNull(3) ? null : dr.GetString(3),
                        //Date = this.ConvertDateForClient(dr.GetString(5)),
                        //PassengerName = dr.GetString(10)
                        BookingID = dr.GetInt32(0),
                        Voucher = dr.IsDBNull(1) ? null : dr.GetString(1),
                        Date = this.ConvertDateForClient(dr.GetString(2)),
                        PassengerName = dr.GetString(3)
                    });
                }
                dr.Close();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }


            return Json(models);
        }

        public ActionResult AddLocations()
        {
            ViewBag.Status = "false";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddLocations(Location model)
        {
            if (ModelState.IsValid)
            {
                bool isError = false;
                try
                {
                    connection.Open();
                    string[] locations = model.pickuplocation.Split(',');
                    for (int i = 0; i < locations.Length; i++)
                    {
                        if (!locations[i].Trim().Equals(""))
                        {
                            cmd = connection.CreateCommand();
                            cmd.CommandText = "INSERT INTO locations(tourid,location) VALUES(@tourid,@location)";
                            cmd.Parameters.AddWithValue("@tourid", model.Tour);
                            cmd.Parameters.AddWithValue("@location", locations[i].Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception e)
                {
                    isError = true;
                    log.Error(e.Message, e);
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
                if (!isError)
                    ViewBag.Status = "true";
                else
                    ViewBag.Status = "error";
            }
            else
                ViewBag.Status = "false";

            return View();
        }


        public JsonResult getSeatsLimitforUpdateSeat(string date, int tourid)
        {
            if (string.IsNullOrEmpty(date) || string.IsNullOrWhiteSpace(date))
                return Json(0);
            if (date.Length < 8)
                return Json(0);
            string[] tempArr = date.Split('/');
            int tempDate;
            if (tempArr.Length < 3)
                return Json(0);
            if (!Int32.TryParse(tempArr[0], out tempDate) || !Int32.TryParse(tempArr[1], out tempDate) || !Int32.TryParse(tempArr[2], out tempDate))
                return Json(0);

            int limit = 0;
            //string[] formats = { "mm/dd/yyyy", "m/d/yyyy" };

            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };

            var dateTime = DateTime.ParseExact(date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");

            bool check = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from available_seats where date='" + dateTime + "' and tourid='" + tourid + "';";
                //cmd.CommandText = "SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'), ast.available Available, ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied, ast.Available - ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Remaining FROM available_seats_temp ast left join bookings b on ast.date = b.date and ast.tourid = b.tourid WHERE ast.tourid = '" + tourid + "' and ast.date = '" + dateTime + "' group by ast.date";
                dr = cmd.ExecuteReader();


                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        //check = true;
                        limit = dr.GetInt32("available");
                    }
                }
                else
                {
                    //if (!check)
                    //{
                    if (tourid == 1)
                        limit = 18;//16;
                    else
                        limit = 21;//24;
                    //}
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return Json(limit);
        }



        



        public JsonResult getSeatsLimit(string date, int tourid)
        {
            if (string.IsNullOrEmpty(date) || string.IsNullOrWhiteSpace(date))
                return Json(0);
            if (date.Length < 8)
                return Json(0);
            string[] tempArr = date.Split('/');
            int tempDate;
            if (tempArr.Length < 3)
                return Json(0);
            if (!Int32.TryParse(tempArr[0], out tempDate) || !Int32.TryParse(tempArr[1], out tempDate) || !Int32.TryParse(tempArr[2], out tempDate))
                return Json(0);

            int limit = 0;
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var dateTime = DateTime.ParseExact(date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");

            bool check = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from available_seats where date='" + dateTime + "' and tourid='" + tourid + "';";
                //cmd.CommandText = "SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'), ast.available Available, ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied, ast.Available - ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Remaining FROM available_seats_temp ast left join bookings b on ast.date = b.date and ast.tourid = b.tourid WHERE ast.tourid = '" + tourid + "' and ast.date = '" + dateTime + "' group by ast.date";
                dr = cmd.ExecuteReader();


                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        //check = true;
                        limit = dr.GetInt32("available");
                    }
                }
                else
                {
                    //if (!check)
                    //{
                    if (tourid == 1)
                        limit = 18;//16;
                    else
                        limit = 21;//24;
                    //}
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return Json(limit);
        }

        public ActionResult AddSeats()
        {
            ViewBag.Status = "false";
            return View();
        }

        public string getLimitedSeats(string dttime, string tour)
        {
            try
            {
                //string[] formats = { "mm/dd/yyyy", "m/d/yyyy" };
                string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
                var dateTime = DateTime.ParseExact(dttime, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
                string str = string.Empty;
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT (sum(adults)+ sum(familychildren) + sum(infant) + sum(children))total FROM bookings where date='" + dateTime + "' and tourid='" + tour + "'and isdeleted=0;";
                //cmd.CommandText = "SELECT (sum(adults)+ sum(children))total FROM bookings where date='" + dateTime + "' and tourid='" + tour + "'and isdeleted=0;";
                str = Convert.ToString(cmd.ExecuteScalar());
                connection.Close();
                return str;
            }
            catch (Exception ex)
            {
                return "0";
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddSeats(Seat seat)
        {

           int seats= getLastAvailSeatLimit(seat.Date.ToString(), seat.Tour);

            if (ModelState.IsValid)
            {


                //seats += seat.available;
                seats = seat.available;
                //string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
                //string[] formats = { "mm/dd/yyyy", "m/d/yyyy" };
                string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
                var dateTime = DateTime.ParseExact(seat.Date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
                bool isError = false, ifExists = false;
                try
                {
                    connection.Open();
                    cmd = connection.CreateCommand();
                    cmd.CommandText = "Select * from available_seats where date='" + dateTime + "' and tourid='" + seat.Tour + "';";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        ifExists = true;
                    }
                    dr.Close();
                    if (ifExists)
                    {
                        cmd.CommandText = "UPDATE available_seats SET available = '" + seats + "' where date='" + dateTime + "' and tourid='" + seat.Tour + "';";
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO available_seats(tourid,date,available) VALUES(@tourid,@date,@available)";
                        cmd.Parameters.AddWithValue("@tourid", seat.Tour);
                        cmd.Parameters.AddWithValue("@date", dateTime);
                        cmd.Parameters.AddWithValue("@available", seats);
                    }
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    isError = true;
                    log.Error(e.Message, e);
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
                if (!isError)
                    ViewBag.Status = "true";
                else
                    ViewBag.Status = "error";
            }
            else
                ViewBag.Status = "false";

            return View();
        }

        public ActionResult AddTime()
        {
            ViewBag.Status = "false";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTime(Location model)
        {
            if (ModelState.IsValid)
            {
                bool isError = false;
                try
                {
                    connection.Open();
                    string[] times = model.time.Split(',');
                    for (int i = 0; i < times.Length; i++)
                    {
                        if (!times[i].Trim().Equals(""))
                        {
                            cmd = connection.CreateCommand();
                            cmd.CommandText = "INSERT INTO times(locationid,pickupid,time) VALUES(@locationid,@pickupid,@time)";
                            cmd.Parameters.AddWithValue("@locationid", model.Tour);
                            cmd.Parameters.AddWithValue("@pickupid", Int32.Parse(model.pickuplocation));
                            cmd.Parameters.AddWithValue("@time", times[i].Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception e)
                {
                    isError = true;
                    log.Error(e.Message, e);
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
                if (!isError)
                    ViewBag.Status = "true";
                else
                    ViewBag.Status = "error";
            }
            else
                ViewBag.Status = "false";

            return View();
        }

        public ActionResult Reports()
        {
            ViewBag.Status = "false";
            return View();
        }

        public PdfResult Pdf()
        {
            // With no Model and default view name.  Pdf is always the default view name
            return new PdfResult();
        }
        public ActionResult ReportsMainfests(string Date, int Tour)
        {
            Seat seat = new Seat();
            seat.Date = Date;
            seat.Tour = Tour;
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var dateTime = DateTime.ParseExact(seat.Date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            string tourcode = string.Empty;
            int number = 0;
            float totalprice = 0;
            int kurandaId = 2; // because Kuranda tour ids start from 2 and we use this for printing all kuranda tours
            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                for (int i = 0; i < 4; i++) // 4 is length because total tours available for kuranda are 4 in table tournames
                {
                    if (seat.Tour == 300321) // 300321 means All Kuranda
                    {
                        // cmd.CommandText = "Select * from bookings JOIN locations ON bookings.pickuplocation = locations.id JOIN times ON bookings.time = times.timeid where bookings.isDeleted = 0  and date='" + dateTime + "' and bookings.tourid='" + kurandaId + "' order by CAST(times.time AS DECIMAL(10, 5)) asc;";
                        cmd.CommandText = "select B.id,B.passenger,B.adults,B.children,B.paymenttype,B.familychildren,B.infant,b.tourcode,B.totalprice,B.contact,B.comments,l.location,t.time,T.timeid,B.Fish,B.Steak,B.Vegetarian from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where B.isDeleted = 0  and B.date='" + dateTime + "' and B.tourid='" + kurandaId + "' order by CAST(T.time AS DECIMAL(10, 5)) asc;";
                        kurandaId++;
                    }
                    else
                        //cmd.CommandText = "Select * from bookings JOIN locations ON bookings.pickuplocation = locations.id JOIN times ON bookings.time = times.timeid where bookings.isDeleted = 0 and  date='" + dateTime + "' and bookings.tourid='" + seat.Tour + "' order by CAST(times.time AS DECIMAL(10, 5)) asc;";
                        cmd.CommandText = "select B.id,B.passenger,B.adults,B.children,B.paymenttype,B.familychildren,B.infant,b.tourcode,B.totalprice,B.contact,B.comments,l.location,t.time,T.timeid,B.Fish,B.Steak,B.Vegetarian from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where  B.isDeleted = 0 and  B.date='" + dateTime + "' and B.tourid='" + seat.Tour + "' order by CAST(T.time AS DECIMAL(10, 5)) asc;";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        tourcode = this.CheckTourCode(dr.GetInt32("tourcode"));
                        Int32.TryParse(dr.GetString("time"), out number);
                        if (dr.GetInt32("paymenttype") != 2)
                            totalprice = dr.GetFloat("totalprice");
                        else
                            totalprice = 0;


                        models.Add(new BookingModel
                        {
                            BookingID = dr.GetInt32("id"), //booking id is used as key for sorting on time attribute
                            time = dr.GetString("time"),
                            timeid = dr.GetInt32("timeid"),
                            location = dr.GetString("location"), //when add new column in booking table increment by one in location index only here
                            PassengerName = dr.GetString("passenger"),
                            Adults = dr.GetInt32("adults"),
                            Children = dr.GetInt32("children"),
                            //SelectedLunch = dr("lunch"),
                            Lunch = (dr.IsDBNull(dr.GetOrdinal("Fish")) ? "0" : dr.GetString("Fish")) + "," + (dr.IsDBNull(dr.GetOrdinal("Steak")) ? "0" : dr.GetString("Steak")) + "," + (dr.IsDBNull(dr.GetOrdinal("Vegetarian")) ? "0" : dr.GetString("Vegetarian")),
                            FamilyChildren = dr.GetInt32("familychildren"),
                            Infant = dr.GetInt32("infant"),
                            tourcodevalues = tourcode,
                            TotalPrice = totalprice,
                            ContactDetails = dr.GetString("contact"),
                            Comments = dr.GetString("comments")
                        });

                    }
                    dr.Close();

                    if (seat.Tour != 300321) break;
                }

                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();



                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
                //models = models.OrderBy(o => o.timeid).ToList();
                models = models.OrderBy(o => DateTime.ParseExact(Convert.ToDateTime(o.time).ToString("HH:mm"), "HH:mm", CultureInfo.InvariantCulture)).ToList();
                var pdf = new PdfResult(models, "CreateReport");
                pdf.ViewBag.Title = "Report";

                if (seat.Tour == 300321) pdf.ViewBag.TourName = "All Kuranda";
                if (seat.Tour == 1) pdf.ViewBag.TourName = "Cape Tribulation Manifest";
                else if (seat.Tour == 2) pdf.ViewBag.TourName = "Kuranda 8am";
                else if (seat.Tour == 3) pdf.ViewBag.TourName = "Kuranda 9am";
                else if (seat.Tour == 4) pdf.ViewBag.TourName = "Kuranda 10am";
                else if (seat.Tour == 5) pdf.ViewBag.TourName = "Kuranda 11am";

                pdf.ViewBag.Date = dateTime;
                pdf.ViewBag.Tour = seat.Tour;
                pdf.ViewBag.Invoice = invoice;

                return pdf;
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View("CreateReport");

        }

        //Changes made on 22-06-2016
        public ActionResult ReportsMain(string Date)
        {
            Seat seat = new Seat();
            seat.Date = Date;
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var dateTime = DateTime.ParseExact(seat.Date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            string tourcode = string.Empty;
            int number = 0;
            float totalprice = 0;
            int kurandaId = 2; // because Kuranda tour ids start from 2 and we use this for printing all kuranda tours
            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                for (int i = 0; i < 4; i++) // 4 is length because total tours available for kuranda are 4 in table tournames
                {
                    if (seat.Tour == 300321) // 300321 means All Kuranda
                    {
                        // cmd.CommandText = "Select * from bookings JOIN locations ON bookings.pickuplocation = locations.id JOIN times ON bookings.time = times.timeid where bookings.isDeleted = 0  and date='" + dateTime + "' and bookings.tourid='" + kurandaId + "' order by CAST(times.time AS DECIMAL(10, 5)) asc;";
                        cmd.CommandText = "select B.id,B.passenger,B.adults,B.children,B.paymenttype,B.familychildren,B.infant,b.tourcode,B.totalprice,B.contact,B.comments,l.location,t.time,T.timeid,B.Fish,B.Steak,B.Vegetarian from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where B.isDeleted = 0  and B.date='" + dateTime + " order by CAST(T.time AS DECIMAL(10, 5)) asc;";
                        kurandaId++;
                    }
                    else
                        //cmd.CommandText = "Select * from bookings JOIN locations ON bookings.pickuplocation = locations.id JOIN times ON bookings.time = times.timeid where bookings.isDeleted = 0 and  date='" + dateTime + "' and bookings.tourid='" + seat.Tour + "' order by CAST(times.time AS DECIMAL(10, 5)) asc;";
                        cmd.CommandText = "select B.id,B.passenger,B.adults,B.children,B.paymenttype,B.familychildren,B.infant,b.tourcode,B.totalprice,B.contact,B.comments,l.location,t.time,T.timeid,B.Fish,B.Steak,B.Vegetarian from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where  B.isDeleted = 0 and  B.date='" + dateTime + "'order by CAST(T.time AS DECIMAL(10, 5)) asc;";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        //  tourcode = this.CheckTourCode(dr.GetInt32("tourcode"));
                        Int32.TryParse(dr.GetString("time"), out number);
                        if (dr.GetInt32("paymenttype") != 2)
                            totalprice = dr.GetFloat("totalprice");
                        else
                            totalprice = 0;


                        models.Add(new BookingModel
                        {
                            BookingID = dr.GetInt32("id"), //booking id is used as key for sorting on time attribute
                            time = dr.GetString("time"),
                            timeid = dr.GetInt32("timeid"),
                            location = dr.GetString("location"), //when add new column in booking table increment by one in location index only here
                            PassengerName = dr.GetString("passenger"),
                            Adults = dr.GetInt32("adults"),
                            Children = dr.GetInt32("children"),
                            //SelectedLunch = dr("lunch"),
                            Lunch = (dr.IsDBNull(dr.GetOrdinal("Fish")) ? "0" : dr.GetString("Fish")) + "," + (dr.IsDBNull(dr.GetOrdinal("Steak")) ? "0" : dr.GetString("Steak")) + "," + (dr.IsDBNull(dr.GetOrdinal("Vegetarian")) ? "0" : dr.GetString("Vegetarian")),
                            FamilyChildren = dr.GetInt32("familychildren"),
                            Infant = dr.GetInt32("infant"),
                            tourcodevalues = tourcode,
                            TotalPrice = totalprice,
                            ContactDetails = dr.GetString("contact"),
                            Comments = dr.GetString("comments")
                        });

                    }
                    dr.Close();

                    if (seat.Tour != 300321) break;
                }

                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();



                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
                //models = models.OrderBy(o => o.timeid).ToList();
                models = models.OrderBy(o => DateTime.ParseExact(Convert.ToDateTime(o.time).ToString("HH:mm"), "HH:mm", CultureInfo.InvariantCulture)).ToList();
                var pdf = new PdfResult(models, "CreateReport");
                pdf.ViewBag.Title = "Report";

                if (seat.Tour == 300321) pdf.ViewBag.TourName = "All Kuranda";
                if (seat.Tour == 1) pdf.ViewBag.TourName = "Cape Tribulation Manifest";
                else if (seat.Tour == 2) pdf.ViewBag.TourName = "Kuranda 8am";
                else if (seat.Tour == 3) pdf.ViewBag.TourName = "Kuranda 9am";
                else if (seat.Tour == 4) pdf.ViewBag.TourName = "Kuranda 10am";
                else if (seat.Tour == 5) pdf.ViewBag.TourName = "Kuranda 11am";

                pdf.ViewBag.Date = dateTime;
                pdf.ViewBag.Tour = seat.Tour;
                pdf.ViewBag.Invoice = invoice;

                return pdf;
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View("CreateReport");

        }
        //End Changes

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reports(Seat seat)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var dateTime = DateTime.ParseExact(seat.Date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            string tourcode = string.Empty;
            int number = 0;
            float totalprice = 0;
            int kurandaId = 2; // because Kuranda tour ids start from 2 and we use this for printing all kuranda tours
            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                for (int i = 0; i < 4; i++) // 4 is length because total tours available for kuranda are 4 in table tournames
                {
                    if (seat.Tour == 300321) // 300321 means All Kuranda
                    {
                        // cmd.CommandText = "Select * from bookings JOIN locations ON bookings.pickuplocation = locations.id JOIN times ON bookings.time = times.timeid where bookings.isDeleted = 0  and date='" + dateTime + "' and bookings.tourid='" + kurandaId + "' order by CAST(times.time AS DECIMAL(10, 5)) asc;";
                        cmd.CommandText = "select B.id,B.passenger,B.adults,B.children,B.paymenttype,B.familychildren,B.infant,b.tourcode,B.totalprice,B.contact,B.comments,l.location,t.time,T.timeid,B.Fish,B.Steak,B.Vegetarian,B.POB,B.agentinvoice from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where B.isDeleted = 0  and B.date='" + dateTime + "' and B.tourid='" + kurandaId + "' order by CAST(T.time AS DECIMAL(10, 5)) asc;";
                        kurandaId++;
                    }
                    else
                        //cmd.CommandText = "Select * from bookings JOIN locations ON bookings.pickuplocation = locations.id JOIN times ON bookings.time = times.timeid where bookings.isDeleted = 0 and  date='" + dateTime + "' and bookings.tourid='" + seat.Tour + "' order by CAST(times.time AS DECIMAL(10, 5)) asc;";
                        cmd.CommandText = "select B.id,B.passenger,B.adults,B.children,B.paymenttype,B.familychildren,B.infant,b.tourcode,B.totalprice,B.contact,B.comments,l.location,t.time,T.timeid,B.Fish,B.Steak,B.Vegetarian,B.POB,B.agentinvoice from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where  B.isDeleted = 0 and  B.date='" + dateTime + "' and B.tourid='" + seat.Tour + "' order by CAST(T.time AS DECIMAL(10, 5)) asc;";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        tourcode = this.CheckTourCode(dr.GetInt32("tourcode"));
                        Int32.TryParse(dr.GetString("time"), out number);
                        if (dr.GetInt32("paymenttype") != 2)
                            totalprice = dr.GetFloat("totalprice");
                        else
                            totalprice = 0;


                        models.Add(new BookingModel
                        {
                            BookingID = dr.GetInt32("id"), //booking id is used as key for sorting on time attribute
                            time = dr.GetString("time"),
                            timeid = dr.GetInt32("timeid"),
                            location = dr.GetString("location"), //when add new column in booking table increment by one in location index only here
                            PassengerName = dr.GetString("passenger"),
                            Adults = dr.GetInt32("adults"),
                            Children = dr.GetInt32("children"),
                            FamilyChildren = dr.GetInt32("familychildren"),
                            Infant = dr.GetInt32("infant"),
                            // Lunch = dr.GetString("Fish")?? "0" + ", " + dr.GetString("Steak")?? "0"  + "," + dr.GetString("Vegetarian")?? "0",
                            Lunch = (dr.IsDBNull(dr.GetOrdinal("Fish")) ? "0" : dr.GetString("Fish")) + "," + (dr.IsDBNull(dr.GetOrdinal("Steak")) ? "0" : dr.GetString("Steak")) + "," + (dr.IsDBNull(dr.GetOrdinal("Vegetarian")) ? "0" : dr.GetString("Vegetarian")),
                            tourcodevalues = tourcode,
                            TotalPrice = totalprice,
                            POB = (dr.IsDBNull(dr.GetOrdinal("POB")) ? 0 : dr.GetFloat("POB")),
                            ContactDetails = dr.GetString("contact"),
                            Comments = dr.GetString("comments"),
                            InvoiceAgent = (dr.IsDBNull(dr.GetOrdinal("agentinvoice")) ? 0 : dr.GetFloat("agentinvoice"))

                        });

                    }
                    dr.Close();
                    models = models.OrderBy(o => DateTime.ParseExact(Convert.ToDateTime(o.time).ToString("HH:mm"), "HH:mm", CultureInfo.InvariantCulture)).ToList();
                    if (seat.Tour != 300321) break;

                }

                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();

                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
                //models = models.OrderBy(o => o.timeid).ToList();

                models = models.OrderBy(o => DateTime.ParseExact(Convert.ToDateTime(o.time).ToString("HH:mm"), "HH:mm", CultureInfo.InvariantCulture)).ToList();
                var pdf = new PdfResult(models, "CreateReport");
                pdf.ViewBag.Title = "Report";

                if (seat.Tour == 300321) pdf.ViewBag.TourName = "All Kuranda";
                if (seat.Tour == 1) pdf.ViewBag.TourName = "Cape Tribulation Manifest";
                else if (seat.Tour == 2) pdf.ViewBag.TourName = "Kuranda 8am";
                else if (seat.Tour == 3) pdf.ViewBag.TourName = "Kuranda 9am";
                else if (seat.Tour == 4) pdf.ViewBag.TourName = "Kuranda 10am";
                else if (seat.Tour == 5) pdf.ViewBag.TourName = "Kuranda 11am";

                pdf.ViewBag.Date = dateTime;
                pdf.ViewBag.Tour = seat.Tour;
                pdf.ViewBag.Invoice = invoice;

                return pdf;

                //Commented on 04-07-2016 by Krishna
                //StringBuilder sb = new StringBuilder();
                //sb.Append("<table width='100%' cellpadding='1.0' cellspacing='2.0' widths='3;5;8;2;2;2;2;2;2;3;3;5;5;2'>");
                //sb.Append("<row>");
                //sb.Append("<cell borderwidth='0.5' left='true' right='true' top='true' bottom='true'>");
                //sb.Append("<paragraph style='font-family:Helvetica; font - size:10; font - style:oblique;'>");
                //sb.Append("<chunk>");
                //sb.Append("");
                //sb.Append("</chunk>");
                //sb.Append("</paragraph></cell>");
                //sb.Append("<table width='100%' cellspacing='0' cellpadding='2'>");
                //sb.Append("<tr><td align='center' style='background-color: #18B5F0' colspan = '2'><b></b></td></tr>");
                //sb.Append("<tr><td colspan = '2'></td></tr>");
                //sb.Append("<tr><td><b>Order No:</b>");
                //sb.Append("1");
                //sb.Append("</td><td><b>Date: </b>");
                //sb.Append(DateTime.Now);
                //sb.Append(" </td></tr>");
                //sb.Append("<tr><td colspan = '2'><b>Company Name :</b> ");
                //sb.Append("Narola");
                //sb.Append("</td></tr>");
                //sb.Append("</table>");
                //StringReader sr = new StringReader(sb.ToString());

                //Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);
                //HTMLWorker htmlparser = new HTMLWorker(pdfDoc);
                //using (MemoryStream memoryStream = new MemoryStream())
                //{
                //    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);
                //    pdfDoc.Open();
                //    htmlparser.Parse(sr);
                //    pdfDoc.Close();
                //    byte[] bytes = memoryStream.ToArray();
                //    memoryStream.Close();

                //    MailMessage mail = new MailMessage();
                //    SmtpClient smtp = new SmtpClient();

                //    mail.To.Add("kgh.narola@narolainfotech.com");
                //    mail.From = new MailAddress("demo.narolainfotech@gmail.com");
                //    mail.Subject = "Report";
                //    mail.Body = "Please Find Attachement";
                //    mail.Attachments.Add(new Attachment(new MemoryStream(bytes), "iTextSharpPDF.pdf"));

                //    mail.IsBodyHtml = true;
                //    smtp.Host = "smtp.gmail.com";
                //    smtp.Port = 587;
                //    smtp.UseDefaultCredentials = false;
                //    smtp.Credentials = new System.Net.NetworkCredential
                //    ("demo.narolainfotech@gmail.com", "Narola102");// Enter senders User name and password  
                //    smtp.EnableSsl = true;
                //    smtp.Send(mail);
                //}

            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View("CreateReport");

        }

        [HttpPost]
        public ActionResult ReportsView(string Date, string Tour)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var dateTime = DateTime.ParseExact(Date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            bool isError = false;
            string tourcode = string.Empty;
            int number = 0;
            float totalprice = 0;
            int kurandaId = 2; // because Kuranda tour ids start from 2 and we use this for printing all kuranda tours

            try
            {
                List<BookingModel> models = new List<BookingModel>();
                connection.Open();
                cmd = connection.CreateCommand();
                for (int i = 0; i < 4; i++) // 4 is length because total tours available for kuranda are 4 in table tournames
                {
                    if (Convert.ToInt32(Tour) == 300321) // 300321 means All Kuranda
                    {
                        // cmd.CommandText = "Select * from bookings JOIN locations ON bookings.pickuplocation = locations.id JOIN times ON bookings.time = times.timeid where bookings.isDeleted = 0 and date='" + dateTime + "' and bookings.tourid >='" + kurandaId + "' order by CAST(times.time AS DECIMAL(10, 5)) asc;";
                        cmd.CommandText = "select B.id,B.passenger,B.adults,B.children,B.paymenttype,B.familychildren,B.infant,b.tourcode,B.totalprice,B.contact,B.comments,l.location,t.time,T.timeid,B.Fish,B.Steak,B.Vegetarian,B.isGoldClass, B.POB,B.agentinvoice from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where B.isDeleted = 0 and date='" + dateTime + "' and B.tourid='" + kurandaId + "' order by CAST(T.time AS DECIMAL(10, 5)) asc;";
                        kurandaId++;
                    }
                    else
                        //cmd.CommandText = "Select * from bookings JOIN locations ON bookings.pickuplocation = locations.id JOIN times ON bookings.time = times.timeid where bookings.isDeleted = 0 and date='" + dateTime + "' and bookings.tourid='" + Convert.ToInt32(Tour) + "' order by CAST(times.time AS DECIMAL(10, 5)) asc;";
                        cmd.CommandText = "select B.id,B.passenger,B.adults,B.children,B.paymenttype,B.familychildren,B.infant,b.tourcode,B.totalprice,B.contact,B.comments,l.location,t.time,T.timeid,B.Fish,B.Steak,B.Vegetarian,B.isGoldClass, B.POB,B.agentinvoice from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where B.isDeleted = 0 and date='" + dateTime + "' and B.tourid='" + Convert.ToInt32(Tour) + "' order by CAST(T.time AS DECIMAL(10, 5)) asc;";

                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        tourcode = this.CheckTourCode(dr.GetInt32("tourcode"));
                        Int32.TryParse(dr.GetString("time"), out number);
                        if (dr.GetInt32("paymenttype") != 2)
                            totalprice = dr.GetFloat("totalprice");
                        else
                            totalprice = 0;

                        models.Add(new BookingModel
                        {
                            BookingID = dr.GetInt32("id"), //booking id is used as key for sorting on time attribute
                            time = dr.GetString("time"),
                            timeid = dr.GetInt32("timeid"),
                            location = dr.GetString("location"), //when add new column in booking table increment by one in location index only here
                            PassengerName = dr.GetString("passenger"),
                            Adults = dr.GetInt32("adults"),
                            Children = dr.GetInt32("children"),
                            Lunch = (dr.IsDBNull(dr.GetOrdinal("Fish")) ? "0" : dr.GetString("Fish")) + "," + (dr.IsDBNull(dr.GetOrdinal("Steak")) ? "0" : dr.GetString("Steak")) + "," + (dr.IsDBNull(dr.GetOrdinal("Vegetarian")) ? "0" : dr.GetString("Vegetarian")),
                            FamilyChildren = dr.GetInt32("familychildren"),
                            Infant = dr.GetInt32("infant"),
                            tourcodevalues = tourcode,
                            TotalPrice = totalprice,
                            POB = (dr.IsDBNull(dr.GetOrdinal("POB")) ? 0 : dr.GetFloat("POB")),
                            InvoiceAgent = (dr.IsDBNull(dr.GetOrdinal("agentinvoice")) ? 0 : dr.GetFloat("agentinvoice")),
                            ContactDetails = dr.GetString("contact"),
                            Comments = dr.GetString("comments")

                        });

                    }
                    dr.Close();

                    if (Convert.ToInt32(Tour) != 300321) break;

                }

                cmd.CommandText = "Select * from invoicegenerator;";
                dr = cmd.ExecuteReader();
                int invoice = 0;
                while (dr.Read())
                {
                    invoice = dr.GetInt32(1);
                }
                dr.Close();
                cmd.CommandText = "UPDATE invoicegenerator SET invoice = '" + (invoice + 1) + "';";
                cmd.ExecuteNonQuery();

                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();

                //DateTime.ParseExact(time, "HH:mm:ss",
                //                        CultureInfo.InvariantCulture);
                //models = models.OrderBy(o => o.timeid).ToList();
                //appointmentMasterDate1 = dc.AppointmentMasters.ToList().Where(x => (x.AppointmentDate1 != null ? x.AppointmentDate1 == appointmentDate : false) && x.AppointmentDateId == Convert.ToInt32(1)
                //                && ((DateTime.ParseExact((Convert.ToString(x.StartHour).Length <= 1 ? "0" + Convert.ToString(x.StartHour) : Convert.ToString(x.StartHour))
                //                                    + ":" + ((Convert.ToString(x.StartMinute).Length <= 1 ? "0" + Convert.ToString(x.StartMinute) : Convert.ToString(x.StartMinute))),
                //                                        "HH:mm", CultureInfo.InvariantCulture)
                //                                    .Subtract(time_current24).Duration()) <= new TimeSpan(0, noOfHourMinute, 0))
                //                                    && x.LinkToStatusMasterId != null && !strArrStatusId1.Contains(x.LinkToStatusMasterId.Value.ToString())).OrderBy(x => x.DentrixCreatedDate).ToList();


                models = models.OrderBy(o => DateTime.ParseExact(Convert.ToDateTime(o.time).ToString("HH:mm"), "HH:mm", CultureInfo.InvariantCulture)).ToList();

                return View("CreateReportView", models);
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View("CreateReport");

        }

        public ActionResult CheckAvailability()
        {
            ViewBag.Status = "false";
            return View();
        }

        public JsonResult CheckSeatsAvailability(int tourid, string from, string to)
        {
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var FromDateTime = DateTime.ParseExact(from, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            string FromDate = FromDateTime.ToString();

            var ToDateTime = DateTime.ParseExact(to, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            string ToDate = ToDateTime.ToString();

            List<Seat> models = new List<Seat>();

            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from available_seats where tourid='" + tourid + "'  and date >='" + FromDate + "' AND date <='" + ToDate + "' ;";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    models.Add(new Seat { Date = this.ConvertDateForClient(dr.GetString(2)), available = Int32.Parse(dr.GetString(3)), ForSorting = Convert.ToDateTime(dr.GetString(2)) });
                }
                models.Sort(new Comparison<Seat>((x, y) => DateTime.Compare(x.ForSorting, y.ForSorting)));
                dr.Close();

            }
            catch (Exception e)
            {

                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return Json(models);
        }

        public ActionResult ViewLocations()
        {
            bool isError = false;
            try
            {
                List<Location> models = new List<Location>();
                connection.Open();
                cmd = connection.CreateCommand();
                //cmd.CommandText = "Select * from locations";
                cmd.CommandText = "SELECT L.id,(L.tourid)Tour,L.location,T.Tourname FROM locations L inner join tournames T on L.tourid = T.id ";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    models.Add(new Location
                    {
                        ID = dr.GetInt32(0),
                        Tour = dr.GetInt32(1),
                        pickuplocation = dr.GetString(2),
                        TourName = dr.GetString(3)
                    });
                }
                dr.Close();
                ViewBag.Status = "true";
                return View(models);
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";

            return View();
        }

        /*commented on 2015-04-19*/
        /*
        public ActionResult EditLocation(int id)
        {
            Location model = null;
            bool isError = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from locations where id = '" + id + "';";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    model = new Location
                    {
                        ID = id,
                        Tour = dr.GetInt32(1),
                        pickuplocation = dr.GetString(2)

                    };
                    ViewBag.Tour = dr.GetInt32(1);
                }
                dr.Close();

            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "ok";
            else
                ViewBag.Status = "error";

            //var repository = new BookingRepository();
            //ViewBag.TourList = repository.GetTourNames();

            return View(model);
        }


         [HttpPost]
         [ValidateAntiForgeryToken]
         public ActionResult EditLocation(Location model)
         {
             if (ModelState.IsValid)
             {
                 try
                 {
                     connection.Open();
                     cmd = connection.CreateCommand();
                     cmd.CommandText = "UPDATE locations SET tourid = '" + model.Tour + "',location='" + model.pickuplocation.Trim() + "' where id='" + model.ID + "';";
                     cmd.ExecuteNonQuery();

                 }
                 catch (Exception e)
                 {
                     ModelState.AddModelError("", e.Message);
                     log.Error(e.Message, e);
                 }
                 finally
                 {
                     if (connection.State == System.Data.ConnectionState.Open)
                         connection.Close();
                 }
                 ViewBag.Status = "true";
             }
             else
                 ViewBag.Status = "false";

             //return View();
             return RedirectToAction("ViewLocations");
         }

        */
        /*End*/

        public ActionResult EditLocation(int id)
        {
            LocationWiseTime model = null;
            bool isError = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from locations where id = '" + id + "';";
                dr = cmd.ExecuteReader();


                int Tour = 0;
                string pickupLocation = string.Empty;
                List<string> timeList = new List<string>();

                while (dr.Read())
                {
                    Tour = dr.GetInt32(1);
                    pickupLocation = dr.GetString(2);
                }
                //ViewBag.locationId = "abbcd";//id.ToString();
                ViewBag.Tour = dr.GetInt32(1);

                dr.Close();

                string[] ttime = null;
                timeList = getTimeList(id);
                if (timeList.Count > 0)
                {
                    ttime = new string[timeList.Count];
                    for (int i = 0; i < timeList.Count; i++)
                    {
                        ttime[i] = timeList[i];
                    }
                }
                else
                {
                    ttime = new string[1];
                    ttime[0] = " ";
                }

                ViewBag.TimeList = ttime;//new string[] { "a", "b", };

                model = new LocationWiseTime
                {
                    //ID = id,
                    //Tour = dr.GetInt32(1),
                    //pickuplocation = dr.GetString(2)
                    ID = id,
                    pickuplocation = pickupLocation,
                    Tour = Tour
                    //time = timeList
                };



            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "ok";
            else
                ViewBag.Status = "error";

            //var repository = new BookingRepository();
            //ViewBag.TourList = repository.GetTourNames();

            return View(model);
        }

        /*Edit Location and time method On 08-09-2015*/
        [HttpPost]
        public string EditLocationandTime(string Tour, string pickuplocation, string locationId, string strtime, string isEditTime)
        {
            bool isBool = true;
            if (!String.IsNullOrEmpty(Tour) && !String.IsNullOrEmpty(pickuplocation))
            {
                try
                {
                    connection.Open();
                    cmd = connection.CreateCommand();
                    //cmd.CommandText = "UPDATE locations SET tourid = '" + Tour + "',location='" + pickuplocation + "' where id='" + locationId + "';";
                    cmd.CommandText = "UPDATE locations SET tourid =@tourID,location= @location where id=@Id";
                    cmd.Parameters.AddWithValue("@tourID", Tour);
                    cmd.Parameters.AddWithValue("@location", pickuplocation);
                    cmd.Parameters.AddWithValue("@Id", locationId);
                    cmd.ExecuteNonQuery();

                    if (isEditTime == "true")
                    {
                        isBool = EditTimeforLocation(strtime, locationId, "32");
                        if (!isBool)
                        {
                            return "errortime";
                        }
                    }

                    return "success";
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                    log.Error(e.Message, e);
                    return "error";
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
            }

            else
            {
                return "Please complete the form.";
            }
        }

        //Start Edit Time on  2015-08-09
        public bool EditTimeforLocation(string strtime, string locationId, string pickupid)
        {

            bool isError = false;
            try
            {
                //Delete  on 2015-08-09
                cmd = new MySqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = "Delete from times where locationid=@locationid";
                cmd.Parameters.AddWithValue("@locationid", locationId);
                cmd.ExecuteNonQuery();

                //End Delete........
                //Insert...........
                string[] times = strtime.Split(',');
                for (int i = 0; i < times.Length; i++)
                {
                    if (!times[i].Trim().Equals(""))
                    {
                        cmd = new MySqlCommand();
                        cmd.Connection = connection;
                        cmd.CommandText = "INSERT INTO times(locationid,pickupid,time) VALUES(@locationid,@pickupid,@time)";
                        cmd.Parameters.AddWithValue("@locationid", locationId);
                        cmd.Parameters.AddWithValue("@pickupid", Int32.Parse(pickupid));
                        cmd.Parameters.AddWithValue("@time", times[i].Trim());
                        cmd.ExecuteNonQuery();

                    }
                }
                //End Insert..........
                return true;
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
                return false;
            }
        }
        //End

        public List<string> getTimeList(int id)
        {
            bool isError = false;
            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "select time  from times where locationid=" + id + "";
                dr = cmd.ExecuteReader();
                List<string> timelist = new List<string>();

                while (dr.Read())
                {

                    timelist.Add(dr.GetString("time"));
                }
                dr.Close();
                return timelist;

            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
                return null;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

        }

        public JsonResult DeleteLocation(int id)
        {
            bool isError = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Delete from locations where id='" + id + "';";
                cmd.ExecuteNonQuery();

            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return Json(!isError);
        }

        private string CheckTourCode(int tourId)
        {
            string tourcode = string.Empty;
            if (tourId == 1)
                tourcode = "CT1";
            else if (tourId == 2)
                tourcode = "TS10";
            else if (tourId == 5)
                tourcode = "S1";
            else if (tourId == 6)
                tourcode = "ST12";
            else if (tourId == 7)
                tourcode = "ST13";
            else if (tourId == 8)
                tourcode = "SS18";
            else if (tourId == 9)
                tourcode = "CT20";
            else if (tourId == 10)
                tourcode = "TS8";
            else if (tourId == 11)
                tourcode = "ST15";
            else if (tourId == 12)
                tourcode = "TS10B";
            else if (tourId == 13)
                tourcode = "TRS";
            else if (tourId == 14)
                tourcode = "TJ19";
            else if (tourId == 15)
                tourcode = "TT17";

            return tourcode;
        }

        private string ConvertDateForClient(string dbformat)
        {
            if (!string.IsNullOrEmpty(dbformat))
            {
                string month, day;
                DateTime date = Convert.ToDateTime(dbformat);
                month = date.Month < 10 ? "0" + date.Month : date.Month + "";
                day = date.Day < 10 ? "0" + date.Day : date.Day + "";

                return string.Format("{0}/{1}/{2}", day, month, date.Year);
            }
            return DBNull.Value.ToString();
        }
        public ActionResult Bookings(int id = 0)
        {
            BookingModel booking = new BookingModel();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = " SELECT b.id,b.uid,b.agent,b.voucher,b.reference,b.date,b.tourid,b.tourcode,b.pickuplocation,ti.time,b.passenger,b.adults,b.familychildren,b.infant,b.children,b.price,b.discount,b.commission,b.totalprice,b.contact,b.comments,b.paymenttype,b.agentid,b.confirmationnumber,b.voucherid,b.shopid,b.salesfrom,b.custo_paymenttype,b.createdDate,b.isDeleted,isGoldClass,b.PaymentMethod,b.saleprice,b.lunch,b.Fish,b.Steak,b.Vegetarian,b.cardPaid,b.CashPaid,b.POB,b.agentInvoice ,l.location,t.tourname,tc.tourcodevalues, ti.time FROM bookings as b JOIN times as ti on b.time = ti.timeid JOIN locations as l on b.pickuplocation = l.id JOIN tournames as t on b.tourid = t.id JOIN tourcode as tc on b.tourcode = tc.id WHERE b.isDeleted = 0 and b.id =" + id.ToString();
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                booking = new BookingModel
                {
                    BookingID = dr.GetInt32("id"),
                    Agent = dr.GetString("agent"),
                    Voucher = dr.IsDBNull(3) ? null : dr.GetString("voucher"),
                    Reference = Convert.ToString(dr["reference"]),
                    Date = this.ConvertDateForClient(dr.GetString("date")),
                    tid = dr.GetInt32("tourid"),
                    tc = dr.GetInt32("tourcode"),
                    pl = dr.GetInt32("pickuplocation"),
                    time = dr.GetString("time"),
                    PassengerName = dr.GetString("passenger"),
                    Adults = dr.GetInt32("adults"),
                    FamilyChildren = dr.GetInt32("familychildren"),
                    Infant = dr.GetInt32("infant"),
                    Children = dr.GetInt32("children"),
                    Price = dr.GetInt32("price"),
                    Discount = dr.GetInt32("discount"),
                    Commission = dr.GetFloat("commission"),
                    ///TotalPrice = dr.GetFloat("totalprice"),
                    //TotalPrice = Math.Round(dr.GetDouble("totalprice"), 2),
                    TotalPrice = (dr.GetFloat("totalprice")),

                    ContactDetails = dr.GetString("contact"),
                    Comments = dr.GetString("comments"),
                    tourcodevalues = dr.GetString("tourcodevalues"),
                    tourname = dr.GetString("tourname"),
                    PaymentType = dr.GetInt32("paymenttype"),
                    AgentId = dr.GetInt32("agentid"),
                    location = dr.GetString("location"),
                    Fish = (dr["Fish"] == DBNull.Value ? 0 : dr.GetInt32("Fish")),
                    Steak = (dr["Steak"] == DBNull.Value ? 0 : dr.GetInt32("Steak")),
                    Vegetarian = (dr["Vegetarian"] == DBNull.Value ? 0 : dr.GetInt32("Vegetarian")),
                    CashPaid = (dr["CashPaid"] == DBNull.Value ? 0 : dr.GetFloat("CashPaid")),
                    CardPaid = (dr["CardPaid"] == DBNull.Value ? 0 : dr.GetFloat("CardPaid")),
                    POB = (dr["POB"] == DBNull.Value ? 0 : dr.GetFloat("POB")),
                    InvoiceAgent = (dr["agentInvoice"] == DBNull.Value ? 0 : dr.GetFloat("agentInvoice"))

                };
            }
            dr.Close();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            return View(booking);
        }

        public void SendAgentBookingEmailToTNTQ(int id = 0)
        {
            BookingModel booking = new BookingModel();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = " SELECT b.id,b.uid,b.agent,b.voucher,b.reference,b.date,b.tourid,b.tourcode,b.pickuplocation,ti.time,b.passenger,b.adults,b.familychildren,b.infant,b.children,b.price,b.discount,b.commission,b.totalprice,b.contact,b.comments,b.paymenttype,b.agentid,b.confirmationnumber,b.voucherid,b.shopid,b.salesfrom,b.custo_paymenttype,b.createdDate,b.isDeleted,isGoldClass,b.PaymentMethod,b.saleprice,b.lunch,b.Fish,b.Steak,b.Vegetarian,b.cardPaid,b.CashPaid,b.POB,b.agentInvoice ,l.location,t.tourname,tc.tourcodevalues, ti.time FROM bookings as b JOIN times as ti on b.time = ti.timeid JOIN locations as l on b.pickuplocation = l.id JOIN tournames as t on b.tourid = t.id JOIN tourcode as tc on b.tourcode = tc.id WHERE b.isDeleted = 0 and b.id =" + id.ToString();
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                booking = new BookingModel
                {
                    BookingID = dr.GetInt32("id"),
                    Agent = dr.GetString("agent"),
                    Voucher = dr.IsDBNull(3) ? null : dr.GetString("voucher"),
                    Reference = Convert.ToString(dr["reference"]),
                    Date = this.ConvertDateForClient(dr.GetString("date")),
                    tid = dr.GetInt32("tourid"),
                    tc = dr.GetInt32("tourcode"),
                    pl = dr.GetInt32("pickuplocation"),
                    time = dr.GetString("time"),
                    PassengerName = dr.GetString("passenger"),
                    Adults = dr.GetInt32("adults"),
                    FamilyChildren = dr.GetInt32("familychildren"),
                    Infant = dr.GetInt32("infant"),
                    Children = dr.GetInt32("children"),
                    Price = dr.GetInt32("price"),
                    Discount = dr.GetInt32("discount"),
                    Commission = dr.GetFloat("commission"),
                    ///TotalPrice = dr.GetFloat("totalprice"),
                    //TotalPrice = Math.Round(dr.GetDouble("totalprice"), 2),
                    TotalPrice = (dr.GetFloat("totalprice")),

                    ContactDetails = dr.GetString("contact"),
                    Comments = dr.GetString("comments"),
                    tourcodevalues = dr.GetString("tourcodevalues"),
                    tourname = dr.GetString("tourname"),
                    PaymentType = dr.GetInt32("paymenttype"),
                    AgentId = dr.GetInt32("agentid"),
                    location = dr.GetString("location"),
                    Fish = (dr["Fish"] == DBNull.Value ? 0 : dr.GetInt32("Fish")),
                    Steak = (dr["Steak"] == DBNull.Value ? 0 : dr.GetInt32("Steak")),
                    Vegetarian = (dr["Vegetarian"] == DBNull.Value ? 0 : dr.GetInt32("Vegetarian")),
                    CashPaid = (dr["CashPaid"] == DBNull.Value ? 0 : dr.GetFloat("CashPaid")),
                    CardPaid = (dr["CardPaid"] == DBNull.Value ? 0 : dr.GetFloat("CardPaid")),
                    POB = (dr["POB"] == DBNull.Value ? 0 : dr.GetFloat("POB")),
                    InvoiceAgent = (dr["agentInvoice"] == DBNull.Value ? 0 : dr.GetFloat("agentInvoice"))

                };
            }
            dr.Close();

            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();

            String bookinghtml = MvcHelpers.RenderViewToString(this.ControllerContext, "~/Views/Booking/BookingEmails.cshtml", booking);
            //StringReader sr = new StringReader(bookinghtml.ToString());
            //byte[] bytes;
            //Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);
            //HTMLWorker htmlparser = new HTMLWorker(pdfDoc);
            //using (MemoryStream memoryStream = new MemoryStream())
            //{
            //    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);
            //    pdfDoc.Open();
            //
            //    htmlparser.Parse(sr);
            //    pdfDoc.Close();
            //
            //    bytes = memoryStream.ToArray();
            //    memoryStream.Close();
            //}
            //Stream stream = new MemoryStream(bytes);
            //bool isError = EmailHelper.SendEmail("treknorth.com.au@gmail.com", "info@treknorth.com.au", "Agent - " + booking.Agent + " made booking - " + booking.BookingID, bookinghtml, stream, "AgentBooking_" + booking.BookingID.ToString() + ".pdf", true);

            bool isError = EmailHelper.SendEmail("treknorth.com.au@gmail.com", "info@treknorth.com.au", "Agent - " + booking.Agent + " made booking - " + booking.BookingID, bookinghtml, null, "", true);
        }

        [HttpPost]
        public JsonResult SendBookingEmail(string HtmlData, string Email)
        {
            bool isError = false;
            try
            {
                isError = EmailHelper.SendEmail("treknorth.com.au@gmail.com", Email, "Booking Details", HtmlData, null, "", true);
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }

            return Json(ViewBag.Status);
        }


        public ActionResult Availability()
        {
            AvailabilityModel availabilityModel = new AvailabilityModel();
            List<AvailabilityModel> list = new List<AvailabilityModel>();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT seats.date, seats.available, tn.tourname FROM `available_seats` as seats JOIN tournames as tn on seats.tourid = tn.id WHERE 1 ORDER BY seats.date ASC;";
            dr = cmd.ExecuteReader();
            availabilityModel.Dates = new List<DateTime>();
            availabilityModel.Seats = new List<int>();
            availabilityModel.Names = new List<string>();
            while (dr.Read())
            {
                availabilityModel.Dates.Add(dr.GetDateTime(0));
                availabilityModel.Seats.Add(dr.GetInt32(1));
                availabilityModel.Names.Add(dr.GetString(2));
                list.Add(new AvailabilityModel
                {
                    Date = dr.GetDateTime(0),
                    Seat = dr.GetInt32(1),
                    TourName = dr.GetString(2)
                });
            }
            dr.Close();
            cmd.CommandText = "SELECT DISTINCT tn.tourname FROM `available_seats` as seats JOIN tournames as tn on seats.tourid = tn.id WHERE 1;";
            dr = cmd.ExecuteReader();
            availabilityModel.TourNames = new List<string>();
            while (dr.Read())
            {
                availabilityModel.TourNames.Add(dr.GetString(0));
            }
            dr.Close();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            ViewBag.Seats = availabilityModel.Seats;
            ViewBag.Dates = availabilityModel.Dates.Distinct().ToList();
            ViewBag.Names = availabilityModel.TourNames;
            ViewBag.TourNames = availabilityModel.Names;
            return View(list);
        }

        public ActionResult AvailabilityByDate(string id)
        {
            AvailabilityModel availabilityModel = new AvailabilityModel();
            List<AvailabilityModel> list = new List<AvailabilityModel>();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT seats.date, seats.available, tn.tourname FROM `available_seats` as seats JOIN tournames as tn on seats.tourid = tn.id WHERE seats.date >  '" + id.Split('-')[2] + "-" + id.Split('-')[1] + "-" + id.Split('-')[0] + "' ORDER BY seats.date ASC;";
            dr = cmd.ExecuteReader();
            availabilityModel.Dates = new List<DateTime>();
            availabilityModel.Seats = new List<int>();
            availabilityModel.Names = new List<string>();
            while (dr.Read())
            {
                availabilityModel.Dates.Add(dr.GetDateTime(0));
                availabilityModel.Seats.Add(dr.GetInt32(1));
                availabilityModel.Names.Add(dr.GetString(2));
                list.Add(new AvailabilityModel
                {
                    Date = dr.GetDateTime(0),
                    Seat = dr.GetInt32(1),
                    TourName = dr.GetString(2)
                });
            }
            dr.Close();
            cmd.CommandText = "SELECT DISTINCT tn.tourname FROM `available_seats` as seats JOIN tournames as tn on seats.tourid = tn.id WHERE 1;";
            dr = cmd.ExecuteReader();
            availabilityModel.TourNames = new List<string>();
            while (dr.Read())
            {
                availabilityModel.TourNames.Add(dr.GetString(0));
            }
            dr.Close();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            ViewBag.Seats = availabilityModel.Seats;
            ViewBag.Dates = availabilityModel.Dates.Distinct().ToList();
            ViewBag.Names = availabilityModel.TourNames;
            ViewBag.TourNames = availabilityModel.Names;
            return View("Availability", list);
        }

        public ActionResult TopAgents()
        {
            return View("ViewTopAgentsReports");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ViewTopAgentsReports(RegisterModel model)
        {
            DateTime temp = new DateTime();

            DateTime tempFromDate = new DateTime();
            DateTime tempToDate = new DateTime();

            string[] FromDate1 = model.FromDate.Split('/');
            tempFromDate = Convert.ToDateTime((FromDate1[2] + "-" + FromDate1[1] + "-" + FromDate1[0]).ToString());
            tempToDate = Convert.ToDateTime((model.ToDate.Split('/')[2] + "-" + model.ToDate.Split('/')[1] + "/" + model.ToDate.Split('/')[0]).ToString());

            //string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };

            connection.Open();
            cmd = connection.CreateCommand();
            //cmd.CommandText = "SELECT u.name, SUM(ab.tour_money) as Total, SUM(ab.adult) + SUM(ab.children) + SUM(ab.familychildren) + SUM(ab.infant) as Pax FROM `agents_bookings` as ab JOIN users as u on ab.agent_id = u.id where 1=1 ";
            cmd.CommandText = "SELECT u.name, SUM(ab.tour_money) as Total, SUM(ab.adult) + SUM(ab.children) + SUM(ab.familychildren) + SUM(ab.infant) as Pax FROM `agents_bookings` as ab inner JOIN users as u on ab.agent_id = u.id inner join bookings as b on b.voucherid=ab.voucherid where 1=1 and b.isdeleted=0 ";

            //if (!string.IsNullOrEmpty(model.FromDate) && DateTime.TryParse(model.FromDate, out temp))
            //    cmd.CommandText += "  and ab.date >='" + DateTime.ParseExact(model.FromDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd") + "'";
            //if (!string.IsNullOrEmpty(model.ToDate) && DateTime.TryParse(model.ToDate, out temp))
            //    cmd.CommandText += "  and ab.date <='" + DateTime.ParseExact(model.ToDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd") + "'";

            if (!string.IsNullOrEmpty(model.FromDate))
                cmd.CommandText += "  and ab.date >='" + tempFromDate.Year + "-" + tempFromDate.Month + "-" + tempFromDate.Day + "'";
            if (!string.IsNullOrEmpty(model.ToDate))
                cmd.CommandText += "  and ab.date <='" + tempToDate.Year + "-" + tempToDate.Month + "-" + tempToDate.Day + "'";

            cmd.CommandText += " group by agent_id order by Total desc";
            dr = cmd.ExecuteReader();
            List<string> Agent = new List<string>();
            List<decimal> Amount = new List<decimal>();
            List<int> Pax = new List<int>();
            while (dr.Read())
            {
                Agent.Add(dr.GetString(0));
                Amount.Add(dr.GetDecimal(1));
                Pax.Add(dr.GetInt32(2));
            }
            dr.Close();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            var pdf = new PdfResult(null, "TopAgents");
            pdf.ViewBag.Agent = Agent;
            pdf.ViewBag.Amount = Amount;
            pdf.ViewBag.Pax = Pax;
            return pdf;
        }

        [HttpPost]
        public ActionResult ViewTopAgentsReportsView(string FromDate, string ToDate)
        {
            DateTime temp = new DateTime();
            DateTime tempFromDate = new DateTime();
            DateTime tempToDate = new DateTime();

            string[] FromDate1 = FromDate.Split('/');
            tempFromDate = Convert.ToDateTime((FromDate1[2] + "-" + FromDate1[1] + "-" + FromDate1[0]).ToString());
            tempToDate = Convert.ToDateTime((ToDate.Split('/')[2] + "-" + ToDate.Split('/')[1] + "/" + ToDate.Split('/')[0]).ToString());

            //string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            // string[] formats = { "mm/dd/yyyy", "m/d/yyyy" };
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT u.name, SUM(ab.tour_money) as Total, SUM(ab.adult) + SUM(ab.children) + SUM(ab.familychildren) + SUM(ab.infant) as Pax FROM `agents_bookings` as ab inner JOIN users as u on ab.agent_id = u.id inner join bookings as b on b.voucherid=ab.voucherid where 1=1 and b.isdeleted=0 ";
            // cmd.CommandText = "SELECT u.name, SUM(ab.tour_money) as Total, SUM(ab.adult) + SUM(ab.children) + SUM(ab.familychildren) + SUM(ab.infant) as Pax FROM `agents_bookings` as ab JOIN users as u on ab.agent_id = u.id where 1=1 ";


            //if (!string.IsNullOrEmpty(FromDate) && DateTime.TryParse(FromDate, out temp))
            //    cmd.CommandText += "  and ab.date >='" + DateTime.ParseExact(FromDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd") + "'";
            //if (!string.IsNullOrEmpty(ToDate) && DateTime.TryParse(ToDate, out temp))
            //    cmd.CommandText += "  and ab.date <='" + DateTime.ParseExact(ToDate, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd") + "'";


            if (!string.IsNullOrEmpty(FromDate))
                cmd.CommandText += "  and ab.date >='" + tempFromDate.Year + "-" + tempFromDate.Month + "-" + tempFromDate.Day + "'";
            if (!string.IsNullOrEmpty(ToDate))
                cmd.CommandText += "  and ab.date <='" + tempToDate.Year + "-" + tempToDate.Month + "-" + tempToDate.Day + "'";

            cmd.CommandText += " group by agent_id order by Total desc";
            dr = cmd.ExecuteReader();
            List<string> Agent = new List<string>();
            List<decimal> Amount = new List<decimal>();
            List<int> Pax = new List<int>();
            while (dr.Read())
            {
                Agent.Add(dr.GetString(0));
                Amount.Add(dr.GetDecimal(1));
                Pax.Add(dr.GetInt32(2));
            }
            dr.Close();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            ViewBag.Agent = Agent;
            ViewBag.Amount = Amount;
            ViewBag.Pax = Pax;
            return View("TopAgentsView");
        }

        public List<BookingModel> getAllTourCodes()
        {
            List<BookingModel> models = new List<BookingModel>();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "Select * from tourcode where tourid;";
            dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                models.Add(new BookingModel { TourCode = dr.GetInt32(0), tourcodevalues = dr.GetString(2) });
            }
            dr.Close();
            connection.Close();
            return models;

        }

        public List<BookingModel> getAllLocations()
        {
            List<BookingModel> models = new List<BookingModel>();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "Select * from locations;";

            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                models.Add(new BookingModel { pickuplocation = dr.GetInt32(0), location = dr.GetString(2) });
            }
            dr.Close();
            connection.Close();
            return models;

        }

        //public JsonResult getTimeList()
        //{
        //    var repository = new BookingRepository();
        //    return Json(repository.GetTimes(646));
        //}

        public List<Location> getAllTimes()
        {
            List<Location> models = new List<Location>();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "Select * from times;";
            dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                models.Add(new Location { Tour = dr.GetInt32(0), time = dr.GetString(3) });
            }

            return models;

        }
        public List<BookingModel> getAllTourNames()
        {
            List<BookingModel> models = new List<BookingModel>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from tournames;";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    models.Add(new BookingModel { Tour = dr.GetInt32(0), tourname = dr.GetString(1) });
                }
                dr.Close();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);

            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return models;

        }

        public ActionResult AddBookingB()
        {
            //getTimess(1073);
            var repository = new BookingRepository();
            
            ViewBag.Users = PrepareUsersDropDown(repository.GetUserNamesAndComments());
            ViewBag.Tours = PrepareToursDropDown(repository.GetTourNames());
            ViewBag.TourCodes = PrepareTourCodesDropDown(repository.GetTourCodes(1));
            ViewBag.PickupLocations = PreparePickupLocationDropDown(repository.GetPickupLocation(1));
            ViewBag.PickupTimes = PrepareTimesDropDown(repository.GetTimes(-1));
            ViewBag.EmptyList = PrepareEmptyList();

            string[] user = User.Identity.Name.Split(','); //userid,username,usertype
            ViewBag.UserId = Int32.Parse(user[0]);
            ViewBag.UserType = Int32.Parse(user[2]);
            ViewBag.User = string.Empty;
            ViewBag.Username = user[1].ToString();

            RegisterModel usr = repository.GetUser(Int32.Parse(user[0]));
            ViewBag.AgentNotes = usr.Comments;

            //string[] user = User.Identity.Name.Split(','); //userid,username,usertype
            //UserId = Int32.Parse(user[0]);
            //UserType = Int32.Parse(user[2]);
            ViewBag.DisableTourPriceForStaff = false;
            if (UserType == (int)USERTYPE.AGENT)
            {
                ViewBag.User = "agent";
                ViewBag.Commission = repository.GetCommission(UserId);
            }
            if (ViewBag.UserType == (int)USERTYPE.STAFF)
            {
                ViewBag.DisableTourPriceForStaff = true;
            }
            ViewBag.Credit = repository.GetCreditStatus(Int32.Parse(user[0]));


            connection.Open();
            cmd = connection.CreateCommand();
            #region get max voucherId
            string voucheridmax = string.Empty;
            cmd.CommandText = "SELECT MAX(CAST((voucherid) AS UNSIGNED))FROM bookings where bookings.isDeleted = 0;";  //on 2015-06-26
            //cmd.CommandText = "Select MAX(voucherId) from bookings where bookings.isDeleted = 0;";
            voucheridmax = cmd.ExecuteScalar().ToString();
            if (!String.IsNullOrEmpty(voucheridmax))
            {
                //voucheridmax = "TNTwork" + (Convert.ToInt32(voucheridmax.Substring(7)) + 1).ToString("D5");
                voucheridmax = (Convert.ToInt32(voucheridmax) + 1).ToString("D5");
            }
            else
            {
                voucheridmax = "00001";
            }
            TempData["Maxvoucherid"] = voucheridmax;
            ViewBag.voucherId = voucheridmax; //on 26-06-2015
            #endregion


            #region Dropdown for shopname commented by yummy
            //cmd.CommandText = "Select * from shop;";
            //MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
            //DataSet ds = new DataSet();
            //adp.Fill(ds);
            //List<SelectListItem> selectListItems = new List<SelectListItem>();
            //SelectListItem item = new SelectListItem();

            //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            //{
            //    item = new SelectListItem();
            //    item.Value = ds.Tables[0].Rows[i]["shopId"].ToString();
            //    item.Text = ds.Tables[0].Rows[i]["shopName"].ToString();
            //    selectListItems.Add(item);
            //}

            //TempData["Mshops"] = selectListItems;



            #endregion
            TempData.Keep();
            BookingModel _objBookingModel = new BookingModel();

            //yummy//
            //setup a view model
            var selectedFruits = new List<lunch>();
            _objBookingModel.AvailableLunch = GetAll().ToList();
            _objBookingModel.SelectedLunch = selectedFruits;
            //END yummy//



            return View(_objBookingModel);
        }
        public static IEnumerable<lunch> GetAll()
        {

            return new List<lunch> {
                              new lunch {Name = "Fish", Id = 1 },
                              new lunch {Name = "Steak", Id = 2},
                              new lunch {Name = "Vegetarian", Id = 3}
                            };


        }
        [HttpPost]
        public string AddBookingB(BookingModel model)
        {
            // Added by Suresh on 22-3-2016 for Resolving VoucherID = 0 insertation on Agent_Booking  table on second save
            connection.Open();
            cmd = connection.CreateCommand();
            #region get max voucherId
            string voucheridmax = string.Empty;
            cmd.CommandText = "SELECT MAX(CAST((voucherid) AS UNSIGNED))FROM bookings where bookings.isDeleted = 0;";  //on 2015-06-26
            //cmd.CommandText = "Select MAX(voucherId) from bookings where bookings.isDeleted = 0;";
            voucheridmax = cmd.ExecuteScalar().ToString();
            if (!String.IsNullOrEmpty(voucheridmax))
            {
                //voucheridmax = "TNTwork" + (Convert.ToInt32(voucheridmax.Substring(7)) + 1).ToString("D5");
                voucheridmax = (Convert.ToInt32(voucheridmax) + 1).ToString("D5");
            }
            else
            {
                voucheridmax = "00001";
            }
            #endregion

            if (model.Tour != 1)
            {
                model.Fish = 0;
                model.Steak = 0;
                model.Vegetarian = 0;
            }

            int id = 0;
            string error = "";

            string[] user = User.Identity.Name.Split(','); //userid,username,usertype
            UserId = Int32.Parse(user[0]);
            UserType = Int32.Parse(user[2]);

            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            bool check = false, isError = false;
            int commission = 0, creditStatus = -1, limit = 0;

            if (UserType == (int)USERTYPE.AGENT)
            {
                ModelState.Remove("Voucher");
                ModelState.Remove("Agentid");
            }
                

            ModelState.Remove("Discount");
            ModelState.Remove("ContactDetails");
            ModelState.Remove("Comments");
            ModelState.Remove("Children");
            ModelState.Remove("FamilyChildren");
            ModelState.Remove("Infant");
            ModelState.Remove("tc");

            if (ModelState.IsValid)
            {
                try
                {
                    var dateTime = DateTime.ParseExact(model.Date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");

                    //connection.Open();
                    //cmd = connection.CreateCommand();

                    cmd.CommandText = "Select * from available_seats where date='" + dateTime + "' and tourid='" + model.Tour + "';";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        check = true;
                        limit = dr.GetInt32(3);
                    }
                    dr.Close();
                    if (!check)
                    {
                        if (model.Tour == (int)TOUR.CAPE_TRIBULATION)
                            limit = 18; // seats limit per day only for cape tribulation tour
                        else
                            limit = 21; // seats limit per day for kuranda tours 

                        cmd.CommandText = "INSERT INTO available_seats(tourid,date,available) VALUES(@tourid,@date,@available)";
                        cmd.Parameters.AddWithValue("@tourid", model.Tour);
                        cmd.Parameters.AddWithValue("@date", dateTime);
                        cmd.Parameters.AddWithValue("@available", limit);
                        cmd.ExecuteNonQuery();

                    }

                    if (limit < model.Adults + model.FamilyChildren + model.Children + model.Infant || limit == 0)
                    {
                        //ModelState.AddModelError("", "Seats are not enough for this booking.");
                        ViewBag.Status = "seatserror";
                        ViewBag.Commission = model.Commission;
                        if (UserType == (int)USERTYPE.AGENT)
                        {
                            ViewBag.User = "agent";
                        }
                        else
                        {
                            ViewBag.User = string.Empty;
                        }
                        return "0|Seats are not enough for this booking.|";
                    }
                    else
                    {
                        if (UserType == (int)USERTYPE.ADMIN || UserType == (int)USERTYPE.STAFF)
                        {
                            cmd.CommandText = "Select * from users where id='" + model.AgentId + "';";
                        }
                        else
                            cmd.CommandText = "Select * from users where id='" + UserId + "';";

                        string userName = "";
                        dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            commission = dr.GetInt32(5);
                            creditStatus = dr.GetInt32(6);
                            userName = dr.GetString(1);
                            if (UserType == (int)USERTYPE.AGENT)
                                model.PaymentType = dr.GetInt32(10);
                        }
                        dr.Close();

                        cmd = connection.CreateCommand();

                        if (model.dateReceived != null)
                        {
                            cmd.CommandText = "INSERT INTO bookings(uid,bookinguserid,agent,voucher,reference,date,tourid,tourcode,pickuplocation,time,passenger,adults,familychildren,children,infant,price,discount,commission,totalprice,contact,comments,paymenttype,agentid,confirmationnumber,voucherId,custo_paymenttype,createdDate,isGoldClass,PaymentMethod,saleprice,Fish,Steak,Vegetarian,CashPaid,CardPaid,agentinvoice,POB,dateReceived,staff) VALUES(@uid,@bookinguserid,@agent,@voucher,@reference,@date,@tourid,@tourcode,@pickuplocation,@time,@passenger,@adults,@familychildren,@children,@infant,@price,@discount,@commission,@totalprice,@contact,@comments,@paymenttype,@agentid,@confirmationnumber,@voucherId,@custo_paymenttype,@createdDate,@isGoldClass,@PaymentMethod,@saleprice, @Fish,@Steak,@Vegetarian,@CashPaid,@CardPaid,@InvoiceAgent,@POB,@dateReceived,@staff)";
                        }
                        else
                        {
                            cmd.CommandText = "INSERT INTO bookings(uid,bookinguserid,agent,voucher,reference,date,tourid,tourcode,pickuplocation,time,passenger,adults,familychildren,children,infant,price,discount,commission,totalprice,contact,comments,paymenttype,agentid,confirmationnumber,voucherId,custo_paymenttype,createdDate,isGoldClass,PaymentMethod,saleprice,Fish,Steak,Vegetarian,CashPaid,CardPaid,agentinvoice,POB,staff) VALUES(@uid,@bookinguserid,@agent,@voucher,@reference,@date,@tourid,@tourcode,@pickuplocation,@time,@passenger,@adults,@familychildren,@children,@infant,@price,@discount,@commission,@totalprice,@contact,@comments,@paymenttype,@agentid,@confirmationnumber,@voucherId,@custo_paymenttype,@createdDate,@isGoldClass,@PaymentMethod,@saleprice, @Fish,@Steak,@Vegetarian,@CashPaid,@CardPaid,@InvoiceAgent,@POB,@staff)";
                        }
                        cmd.Parameters.AddWithValue("@saleprice", model.saleprice);
                        //cmd.Parameters.AddWithValue("@createdDate", DateTime.Today);
                        cmd.Parameters.AddWithValue("@createdDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@isGoldClass", model.isGoldClass);
                        cmd.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod);
                        cmd.Parameters.AddWithValue("@uid", UserId.ToString());
                        cmd.Parameters.AddWithValue("@bookinguserid", UserId.ToString());
                        //cmd.Parameters.AddWithValue("@voucherId", Convert.ToInt32(TempData["Maxvoucherid"]).ToString());
                        cmd.Parameters.AddWithValue("@voucherId", Convert.ToInt32(voucheridmax).ToString());

                        cmd.Parameters.AddWithValue("@custo_paymenttype", model.custo_paymenttype);

                        if ((int)USERTYPE.ADMIN == UserType || (int)USERTYPE.STAFF == UserType)
                        {
                            cmd.Parameters.AddWithValue("@agent", userName.Trim());
                            cmd.Parameters.AddWithValue("@voucher", model.Voucher);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@agent", userName.Trim());//yummy aading username rather than empty
                            cmd.Parameters.AddWithValue("@voucher", string.Empty);
                        }
                        cmd.Parameters.AddWithValue("@reference", Convert.ToString(model.Reference));

                        cmd.Parameters.AddWithValue("@date", dateTime);
                        cmd.Parameters.AddWithValue("@tourid", model.Tour);
                        cmd.Parameters.AddWithValue("@tourcode", model.TourCode);
                        cmd.Parameters.AddWithValue("@pickuplocation", model.pickuplocation);
                        cmd.Parameters.AddWithValue("@time", model.time);
                        cmd.Parameters.AddWithValue("@passenger", model.PassengerName);
                        cmd.Parameters.AddWithValue("@adults", model.Adults);
                        cmd.Parameters.AddWithValue("@familychildren", model.FamilyChildren);
                        cmd.Parameters.AddWithValue("@children", model.Children);
                        cmd.Parameters.AddWithValue("@infant", model.Infant);
                        cmd.Parameters.AddWithValue("@price", model.Price);
                        cmd.Parameters.AddWithValue("@discount", model.Discount);
                        cmd.Parameters.AddWithValue("@confirmationnumber", model.ConfirmationNumber);
                        cmd.Parameters.AddWithValue("@Fish", model.Fish);
                        cmd.Parameters.AddWithValue("@Steak", model.Steak);
                        cmd.Parameters.AddWithValue("@Vegetarian", model.Vegetarian);

                        if (UserType == (int)USERTYPE.ADMIN || UserType == (int)USERTYPE.STAFF)
                            cmd.Parameters.AddWithValue("@commission", model.Commission);
                        else
                            cmd.Parameters.AddWithValue("@commission", commission);



                        cmd.Parameters.AddWithValue("@CashPaid", Convert.ToDecimal(model.CashPaid));
                        cmd.Parameters.AddWithValue("@CardPaid", Convert.ToDecimal(model.CardPaid));

                        cmd.Parameters.AddWithValue("@InvoiceAgent", Convert.ToDecimal(model.InvoiceAgent));
                        cmd.Parameters.AddWithValue("@POB", Convert.ToDecimal(model.POB));

                        if (model.dateReceived != null)
                        {
                            cmd.Parameters.AddWithValue("@dateReceived", model.dateReceived);
                        }
                        cmd.Parameters.AddWithValue("@staff", (model.staff == null) ? string.Empty : model.staff.ToString());


                        float price = model.Price;
                        float discount = model.Discount;
                        float comm = 0;
                        if (UserType == (int)USERTYPE.ADMIN || UserType == (int)USERTYPE.STAFF)
                            comm = model.Commission;
                        else
                            comm = commission;

                        float temp = price * (discount / 100);
                        float tempPrice = price - temp;
                        temp = tempPrice * (comm / 100);
                        float totalPrice = tempPrice - temp;

                        float CommCal = 0;
                        if (model.Commission != 0)
                            CommCal = model.Commission;
                        else
                            CommCal = commission;

                        if (model.PaymentType == (int)PAYMENT_TYPE.AGENT_INVOICE && creditStatus == (int)CREDIT_STATUS.FULL_AMOUNT)
                            cmd.Parameters.AddWithValue("@totalprice", model.Price - (model.Price * CommCal) / 100);
                        else
                            cmd.Parameters.AddWithValue("@totalprice", totalPrice);

                        if (string.IsNullOrEmpty(model.ContactDetails))
                            cmd.Parameters.AddWithValue("@contact", string.Empty);
                        else
                            cmd.Parameters.AddWithValue("@contact", model.ContactDetails);

                        if (string.IsNullOrEmpty(model.Comments))
                            cmd.Parameters.AddWithValue("@comments", string.Empty);
                        else
                            cmd.Parameters.AddWithValue("@comments", model.Comments);

                        cmd.Parameters.AddWithValue("@paymenttype", model.PaymentType);

                        if ((int)USERTYPE.ADMIN == UserType || (int)USERTYPE.STAFF == UserType)
                        {
                            cmd.Parameters.AddWithValue("@agentid", model.AgentId);
                        }
                        else
                            cmd.Parameters.AddWithValue("@agentid", UserId);

                        cmd.ExecuteNonQuery();

                        limit = limit - (model.Adults + model.FamilyChildren + model.Children + model.Infant);  //on 2015-06-27

                        cmd.CommandText = "UPDATE available_seats SET available = '" + limit + "' where date='" + dateTime + "' and tourid='" + model.Tour + "';";
                        cmd.ExecuteNonQuery();

                        //if (model.PaymentType == (int)PAYMENT_TYPE.AGENT_INVOICE)  && creditStatus == (int)CREDIT_STATUS.FULL_AMOUNT)  on 2015-06-27   we check current entry type  for payment
                        // if (model.PaymentType == (int)PAYMENT_TYPE.AGENT_INVOICE) // commented on 12-09-2016 because we will insert or update all DB
                        {
                            string tourcode = this.CheckTourCode(model.TourCode);
                            float commissionCalculation;
                            cmd = connection.CreateCommand();
                            cmd.CommandText = "INSERT INTO agents_bookings(date,agent_id,tour_money,passengername,adult,children,familychildren,infant,voucher,tourcode,voucherid) VALUES(@date,@agent_id,@tour_money,@passengername,@adult,@children,@familychildren,@infant,@voucher,@tourcode,@voucherid)";
                            cmd.Parameters.AddWithValue("@date", dateTime);
                            cmd.Parameters.AddWithValue("@agent_id", model.AgentId);
                            if (model.Commission != 0)
                                commissionCalculation = model.Commission;
                            else
                                commissionCalculation = commission;

                            cmd.Parameters.AddWithValue("@tour_money", model.Price - (model.Price * commissionCalculation) / 100);
                            cmd.Parameters.AddWithValue("@passengername", model.PassengerName);
                            cmd.Parameters.AddWithValue("@adult", model.Adults);
                            cmd.Parameters.AddWithValue("@children", model.Children);
                            cmd.Parameters.AddWithValue("@familychildren", model.FamilyChildren);
                            cmd.Parameters.AddWithValue("@infant", model.Infant);
                            if (UserType == (int)USERTYPE.AGENT)
                                cmd.Parameters.AddWithValue("@voucher", string.Empty);
                            else
                                cmd.Parameters.AddWithValue("@voucher", model.Voucher);

                            cmd.Parameters.AddWithValue("@tourcode", tourcode);
                            //cmd.Parameters.AddWithValue("@voucherId", Convert.ToInt32(TempData["Maxvoucherid"]).ToString());
                            cmd.Parameters.AddWithValue("@voucherId", Convert.ToInt32(voucheridmax).ToString());

                            cmd.ExecuteNonQuery();
                        }
                    }

                }
                catch (Exception e)
                {
                    isError = true;
                    //ViewBag.Error = e.InnerException + " " + e.Message;
                    error += e.InnerException + " " + e.Message;
                    log.Error(e.Message, e);
                    return "0|Some Error occured.!";

                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
                connection.Open();
                cmd.CommandText = "SELECT max(id) from bookings where bookings.isDeleted = 0";
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    //ViewBag.BookingId = dr.GetInt32(0);
                    id = dr.GetInt32(0);
                }
                dr.Close();
                connection.Close();
                //else
                //    ViewBag.Status = "error";
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                if (errors != null)
                {
                    error = string.Join(",", errors
                                     .Select(e => e.ErrorMessage.ToString()));

                }
            }

            if (UserType == (int)USERTYPE.AGENT)
            {
                @ViewBag.User = "agent";
                @ViewBag.Commission = commission;
            }
            else
            {
                @ViewBag.User = string.Empty;
            }
            if (error != "")
            {
                //  return id.ToString() + "|" + error;
                ModelState.AddModelError("", " enough for this booking.");
                return "0|Pessanger name not contain character.|";

            }
            else
            {
                if (UserType == (int)USERTYPE.AGENT)
                {
                    SendAgentBookingEmailToTNTQ(id);
                }

                return id.ToString();
            }

        }

        public List<SelectListItem> PrepareUsersDropDown(List<RegisterModel> models)
        {
            var listItems = new List<SelectListItem> { new SelectListItem() { Text = "", Value = "" } };
            listItems.AddRange(models.Select(user => new SelectListItem() { Text = user.Name, Value = user.ID.ToString(CultureInfo.InvariantCulture) }));
            //return models.Select(c => new SelectListItem { Value = c.ID.ToString(CultureInfo.InvariantCulture), Text = c.Name }).ToList();
            return listItems;
        }

        public List<SelectListItem> PrepareToursDropDown(List<BookingModel> models)
        {
            return models.Select(c => new SelectListItem { Value = c.Tour.ToString(CultureInfo.InvariantCulture), Text = c.tourname }).ToList();
        }

        public List<SelectListItem> PrepareTourCodesDropDown(List<BookingModel> models)
        {
            return models.Select(c => new SelectListItem { Value = c.TourCode.ToString(CultureInfo.InvariantCulture), Text = c.tourcodevalues }).ToList();
        }

        public List<SelectListItem> PreparePickupLocationDropDown(List<BookingModel> models)
        {
            return models.Select(c => new SelectListItem { Value = c.pickuplocation.ToString(CultureInfo.InvariantCulture), Text = c.location }).ToList();
        }

        public List<SelectListItem> PrepareTimesDropDown(List<Location> models)
        {
            return models.Select(c => new SelectListItem { Value = c.Tour.ToString(CultureInfo.InvariantCulture), Text = c.time }).ToList();
        }

        public List<SelectListItem> PrepareEmptyList()
        {
            return new List<SelectListItem> { new SelectListItem { Value = "0", Text = "" } };
        }

        [HttpGet, ActionName("Comments")]
        public string Comments(int id)
        {
            var repository = new BookingRepository();
            RegisterModel model = repository.GetUser(id);
            return model.Comments + "|" + model.PaymentType;
        }

        [HttpGet, ActionName("TourCodes")]
        public string TourCodes(int id)
        {
            var repository = new BookingRepository();
            return repository.GetTourCodes(id).Aggregate("", (current, title) => current + ("<option value=\"" + title.TourCode + "\">" + title.tourcodevalues + "</option>"));
        }

        [HttpGet, ActionName("PickupLocations")]
        public string PickupLocations(int id)
        {
            var repository = new BookingRepository();
            return repository.GetPickupLocation(id).Aggregate("", (current, title) => current + ("<option value=\"" + title.pickuplocation + "\">" + title.location + "</option>"));
        }

        [HttpGet, ActionName("Times")]
        public string Time(int id)
        {
            var repository = new BookingRepository();
            return repository.GetTimes(id).Aggregate("", (current, title) => current + ("<option value=\"" + title.Tour + "\">" + title.time + "</option>"));
        }

        //public SelectList getState()
        //{
        //   // IEnumerable<SelectListItem> stateList = (from m in db  where m.bstatus == true select m).AsEnumerable().Select(m => new SelectListItem() { Text = m.vstate, Value = m.istateid.ToString() });
        //  //  var repository = new BookingRepository();
        //   // IEnumerable<SelectListItem> stateList = (repository.GetTimes(id).Aggregate("", (curre)) { Text = m.vstate, Value = m.istateid.ToString() });
        //   // return new SelectList(stateList, "Value", "Text", istateid);
        //}

        [HttpGet]
        public ActionResult GetBookingsByDate(string year, string month, string date, int TourCodeID)
        {
            var rep = new BookingRepository();
            return Json(rep.GetBookingsByDate(year, month, date, TourCodeID), JsonRequestBehavior.AllowGet);
        }

        //Changes made on 21-06-2016
        [HttpGet]
        public ActionResult GetBookingsByDateData(string year, string month, string date)
        {
            var rep = new BookingRepository();
            return Json(rep.GetBookingsByDateData(year, month, date), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Seats()
        {

            List<SelectListItem> selectListItems = new List<SelectListItem>();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT tn.id, tn.tourname FROM tournames as tn WHERE 1;";
            dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                SelectListItem item = new SelectListItem();
                item.Text = dr.GetString(1);
                item.Value = dr.GetString(0);
                selectListItems.Add(item);
            }
            dr.Close();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            ViewBag.Tours = selectListItems;
            return View(new AvailabilityModel());
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //TEMPORARY CODE

        public ActionResult SeatsTemp()
        {
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT tn.id, tn.tourname FROM tournames as tn WHERE 1;";
            dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                SelectListItem item = new SelectListItem();
                item.Text = dr.GetString(1);
                item.Value = dr.GetString(0);
                selectListItems.Add(item);
            }
            dr.Close();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            ViewBag.Tours = selectListItems;
            return View(new AvailabilityModel());
        }

        [HttpPost]
        public string SeatsTemp(string[] attr1, string[] attr2, string attr3)
        {
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "START TRANSACTION;"
                + " UPDATE available_seats_temp SET date = DATE_ADD(date, INTERVAL 10 YEAR) WHERE tourid = " + attr3 + " and YEAR(date) = YEAR(CURDATE()); ";
            if (attr2.Any(x => Convert.ToInt32(x) != 0))
                cmd.CommandText += " INSERT INTO available_seats_temp(tourid,date,available) VALUES";

            //PREPARE STRING FOR BULK ADDITION
            for (var i = 0; i < attr2.Length; i++)
            {
                if (!string.IsNullOrEmpty(attr2[i]) && Convert.ToInt32(attr2[i]) != 0)
                    cmd.CommandText += " (" + attr3 + ",'" + attr1[i] + "'," + attr2[i] + "),";
            }
            cmd.CommandText = cmd.CommandText.Substring(0, cmd.CommandText.Length - 1);

            cmd.CommandText += ";";
            cmd.CommandText += " COMMIT;";
            cmd.ExecuteNonQuery();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            return "Temporary Seats saved Successfully";
        }

        //Code on 2016-05-03
        [HttpPost]
        public JsonResult SeatDataInfo(string todate, string noofday, int tourid)
        {
            AvailabilityModel model = new AvailabilityModel();
            List<AvailabilityModel> list = new List<AvailabilityModel>();
            // var currdate = DateTime.ParseExact(todate, "dd/m/yyyy", new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            string[] date = todate.Split('/');
            string currdate = date[2] + '-' + date[1] + '-' + date[0];
            try
            {

                connection.Open();
                cmd = connection.CreateCommand();
                /*old 
               

                //Commented on 09-06-2016
                //cmd.CommandText = "SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'),ast.tourid,ast.available Available , ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied,ast.available"
                //            + " FROM ( select distinct available, date, tourid  from available_seats ) ast "
                //            + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid "
                //            + " WHERE ast.tourid=" + tourid + " and ast.date >= '" + currdate + "' and ast.date<='" + currdate + "' + INTERVAL 8 DAY   and b.isDeleted = 0"
                //            + " group by ast.date";
                */

                /*commented on 26-09-2016
                cmd.CommandText = "SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'),ast.tourid,ast.available Available , ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied,ast.available"
                            + " FROM ( select distinct available, date, tourid  from available_seats ) ast "
                            + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid "
                            + " WHERE ast.tourid=" + tourid + " and ast.date >= '" + currdate + "' and ast.date<='" + currdate + "' + INTERVAL 60 DAY and (b.isDeleted = 0 or b.isDeleted is null)"
                            + " group by ast.date";

                */

                //05-02-2017 Seat Calculation
                //cmd.CommandText = "SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'),ast.tourid,(ast.available + ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0)) Available , ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied,(ast.available + + ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0))available"
                //            + " FROM ( select distinct available, date, tourid  from available_seats ) ast "
                //            + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid "
                //            + " WHERE ast.tourid=" + tourid + " and ast.date >= '" + currdate + "' and ast.date<='" + currdate + "' + INTERVAL 15 DAY and (b.isDeleted = 0 or b.isDeleted is null)"
                //            + " group by ast.date";

                cmd.CommandText = "SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'),ast.tourid,(ast.available + ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0)) Available , ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied,(ast.available) available"
                            + " FROM ( select distinct available, date, tourid  from available_seats ) ast "
                            + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid "
                            + " WHERE ast.tourid=" + tourid + " and ast.date >= '" + currdate + "' and ast.date<='" + currdate + "' + INTERVAL 15 DAY and (b.isDeleted = 0 or b.isDeleted is null)"
                            + " group by ast.date";


                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new AvailabilityModel
                    {
                        Date1 = dr.GetString(0),
                        // Seat = dr.GetInt32(1),
                        TourID = dr.GetInt16(1),
                        TotalSeats = dr.GetInt32(2),
                        OccupiedSeats = dr.GetInt32(3),
                        Seat = dr.GetInt32(4)

                    });
                }
                dr.Close();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            return Json(list);


        }

        [HttpPost]
        public JsonResult SeatData(string todate, string noofday, int tourid)
        {
            AvailabilityModel model = new AvailabilityModel();
            List<AvailabilityModel> list = new List<AvailabilityModel>();
            var currdate = DateTime.ParseExact(todate, "d/m/yyyy", new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                //cmd.CommandText = "SELECT  ast.available Available, ifnull(sum(b.adults + b.children + b.infant + b.familyChildren), 0) Occupied "
                //                    + " FROM available_seats ast "
                //                    + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid "
                //                    + " WHERE ast.tourid = " + tourid + " and  date >= '" + currdate + "' and date<= '" + currdate + "' + INTERVAL 8 DAY ";

                //cmd.CommandText = "SELECT  ast.available Available, ifnull(sum(b.adults + b.children + b.infant + b.familyChildren), 0) Occupied,ast.available"
                //                  + " FROM ( select distinct available, date, tourid  from available_seats ) ast "
                //                  + " inner join bookings b on ast.date = b.date "
                //                  + " WHERE ast.date >= '" + currdate + "' and ast.date<='" + currdate + "' + INTERVAL 8 DAY  and b.isDeleted = 0"
                //                  + " group by ast.date,ast.tourid";
                cmd.CommandText = "SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'),ast.tourid,ast.available Available , ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied,ast.available"
                            + " FROM ( select distinct available, date, tourid  from available_seats ) ast "
                            + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid "
                            + " WHERE ast.tourid=" + tourid + " and ast.date >= '" + currdate + "' and ast.date<='" + currdate + "'  + INTERVAL 15 DAY  and b.isDeleted = 0"
                            + " group by  ast.date";

                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new AvailabilityModel
                    {
                        Date1 = dr.GetString(0),
                        // Seat = dr.GetInt32(1),
                        TourID = dr.GetInt16(1),
                        TotalSeats = dr.GetInt32(2),
                        OccupiedSeats = dr.GetInt32(3),
                        Seat = dr.GetInt32(4)

                    });
                }
                dr.Close();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            return Json(list);

        }
        public JsonResult TourCodeValue()
        {
            AvailabilityModel model = new AvailabilityModel();
            List<AvailabilityModel> list = new List<AvailabilityModel>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT distinct a.tourid,t.tourname FROM available_seats a INNER JOIN tournames t ON a.tourid = t.id";
                //     cmd.CommandText = "select distinct tourid from  available_seats"; 
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new AvailabilityModel
                    {
                        TourID = dr.GetInt16(0),
                        TourName = dr.GetString(1)

                    });
                }
                dr.Close();

            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return Json(list);
        }
        //End Code

        public string GetAllSeatsTemp(int id)
        {
            AvailabilityModel model = new AvailabilityModel();
            List<AvailabilityModel> list = new List<AvailabilityModel>();
            connection.Open();
            cmd = connection.CreateCommand();
            //cmd.CommandText = "SELECT DATE_FORMAT(seats.date,'%d/%m/%Y'), seats.available FROM `available_seats_temp` as seats WHERE seats.tourid =  " + id.ToString() + " and YEAR(seats.date) = YEAR(CURDATE()) ORDER BY seats.date ASC;";
            cmd.CommandText = " SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'), ast.available Available, ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied "
                            + " FROM available_seats_temp ast "
                            + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid "
                            + " WHERE ast.tourid = " + id + " and YEAR(ast.date) = YEAR(CURDATE()) "
                            + " group by ast.date ";
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new AvailabilityModel
                {
                    Date1 = dr.GetString(0),
                    //Seat = dr.GetInt32(1),
                    TotalSeats = dr.GetInt32(1),
                    OccupiedSeats = dr.GetInt32(2)
                });
            }
            dr.Close();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            return (new JavaScriptSerializer()).Serialize(list);
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [HttpPost]
        public string Seats(string[] attr1, string[] attr2, string attr3)
        {
            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "START TRANSACTION;"
                + " UPDATE available_seats SET date = DATE_ADD(date, INTERVAL 10 YEAR) WHERE tourid = " + attr3 + " and YEAR(date) = YEAR(CURDATE()); ";
            if (attr2.Any(x => Convert.ToInt32(x) != 0))
                cmd.CommandText += " INSERT INTO available_seats(tourid,date,available) VALUES";

            //PREPARE STRING FOR BULK ADDITION
            for (var i = 0; i < attr2.Length; i++)
            {
                if (!string.IsNullOrEmpty(attr2[i]) && Convert.ToInt32(attr2[i]) != 0)
                    cmd.CommandText += " (" + attr3 + ",'" + attr1[i] + "'," + attr2[i] + "),";
            }
            cmd.CommandText = cmd.CommandText.Substring(0, cmd.CommandText.Length - 1);
            cmd.CommandText += ";";
            cmd.CommandText += " COMMIT;";
            cmd.ExecuteNonQuery();
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            return "Seats saved Successfully";
        }

        public string GetAllSeats(int id)
        {
            AvailabilityModel model = new AvailabilityModel();
            List<AvailabilityModel> list = new List<AvailabilityModel>();
            connection.Open();
            cmd = connection.CreateCommand();

            //cmd.CommandText = "SELECT DATE_FORMAT(seats.date,'%d/%m/%Y'), seats.available FROM `available_seats` as seats WHERE seats.tourid =  " + id.ToString() + " and YEAR(seats.date) = YEAR(CURDATE()) ORDER BY seats.date ASC;";

            cmd.CommandText = " SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'), ast.available Available , ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied,ast.available"
                            + " FROM ( select distinct available, date, tourid  from available_seats ) ast "
                            + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid "
                            + " WHERE ast.tourid = " + id + " and YEAR(ast.date) = YEAR(CURDATE()) and b.isDeleted = 0"
                            + " group by ast.date ";



            // cmd.CommandText = "SELECT DATE_FORMAT(date,'%d/%m/%Y'), ifnull(sum(adults+children+infant+familyChildren),0)  FROM bookings  where tourid=" + id + " and isdeleted=0  group by date";
            //cmd.CommandText = " SELECT DATE_FORMAT(ast.date,'%d/%m/%Y'), ast.available Available , ifnull(sum(b.adults+b.children+b.infant+b.familyChildren),0) Occupied,ast.available"
            //                + " FROM available_seats ast"
            //                + " left join bookings b on ast.date = b.date and ast.tourid = b.tourid"
            //                + " WHERE ast.tourid = " + id + " and MONTH(ast.date) = MONTH(CURDATE()) and(b.isDeleted = 0 or b.isDeleted is null)"
            //                + " group by ast.date";

            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new AvailabilityModel
                {
                    Date1 = dr.GetString(0),
                    // Seat = dr.GetInt32(1),
                    TotalSeats = dr.GetInt32(1),
                    OccupiedSeats = dr.GetInt32(2),
                    Seat = dr.GetInt32(3),

                });
            }

            /*cmd1 = connection.CreateCommand();
            cmd1.CommandText = "SELECT available FROM available_seats where  tourid = " + id + " and YEAR(date) = YEAR(CURDATE()) LIMIT 1";
            dr.Close();
            dr1 = cmd1.ExecuteReader();
            while (dr1.Read())
            {
                list.Add(new AvailabilityModel
                {
                    TotalSeats = dr1.GetInt32(0),
                    // Seat = TotalSeats- OccupiedSeats,

                });
            }
            dr1.Close(); */
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            return (new JavaScriptSerializer()).Serialize(list);
        }

        //Adding on 2016-05-13 For Display Tour Names
        public ActionResult ViewTournames()
        {
            var repository = new BookingRepository();
            List<BookingModel> models = repository.GetTourCodesData();
            return View(models);
        }
        public ActionResult AddTour()
        {
            ViewBag.Status = "false";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTour(BookingModel model)
        {
            //if (ModelState.IsValid)
            //{
            bool isError = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO tourcode(tourid,tourcodevalues) VALUES(@tourid,@tourcodevalues)";
                cmd.Parameters.AddWithValue("@tourid", model.Tour);
                cmd.Parameters.AddWithValue("@tourcodevalues", model.tourcodevalues);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";
            //}
            //else
            //    ViewBag.Status = "false";

            return View();
        }

        public JsonResult DeleteTourCode(int id)
        {
            bool isError = false;
            int Total = 0;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "select count(*) from bookings where tourcode=" + id + " and isDeleted=0";
                Total = Convert.ToInt16(cmd.ExecuteScalar().ToString());
                if (Convert.ToInt16(cmd.ExecuteScalar().ToString()) == 0)
                {
                    cmd.CommandText = "UPDATE tourcode SET isDeleted = 1  where id = " + id;
                    cmd.ExecuteNonQuery();
                }
            }

            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return Json(!isError);
        }

        public ActionResult EditTour(int id)
        {
            BookingModel model = null;
            bool isError = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from tourcode where id='" + id + "';";
                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    model = new BookingModel
                    {
                        //tid = dr.GetInt16(1),
                        tourcodevalues = dr.GetString(2)
                    };
                    ViewBag.tid = dr.GetInt16(0);
                    ViewBag.Tour = dr.GetInt16(1);
                }
                dr.Close();

            }
            catch (Exception e)
            {
                isError = true;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "ok";
            else
                ViewBag.Status = "error";

            return View(model);
        }
        public ActionResult ViewTour()
        {
            var repository = new BookingRepository();
            List<BookingModel> models = repository.GetTourNames();
            return View(models);
        }
        [HttpPost]
        public string EditTour(string Tour, string tourcodevalues, string TourCode)
        {
            bool isBool = true;
            if (!String.IsNullOrEmpty(Tour) && !String.IsNullOrEmpty(tourcodevalues))
            {
                try
                {
                    connection.Open();
                    cmd = connection.CreateCommand();
                    cmd.CommandText = "UPDATE tourcode SET tourid =@tourID,tourcodevalues= @tourcodevalues where id=@Id";
                    cmd.Parameters.AddWithValue("@tourID", Tour);
                    cmd.Parameters.AddWithValue("@tourcodevalues", tourcodevalues);
                    cmd.Parameters.AddWithValue("@Id", TourCode);
                    cmd.ExecuteNonQuery();
                    return "success";
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                    log.Error(e.Message, e);
                    return "error";
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
            }

            else
            {
                return "Please complete the form.";
            }
        }
        public ActionResult AddTourData()
        {
            ViewBag.Status = "false";
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTourData(BookingModel model)
        {
            //if (ModelState.IsValid)
            //{
            bool isError = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO tournames(id,tourname) VALUES(@id,@tourname)";
                cmd.Parameters.AddWithValue("@id", model.Tour);
                cmd.Parameters.AddWithValue("@tourname", model.tourname);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "true";
            else
                ViewBag.Status = "error";
            //}
            //else
            //    ViewBag.Status = "false";

            return View();
        }

        public JsonResult DeleteTour(int id)
        {
            bool isError = false;
            int Total = 0;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "select count(*) from bookings where tourid=" + id + " and isDeleted=0";
                Total = Convert.ToInt16(cmd.ExecuteScalar().ToString());
                if (Convert.ToInt16(cmd.ExecuteScalar().ToString()) == 0)
                {
                    cmd.CommandText = "delete from tournames where id=" + id;
                    //cmd.CommandText = "UPDATE tourcode SET isDeleted = 1  where id = " + id;
                    cmd.ExecuteNonQuery();
                }
            }

            catch (Exception e)
            {
                isError = true;
                log.Error(e.Message, e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return Json(!isError);
        }

        public ActionResult EditTourData(int id)
        {
            BookingModel model = null;
            bool isError = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from tournames where id='" + id + "';";
                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    model = new BookingModel
                    {
                        //tid = dr.GetInt16(1),
                        tourname = dr.GetString(1)
                    };
                    ViewBag.id = dr.GetInt16(0);
                }
                dr.Close();

            }
            catch (Exception e)
            {
                isError = true;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            if (!isError)
                ViewBag.Status = "ok";
            else
                ViewBag.Status = "error";

            return View(model);
        }

        [HttpPost]
        public string EditTourData(string tournames, string id)
        {
            bool isBool = true;
            if (!String.IsNullOrEmpty(tournames))
            {
                try
                {
                    connection.Open();
                    cmd = connection.CreateCommand();
                    cmd.CommandText = "UPDATE tournames SET tourname= @tournames where id=@Id";
                    cmd.Parameters.AddWithValue("@tournames", tournames);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                    return "success";
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                    log.Error(e.Message, e);
                    return "error";
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
            }
            else
            {
                return "Please complete the form.";
            }
        }
        //End Codes

        [HttpPost]
        public string DeleteBookingFromSeats(string bid, int tId, string tdt, int tseat)
        {

            connection.Open();
            cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE bookings SET isDeleted = 1  where id = " + bid;
            if (cmd.ExecuteNonQuery() == 1)
            {
                cmd.CommandText = "select available from available_seats where tourid =" + tId + " and date='" + tdt + "'";
                int totseatavailbe = Convert.ToInt16(cmd.ExecuteScalar().ToString()) + tseat;
                cmd.CommandText = "UPDATE available_seats SET available =" + totseatavailbe + " where tourid =" + tId + " and date='" + tdt + "'";
                cmd.ExecuteNonQuery();
            }
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();

            return "Booking deleted Successfully";
        }

        //Code made on 27-06-2016
        [HttpPost]
        public ActionResult getEmail(string username)
        {
            MailMessage mail = new MailMessage();
            //SmtpClient smtp = new SmtpClient();
            try
            {
                cmd = connection.CreateCommand();
                connection.Open();
                cmd.CommandText = "select B.Id,t.time,l.location,B.passenger,B.adults,B.children,B.familychildren,B.infant,b.tourcode,B.totalprice,B.POB,B.agentinvoice,B.comments, CONCAT(B.Fish  , ' ',  B.Steak , ' ' , B.Vegetarian)'Lunch(F,S,V)',  B.comments from bookings B JOIN locations l ON B.pickuplocation = l.id JOIN times T ON B.time = T.timeid where B.isDeleted = 0";
                dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                DataTable dt = new DataTable();
                dt.Load(dr);
                connection.Close();

                /*
                DataTable dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[3] {
                                new DataColumn("OrderId"),
                                new DataColumn("Product"),
                                new DataColumn("Quantity")});
                dt.Rows.Add(101, "Sun Glasses", 5);
                dt.Rows.Add(102, "Jeans", 2);
                dt.Rows.Add(103, "Trousers", 12);

                */
                string companyName = "ASPSnippets";
                int orderNo = 2303;
                StringBuilder sb = new StringBuilder();
                sb.Append("<table width='100%' cellspacing='0' cellpadding='2'>");
                sb.Append("<tr><td align='center' style='background-color: #18B5F0' colspan = '2'><b>Order Sheet</b></td></tr>");
                sb.Append("<tr><td colspan = '2'></td></tr>");
                sb.Append("<tr><td><b>Order No:</b>");
                sb.Append(orderNo);
                sb.Append("</td><td><b>Date: </b>");
                sb.Append(DateTime.Now);
                sb.Append(" </td></tr>");
                sb.Append("<tr><td colspan = '2'><b>Company Name :</b> ");
                sb.Append(companyName);
                sb.Append("</td></tr>");
                sb.Append("</table>");
                sb.Append("<br />");
                sb.Append("<table border = '1'>");
                sb.Append("<tr>");

                foreach (DataColumn column in dt.Columns)
                {
                    sb.Append("<th style = 'background-color: #D20B0C;color:#ffffff'>");
                    sb.Append(column.ColumnName);
                    sb.Append("</th>");
                }

                sb.Append("</tr>");
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        sb.Append("<td>");
                        sb.Append(row[column]);
                        sb.Append("</td>");
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
                StringReader sr = new StringReader(sb.ToString());

                //Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);
                Document pdfDoc = new Document(PageSize.A4, 30, 30, 30, 30);
                HTMLWorker htmlparser = new HTMLWorker(pdfDoc);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);
                    pdfDoc.Open();
                    htmlparser.Parse(sr);
                    pdfDoc.Close();
                    byte[] bytes = memoryStream.ToArray();
                    memoryStream.Close();

                    MailMessage mm = new MailMessage("demo.narolainfotech@gmail.com", "kp@narola.email");
                    mm.Subject = "iTextSharp PDF";
                    mm.Body = "iTextSharp PDF Attachment";
                    mm.Attachments.Add(new Attachment(new MemoryStream(bytes), "iTextSharpPDF.pdf"));
                    mm.IsBodyHtml = true;
                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = "smtp.gmail.com";
                    smtp.EnableSsl = true;
                    NetworkCredential NetworkCred = new NetworkCredential();
                    NetworkCred.UserName = "demo.narolainfotech@gmail.com";
                    NetworkCred.Password = "Narola102";
                    smtp.UseDefaultCredentials = true;
                    smtp.Credentials = NetworkCred;
                    smtp.Port = 587;
                    smtp.Send(mm);
                }

                /*
                string email = "";
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select email from users where username='" + username + "'";
                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    email = dr.GetString(0);
                }
                dr.Close();
                mail.To.Add("kp@narola.email");
                mail.From = new MailAddress("demo.narolainfotech@gmail.com");
                mail.Subject = "Report";
                mail.Body = "Please Find Attachement";
                
                mail.Attachments.Add(new Attachment("D:/Tourism Project/Tourism Project/Tourism Project/pdf/pdf.pdf"));
                mail.IsBodyHtml = true;
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential
                ("demo.narolainfotech@gmail.com", "Narola102");// Enter senders User name and password  
                smtp.EnableSsl = true;
                smtp.Send(mail);
                */
            }
            catch (Exception e)
            {
                //isError = true;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            return RedirectToAction("ReportsView");
        }
        //End Code



        //Start on 24-09-2016

        public int getLastAvailSeatLimit(string date, int tourid)
        {
            int limit = 0;
            string[] formats = { "dd/mm/yyyy", "d/m/yyyy" };
            var dateTime = DateTime.ParseExact(date, formats, new CultureInfo("en-US"), DateTimeStyles.None).ToString("yyyy-mm-dd");

            bool check = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from available_seats where date='" + dateTime + "' and tourid='" + tourid + "';";
                dr = cmd.ExecuteReader();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        //check = true;
                        limit = dr.GetInt32("available");
                    }
                }
                else
                {
                    if (tourid == 1)
                        limit = 18;//16;
                    else
                        limit = 21;//24;

                }
                return (limit);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                return 0;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

         
        }

        protected MemoryStream CreatePDF(List<BookingModel> models, string Title, string Address, string Invoice, bool Single)
        {
            // Create a Document object
            Document document = new Document(PageSize.A4.Rotate(), 30, 30, 30, 30);

            //MemoryStream
            MemoryStream PDFData = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(document, PDFData);

            // First, create our fonts
            var boldTableFont = FontFactory.GetFont("Times", 14, Font.BOLD);
            var bodyFont = FontFactory.GetFont("Times", 12, Font.NORMAL);
            Rectangle pageSize = writer.PageSize;

            // Open the Document for writing
            document.Open();
            //Add elements to the document here

            #region Header Table
            
            // Create the header table 
            PdfPTable headertable = new PdfPTable(2);
            headertable.HorizontalAlignment = 0;
            headertable.WidthPercentage = 100;
            headertable.SetWidths(new float[] { 5, 5 });  // then set the column's __relative__ widths
            headertable.DefaultCell.Border = Rectangle.NO_BORDER;
            headertable.SpacingAfter = 30;

            iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(Server.MapPath("~/Images/LOGO.png"));
            logo.ScaleToFit(64f, 64f);
            //logo.Width = 64;
            PdfPCell logoCell = new PdfPCell(logo, false);
            logoCell.HorizontalAlignment = 0;
            logoCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(logoCell);

            PdfPCell invoiceCell = new PdfPCell(new Phrase("Invoice : " + Invoice, boldTableFont));
            invoiceCell.HorizontalAlignment = 2;
            invoiceCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(invoiceCell);

            PdfPCell agentCell = new PdfPCell(new Phrase(Title, FontFactory.GetFont("Times", 13, Font.BOLDITALIC, Color.RED)));
            agentCell.HorizontalAlignment = 0;
            agentCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(agentCell);

            PdfPCell trekNorthCell = new PdfPCell(new Phrase("Trek North", boldTableFont));
            trekNorthCell.HorizontalAlignment = 2;
            trekNorthCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(trekNorthCell);

            PdfPCell addrCell = new PdfPCell(new Phrase(Address, bodyFont));
            addrCell.HorizontalAlignment = 0;
            addrCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(addrCell);

            PdfPCell phoneCell = new PdfPCell(new Phrase("Phone : " + ConfigurationManager.AppSettings["PhoneNumber"] != null ? ConfigurationManager.AppSettings["PhoneNumber"].ToString() : "", bodyFont));
            phoneCell.HorizontalAlignment = 2;
            phoneCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(phoneCell);

            PdfPCell reportDateCell = new PdfPCell(new Phrase("Report Date : " + DateTime.Now.AddHours(2).ToString("MM/dd/yyyy"), bodyFont));
            reportDateCell.HorizontalAlignment = 0;
            reportDateCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(reportDateCell);

            //PdfPCell addressCell = new PdfPCell(new Phrase("PO Box 2901, Cairns, QId 4870", bodyFont));
            PdfPCell addressCell = new PdfPCell(new Phrase(" ", bodyFont));
            addressCell.HorizontalAlignment = 2;
            addressCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(addressCell);

            PdfPCell reportTimeCell = new PdfPCell(new Phrase("Report Time : " + DateTime.Now.AddHours(2).ToString("hh:mm tt"), bodyFont));
            reportTimeCell.HorizontalAlignment = 0;
            reportTimeCell.Colspan = 2;
            reportTimeCell.Border = Rectangle.NO_BORDER;
            headertable.AddCell(reportTimeCell);

            #endregion

            #region Item Table

            float total = 0;
            float totalPrice = 0;

            //Create body table
            PdfPTable itemTable = new PdfPTable(4);
            if (Single == false)
            {
                itemTable = new PdfPTable(7);
                itemTable.WidthPercentage = 100;
                itemTable.SetWidths(new float[] { 1,2,1.5f,1.5f,2,1,1 });
            }
            else
            {
                itemTable = new PdfPTable(6);
                itemTable.WidthPercentage = 100;
                itemTable.SetWidths(new float[] { 1,2,1.5f,3,1,1 });
            }
            itemTable.HorizontalAlignment = 0;
              // then set the column's __relative__ widths
            itemTable.SpacingAfter = 20;
            itemTable.DefaultCell.Border = Rectangle.BOX;
            itemTable.DefaultCell.BorderWidth = 0.25f;
            itemTable.DefaultCell.Padding = 5f;

            PdfPCell cell1 = new PdfPCell(new Phrase("Tour Date", boldTableFont));
            cell1.HorizontalAlignment = 1;
            cell1.Padding = 5f;
            itemTable.AddCell(cell1);
            

            if (Single == false)
            {
                PdfPCell cell2 = new PdfPCell(new Phrase("Agent", boldTableFont));
                cell2.HorizontalAlignment = 0;
                cell2.Padding = 5f;
                itemTable.AddCell(cell2);
            }

            PdfPCell cell3 = new PdfPCell(new Phrase("Tour", boldTableFont));
            cell3.HorizontalAlignment = 0;
            cell3.Padding = 5f;
            itemTable.AddCell(cell3);

            PdfPCell cell4 = new PdfPCell(new Phrase("Voucher", boldTableFont));
            cell4.HorizontalAlignment = 0;
            cell4.Padding = 5f;
            itemTable.AddCell(cell4);

            PdfPCell cell5 = new PdfPCell(new Phrase("Passenger", boldTableFont));
            cell5.HorizontalAlignment = 0;
            cell5.Padding = 5f;
            itemTable.AddCell(cell5);

            PdfPCell cell6 = new PdfPCell(new Phrase("Tour Price", boldTableFont));
            cell6.HorizontalAlignment = 2;
            cell6.Padding = 5f;
            itemTable.AddCell(cell6);

            PdfPCell cell7 = new PdfPCell(new Phrase("Payable", boldTableFont));
            cell7.HorizontalAlignment = 2;
            cell7.Padding = 5f;
            itemTable.AddCell(cell7);

            foreach (BookingModel row in models)
            {
                PdfPCell tourdateCell = new PdfPCell(new Phrase(row.Date, bodyFont));
                tourdateCell.HorizontalAlignment = 1;
                tourdateCell.Padding = 5f;
                tourdateCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(tourdateCell);

                if (Single == false)
                {
                    PdfPCell agentNameCell = new PdfPCell(new Phrase(row.Agent, bodyFont));
                    agentNameCell.HorizontalAlignment = 0;
                    agentNameCell.Padding = 5f;
                    agentNameCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                    itemTable.AddCell(agentNameCell);
                }

                PdfPCell tourCell = new PdfPCell(new Phrase(row.tourname, bodyFont));
                tourCell.HorizontalAlignment = 0;
                tourCell.Padding = 5f;
                tourCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(tourCell);

                PdfPCell voucherCell = new PdfPCell(new Phrase(row.Voucher, bodyFont));
                voucherCell.HorizontalAlignment = 0;
                voucherCell.Padding = 5f;
                voucherCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(voucherCell);

                PdfPCell passengerCell = new PdfPCell(new Phrase(row.PassengerName, bodyFont));
                passengerCell.HorizontalAlignment = 0;
                passengerCell.Padding = 5f;
                passengerCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(passengerCell);

                PdfPCell tourpriceCell = new PdfPCell(new Phrase("$" + String.Format("{0:0.00}", row.Price), bodyFont));
                tourpriceCell.HorizontalAlignment = 2;
                tourpriceCell.Padding = 5f;
                tourpriceCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(tourpriceCell);

                PdfPCell payableCell = new PdfPCell(new Phrase("$" + String.Format("{0:0.00}", row.TotalPrice), bodyFont));
                payableCell.HorizontalAlignment = 2;
                payableCell.Padding = 5f;
                payableCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(payableCell);

                total += row.Price;
                totalPrice += (float)row.TotalPrice;

            }

            //Total Section
            PdfPCell empCell = new PdfPCell(new Phrase("", bodyFont));
            empCell.HorizontalAlignment = 1;
            empCell.Padding = 5f;
            empCell.Border = Rectangle.BOX;
            itemTable.AddCell(empCell);

            if (Single == false)
            {
                itemTable.AddCell(empCell);
            }
            itemTable.AddCell(empCell);
            itemTable.AddCell(empCell);

            PdfPCell amountDueLabelCell = new PdfPCell(new Phrase("Amount Due", bodyFont));
            amountDueLabelCell.HorizontalAlignment = 0;
            amountDueLabelCell.Padding = 5f;
            amountDueLabelCell.Border = Rectangle.BOX;
            itemTable.AddCell(amountDueLabelCell);

            PdfPCell totalamountDueCell = new PdfPCell(new Phrase("$" + String.Format("{0:0.00}", total), bodyFont));
            totalamountDueCell.HorizontalAlignment = 2;
            totalamountDueCell.Padding = 5f;
            totalamountDueCell.Border = Rectangle.BOX;
            itemTable.AddCell(totalamountDueCell);

            PdfPCell totalpayableCell = new PdfPCell(new Phrase("$" + String.Format("{0:0.00}", totalPrice), bodyFont));
            totalpayableCell.HorizontalAlignment = 2;
            totalpayableCell.Padding = 5f;
            totalpayableCell.Border = Rectangle.BOX;
            itemTable.AddCell(totalpayableCell);

            PdfPCell allGSTCell = new PdfPCell(new Phrase("All GST Inclusive", boldTableFont));
            allGSTCell.HorizontalAlignment = 2;
            allGSTCell.Colspan = Single == false ? 7 : 6;
            allGSTCell.Padding = 5f;
            allGSTCell.Border = Rectangle.NO_BORDER;
            itemTable.AddCell(allGSTCell);

            #endregion

            #region Footer Table

            // Create the header table 
            PdfPTable footertable = new PdfPTable(1);
            footertable.HorizontalAlignment = 2;
            footertable.WidthPercentage = 21;
            footertable.DefaultCell.Border = Rectangle.NO_BORDER;
            footertable.SpacingAfter = 10;


            PdfPCell paymentMadeToCell = new PdfPCell(new Phrase("Payments made to:", boldTableFont));
            paymentMadeToCell.HorizontalAlignment = 0;
            paymentMadeToCell.Border = Rectangle.NO_BORDER;
            footertable.AddCell(paymentMadeToCell);

            PdfPCell paymentAccountNameCell = new PdfPCell(new Phrase(ConfigurationManager.AppSettings["PaymentAccountName"] != null ? ConfigurationManager.AppSettings["PaymentAccountName"].ToString() : "", bodyFont));
            paymentAccountNameCell.HorizontalAlignment = 0;
            paymentAccountNameCell.Border = Rectangle.NO_BORDER;
            footertable.AddCell(paymentAccountNameCell);

            PdfPCell BSBCell = new PdfPCell(new Phrase("BSB : " + (ConfigurationManager.AppSettings["BSB"] != null ? ConfigurationManager.AppSettings["BSB"].ToString() : ""), bodyFont));
            BSBCell.HorizontalAlignment = 0;
            BSBCell.Border = Rectangle.NO_BORDER;
            footertable.AddCell(BSBCell);

            PdfPCell ACCCell = new PdfPCell(new Phrase("ACC : " + (ConfigurationManager.AppSettings["ACC"] != null ? ConfigurationManager.AppSettings["ACC"].ToString() : ""), bodyFont));
            ACCCell.HorizontalAlignment = 0;
            ACCCell.Border = Rectangle.NO_BORDER;
            footertable.AddCell(ACCCell);
            
            #endregion

            document.Add(headertable);
            document.Add(itemTable);
            document.Add(footertable);
            writer.CloseStream = false; //set the closestream property
            // Close the Document without closing the underlying stream
            document.Close();

            return PDFData;
        }

        //End

    }


    public class TempClass
    {
        public List<BookingModel> models;
        public bool single;
    }
}
