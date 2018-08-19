using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using Trg.Data;
using Tourism_Project.Models;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.Web.Configuration;
using System.Globalization;

namespace Tourism_Project.Controllers
{
    public class VoucherRepository : EntityRepository<Voucher>
    {

        /*mysql*/
        private MySqlConnection mysqlconnection;
        private MySqlCommand mysqlcmd;
        private MySqlDataReader mysqldr;
        #region Connection String
        static string connString = WebConfigurationManager.AppSettings["ServerDBConnection"].ToString();
        #endregion
        /*mysql*/

        //public VoucherRepository() : base(new VoucherEntities()) { }
        public VoucherRepository()
            : base(new VoucherEntities())
        {
            mysqlconnection = new MySqlConnection(connString);
            mysqlcmd = new MySqlCommand();
            mysqldr = null;
        }

        public List<Voucher> Select(string VoucherID, int CompanyID, DateTime TravelDate, int? OnPageCount, int? RequiredNumber)
        {
            if (OnPageCount == null)
                OnPageCount = 0;
            if (RequiredNumber == null)
                RequiredNumber = 10;
            var query = "   SELECT x.*                                   "
                                                               + " FROM (                                                             "
                                                               + "     SELECT v.*                                                     "
                //+ "        , Row_Number() OVER (ORDER BY v.Create_Date DESC) as rowNum  "
                                                               + "        , Row_Number() OVER (ORDER BY v.VoucherBookingID DESC) as rowNum  "
                                                               + "     FROM dbo.Vouchers v                                            "
                                                               + "     WHERE v.IsActive=1                                                      "
                           + (!string.IsNullOrEmpty(VoucherID) ? "     AND v.VoucherID = '" + VoucherID.ToString() + "'               " : "")
                                             + (CompanyID != 0 ? "     AND v.CompanyID = " + CompanyID.ToString() + "                 " : "")
                            + (TravelDate != DateTime.MinValue ? "     AND v.TravelDate between '" + TravelDate.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "' AND '" + TravelDate.AddHours(23).AddMinutes(59).ToString("yyyy-MM-ddTHH:mm:ss.fff") + "'                             " : "")
                                                               + " ) x                                                                "
                                                               + " WHERE rowNum between " + (OnPageCount + 1) + " AND " + (OnPageCount + RequiredNumber);
            //+" WHERE (rowNum / " + RequiredNumber.ToString() + ") + 1= " + PageNumber.ToString();

            var db = new VoucherEntities();
            return db.ExecuteStoreQuery<Voucher>(query).ToList<Voucher>();

        }


        internal Voucher Create(Voucher voucher)
        {
            var db = new VoucherEntities();
            voucher.Create_Date = DateTime.Now;
            voucher.IsActive = false;
            bool isValid = false;
            string lastVoucherNumber = string.Empty;
            string countVoucher = string.Empty;
            while (!isValid)
            {
                lastVoucherNumber = db.ExecuteStoreQuery<string>("Select MAX(VoucherID) from dbo.Vouchers").ToList()[0];
                //countVoucher = db.ExecuteStoreQuery<int>("Select COUNT(VoucherID) from dbo.Vouchers Where VoucherID='" + "TNT" + (Convert.ToInt32(lastVoucherNumber.Substring(3)) + 1).ToString("D5") + "' ").ToList()[0].ToString();
                countVoucher = db.ExecuteStoreQuery<int>("Select COUNT(VoucherID) from dbo.Vouchers Where VoucherID='" + "TNTQ" + (Convert.ToInt32(lastVoucherNumber.Substring(4)) + 1).ToString("D5") + "' ").ToList()[0].ToString();
                if ((!String.IsNullOrEmpty(lastVoucherNumber)) && Convert.ToInt32(countVoucher) == 0)
                {
                    isValid = true;
                }
            }

            if (!String.IsNullOrEmpty(lastVoucherNumber))
            {
                // voucher.VoucherID = "TNT" + (Convert.ToInt32(lastVoucherNumber.Substring(3)) + 1).ToString("D5");
                voucher.VoucherID = "TNTQ" + (Convert.ToInt32(lastVoucherNumber.Substring(4)) + 1).ToString("D5");
            }
            else
            {
                //voucher.VoucherID = "TNT00001";
                voucher.VoucherID = "TNTQ00001";
            }
            //Commented by Suresh on 24-02-2015 for resolving skiping Voucher Number Issue
            //Add(voucher);

            //db.ExecuteStoreCommand(ConfigurationManager.AppSettings["InactiveVoucherCleanUpQuery"]);
            var companyRespository = new CompanyRespository();
            voucher.Companies = companyRespository.GetList(c => c.IsActive).OrderBy(x => x.Name).ToList();
            return voucher;
        }

        public List<VoucherTemporary> GetTopCompanies(Voucher voucher)
        {
            var list = new List<VoucherTemporary>();

            var db = new VoucherEntities();
            var query = "select c.Name, Sum(v.AdultCount) as AdultCount, IsNull(Sum(v.ChildrenCount),0) as ChildrenCount, IsNull(Sum(InfantCount),0) as InfantCount, Sum(v.Price) as Price, Sum(v.Commission) as Commission, Sum(v.Levy) as Levy, Sum(v.Discount) as Discount from dbo.Vouchers v inner join dbo.Companies c on v.CompanyID = c.CompanyID where 1=1 ";
            if (voucher.CompanyID != 0)
            //vouchers = vouchers.Where(x => x.CompanyID == voucher.CompanyID).ToList();
            {
                query += " and c.CompanyID=" + voucher.CompanyID;
            }
            if (!string.IsNullOrEmpty(voucher.Tour) & voucher.Tour != "0")
            //vouchers = vouchers.Where(x => x.Tour.ToLower().Contains(voucher.Tour.ToLower())).ToList();
            {
                query += " and v.Tour like '%" + voucher.Tour + "%'";
            }


            /*
        //vouchers = vouchers.Where(x => x.TravelDate >= voucher.TravelDateFrom_Report).ToList();
        {
            query += " and v.TravelDate >= '" + voucher.TravelDateFrom_Report.Value.Date.ToString("yyyy-MM-dd HH:mm:ss") + "'";
        }
        if (voucher.TravelDateTo_Report != null)
        //vouchers = vouchers.Where(x => x.TravelDate <= voucher.TravelDateTo_Report.Value.AddHours(23).AddMinutes(59)).ToList();
        {
            query += " and v.TravelDate <= '" + voucher.TravelDateFrom_Report.Value.AddHours(23).AddMinutes(59).Date.ToString("yyyy-MM-dd HH:mm:ss") + "'";
        }
        if (voucher.EnteredDateFrom_Report != null)
        //vouchers = vouchers.Where(x => x.Create_Date >= voucher.EnteredDateFrom_Report).ToList();
        {
            query += " and v.Create_Date >= '" + voucher.EnteredDateFrom_Report.Value.Date.ToString("yyyy-MM-dd HH:mm:ss") + "'";
        }
        if (voucher.EnteredDateTo_Report != null)
        //vouchers = vouchers.Where(x => x.Create_Date <= voucher.EnteredDateTo_Report.Value.AddHours(23).AddMinutes(59)).ToList();
        {
            query += " and v.Create_Date <= '" + voucher.EnteredDateTo_Report.Value.AddHours(23).AddMinutes(59).ToString("yyyy-MM-dd HH:mm:ss") + "'";
        }
        
            */

            if (voucher.TravelDateFrom_Report != null)
            {
                DateTime dtFrom = new DateTime();
                dtFrom = new DateTime(voucher.TravelDateFrom_Report.Value.Year, voucher.TravelDateFrom_Report.Value.Month, voucher.TravelDateFrom_Report.Value.Day);
                query += " and v.TravelDate >= '" + dtFrom.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            }
            if (voucher.TravelDateTo_Report != null)
            {
                DateTime dtTo = new DateTime();
                dtTo = new DateTime(voucher.TravelDateTo_Report.Value.Year, voucher.TravelDateTo_Report.Value.Month, voucher.TravelDateTo_Report.Value.Day);
                query += " and v.TravelDate <= '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            }

            if (voucher.EnteredDateFrom_Report != null)
            {
                DateTime dtFrom = new DateTime();
                dtFrom = new DateTime(voucher.EnteredDateFrom_Report.Value.Year, voucher.EnteredDateFrom_Report.Value.Month, voucher.EnteredDateFrom_Report.Value.Day);
                query += " and v.Create_Date >= '" + dtFrom.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            }
            if (voucher.EnteredDateTo_Report != null)
            {
                DateTime dtTo = new DateTime();
                dtTo = new DateTime(voucher.EnteredDateTo_Report.Value.Year, voucher.EnteredDateTo_Report.Value.Month, voucher.EnteredDateTo_Report.Value.Day);
                query += " and v.Create_Date <= '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            }
            query += " group by v.CompanyID, c.Name order by Price desc";


            //list = db.ExecuteStoreQuery<VoucherTemporary>("select c.Name, Sum(v.AdultCount) as AdultCount, IsNull(Sum(v.ChildrenCount),0) as ChildrenCount, IsNull(Sum(InfantCount),0) as InfantCount, Sum(v.Price) as Price, Sum(v.Commission) as Commission, Sum(v.Levy) as Levy, Sum(v.Discount) as Discount from dbo.Vouchers v inner join dbo.Companies c on v.CompanyID = c.CompanyID group by v.CompanyID, c.Name order by Price desc").ToList<VoucherTemporary>();
            list = db.ExecuteStoreQuery<VoucherTemporary>(query).ToList<VoucherTemporary>();
            return list;
        }
        public List<String> GetUniqueTours()
        {

            var db = new VoucherEntities();
            return db.ExecuteStoreQuery<String>("SELECT [Tour] FROM [dbo].[Vouchers] where Tour is not null group by Tour").ToList<String>();

        }


        //public List<voucherStaffList> getStaffVoucher(DateTime FromTravelDate, DateTime ToTravelDate)
        // {


        //        var query = @"SELECT (Create_By)createdBy,sum(cardPaid)totalCardPaid,sum(cashPaid)totalCashPaid,sum(Price)totalPrice 
        //                FROM [dbo].[Vouchers] where Create_Date >='" + FromTravelDate.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "' AND Create_Date<='" + ToTravelDate.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "'group  by Create_By";
        //        var db = new VoucherEntities();
        //        return db.ExecuteStoreQuery<voucherStaffList>(query).ToList<voucherStaffList>();
        //}



        public List<voucherStaffList> getStaffVoucher(DateTime FromTravelDate, DateTime ToTravelDate)
        {


            var query = @"SELECT (Create_By)createdBy,sum(cardPaid)totalCardPaid,sum(cashPaid)totalCashPaid,sum(Price)totalPrice 
                        FROM [dbo].[Vouchers] where CAST(Create_Date AS DATE) >='" + FromTravelDate.ToString("yyyy-MM-dd") + "' AND CAST(Create_Date AS DATE)<='" + ToTravelDate.ToString("yyyy-MM-dd") + "'group  by Create_By";
            var db = new VoucherEntities();
            return db.ExecuteStoreQuery<voucherStaffList>(query).ToList<voucherStaffList>();
        }


        //Changes Made on 02-29-2016
        public List<voucherGraph> getVoucherCount(DateTime FromTravelDate, DateTime ToTravelDate)
        {
            var query = @"select count(*)  as [Count],DATEPART(hour,Create_Date) AS [Hour] from [dbo].[Vouchers]" 
                        + "where IsActive = 1 and CAST(Create_Date AS DATE) >= CAST('" + FromTravelDate.ToString("yyyy-MM-dd") 
                        + "' AS DATE) and CAST(Create_Date AS DATE) <=  CAST('" + ToTravelDate.ToString("yyyy-MM-dd") 
                        + "' AS DATE) group by DATEPART(HOUR,Create_Date)";
            var db = new VoucherEntities();
            return db.ExecuteStoreQuery<voucherGraph>(query).ToList<voucherGraph>();
        }

        public List<voucherGraph> getBookingCount(DateTime FromTravelDate, DateTime ToTravelDate)
        {
            try
            {
                List<voucherGraph> lstbookingGraph = new List<voucherGraph>();
                mysqlconnection.Open();
                mysqlcmd = mysqlconnection.CreateCommand();
                mysqlcmd.CommandText = "SELECT HOUR(createdDate)Hour,count(*)Count FROM bookings WHERE isDeleted = 0 AND DATE(createdDate) >= @createdDate and DATE(createdDate) <= @createdEndDate GROUP BY HOUR(createdDate)";
                mysqlcmd.Parameters.AddWithValue("@createdDate", FromTravelDate.Year + "-" + FromTravelDate.Month + "-" + FromTravelDate.Day);
                mysqlcmd.Parameters.AddWithValue("@createdEndDate", ToTravelDate.Year + "-" + ToTravelDate.Month + "-" + ToTravelDate.Day);
                mysqldr = mysqlcmd.ExecuteReader();
                while (mysqldr.Read())
                {
                    voucherGraph vg = new voucherGraph();
                    vg.Hour = mysqldr.GetInt16(0);
                    vg.Count = mysqldr.GetInt16(1);
                    lstbookingGraph.Add(vg);
                }
                mysqldr.Close();
                return lstbookingGraph;

            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (mysqlconnection.State == System.Data.ConnectionState.Open)
                    mysqlconnection.Close();
            }

        }
        //End Changes

        public List<voucherStaffList> getStaffBooking(DateTime FromTravelDate, DateTime ToTravelDate)
        {

            try
            {
                List<voucherStaffList> lstvoucherStaffList = new List<voucherStaffList>();
                mysqlconnection.Open();
                mysqlcmd = mysqlconnection.CreateCommand();
                //mysqlcmd.CommandText = "Select * from bookings as b,tourcode as t,tournames as tn where b.uid=@uid and b.createdDate=@createdDate and t.id=b.tourcode and t.tourid=tn.id";
                //mysqlcmd.CommandText = "Select (uid)createdBy,sum(cardPaid)totalCardPaid,sum(cashPaid)totalCashPaid,sum(Price)totalPrice from bookings where createdDate>=@createdDate  and  createdDate<=@createdEndDate";
                //Select CONCAT (u.id , ',', 'admin', ',', u.usertype, CHAR(10)) createdBy,sum(cardPaid)totalCardPaid,sum(cashPaid)totalCashPaid,sum(Price)totalPrice from bookings b inner join users u on u.id = b.uid where createdDate>= '2016-02-04' and createdDate<= '2016-02-04'
                mysqlcmd.CommandText = "Select CONCAT(CONVERT(u.id,char) , ',' ,u.username, ',' , CONVERT(u.usertype,CHAR)) createdBy, sum(cardPaid)totalCardPaid,sum(cashPaid)totalCashPaid,sum(Price)totalPrice from bookings b inner join users u on u.id = b.uid where CAST(createdDate AS DATE) >=@createdDate  and  CAST(createdDate AS DATE) <=@createdEndDate group by b.uid";
                mysqlcmd.Parameters.AddWithValue("@createdDate", FromTravelDate.Year + "-" + FromTravelDate.Month + "-" + FromTravelDate.Day);
                mysqlcmd.Parameters.AddWithValue("@createdEndDate", ToTravelDate.Year + "-" + ToTravelDate.Month + "-" + ToTravelDate.Day);

                mysqldr = mysqlcmd.ExecuteReader();
                while (mysqldr.Read())
                {

                    voucherStaffList vst = new voucherStaffList();
                    vst.createdBy = mysqldr.GetString(0);
                    vst.totalCardPaid = mysqldr.GetDecimal(1);
                    vst.totalCashPaid = mysqldr.GetDecimal(2);
                    vst.totalPrice = mysqldr.GetDecimal(3);
                    lstvoucherStaffList.Add(vst);
                }
                mysqldr.Close();
                return lstvoucherStaffList;
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (mysqlconnection.State == System.Data.ConnectionState.Open)
                    mysqlconnection.Close();

            }
        }



        /**
         *Staff wise Report 
         *Voucher wise shop Report 
         */
        /*------------Start on 23-09-2015 ---------------------*/
        public DataTable VoucherBookingStaffWiseReport(DateTime FromTravelDate, DateTime ToTravelDate)
        {
            List<voucherStaffList> lstStaffwisereport = new List<voucherStaffList>();
            lstStaffwisereport = getStaffVoucher(FromTravelDate, ToTravelDate);

            DataTable dtStaffSupport = new DataTable();
            dtStaffSupport.Columns.Add("createdBy");
            dtStaffSupport.Columns.Add("totalCardPaid");
            dtStaffSupport.Columns.Add("totalCashPaid");
            dtStaffSupport.Columns.Add("totalPrice");


            foreach (voucherStaffList vst in lstStaffwisereport)
            {
                DataRow dr = dtStaffSupport.NewRow();
                dr["createdBy"] = vst.createdBy;
                dr["totalCardPaid"] = vst.totalCardPaid;
                dr["totalCashPaid"] = vst.totalCashPaid;
                dr["totalPrice"] = vst.totalPrice;
                dtStaffSupport.Rows.Add(dr);
            }
            return dtStaffSupport;

            //Changes Made on 3-2-2016

            List<voucherStaffList> abcd = new List<voucherStaffList>();
            abcd = getStaffBooking(FromTravelDate, ToTravelDate);

            DataTable dtAdminSupport = new DataTable();
            dtAdminSupport.Columns.Add("createdBy");
            dtAdminSupport.Columns.Add("totalCardPaid");
            dtAdminSupport.Columns.Add("totalCashPaid");
            dtAdminSupport.Columns.Add("totalPrice");

            foreach(voucherStaffList lst in abcd)
            {
                DataRow dr1 = dtAdminSupport.NewRow();
                dr1["createdBy"] = lst.createdBy;
                dr1["totalCardPaid"] = lst.totalCardPaid;
                dr1["totalCashPaid"] = lst.totalCashPaid;
                dr1["totalPrice"] = lst.totalPrice;
                dtAdminSupport.Rows.Add(dr1);
            }
            return dtAdminSupport;

        }
        /*----------End  --------------------------------------*/


    }
}