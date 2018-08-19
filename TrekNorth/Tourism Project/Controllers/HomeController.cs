using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Tourism_Project.Models;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.Web.Configuration;
using System.Globalization;

namespace Tourism_Project.Controllers
{
    public class HomeController : Controller
    {
        List<BookingModel> models = new List<BookingModel>();
        public ActionResult Index()
        {

            var repository = new BookingRepository();
            ViewBag.Tours = PrepareToursDropDown(repository.GetTourNames());
            ViewBag.TourCodes = PrepareTourCodesDropDown(repository.GetTourCodes(1));
      

            ViewBag.Message = "Trek North";
            HttpCookie cookie = Request.Cookies.Get("TntqTrackit");

            if(!String.IsNullOrEmpty(User.Identity.Name))
            {
                string[] user = User.Identity.Name.Split(',');
                if (Int32.Parse(user[2]) == 5)
                {
                    return RedirectToAction("Index", "Voucher");
                }
            }

            if (String.IsNullOrEmpty(User.Identity.Name) && cookie != null)
            {
                return RedirectToAction("Login", "Account");
            }
         
            return View();
        }

        public List<SelectListItem> PrepareTourCodesDropDown(List<BookingModel> models)
        {
            return models.Select(c => new SelectListItem { Value = c.TourCode.ToString(CultureInfo.InvariantCulture), Text = c.tourcodevalues }).ToList();
        }

        public List<SelectListItem> PrepareToursDropDown(List<BookingModel> models)
        {
            return models.Select(c => new SelectListItem { Value = c.Tour.ToString(System.Globalization.CultureInfo.InvariantCulture), Text = c.tourname }).ToList();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Schedule()
        {
            DataTable demo = new DataTable();
            List<JsonData> data = new List<JsonData>();
            List<JsonData> dataAdmin = new List<JsonData>();
            string cs = ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;
            try
            {
                SqlConnection cnn = new SqlConnection(cs);
                cnn.Open();
                SqlCommand cmd = new SqlCommand("select * from demo", cnn);
                SqlDataReader dr = cmd.ExecuteReader();
             

                while (dr.Read())
                {
                    JsonData obj = new JsonData
                    {
                        rowBoundIndex = dr["row"].ToString(),
                        dataField = dr["col"].ToString(),
                        value = dr["data"].ToString(),
                        colors = dr["colors"].ToString()
                    };
                    data.Add(obj);
                }
                dr.Close();


                SqlCommand cmd1 = new SqlCommand("select * from AdminSpreadsheet", cnn);
                SqlDataReader dr1 = cmd1.ExecuteReader();
                while (dr1.Read())
                {
                    JsonData obj = new JsonData
                    {
                        rowBoundIndex = dr1["row"].ToString(),
                        dataField = dr1["cols"].ToString(),
                        value = dr1["data"].ToString(),
                        colors = dr1["colors"].ToString()
                    };
                    dataAdmin.Add(obj);

                }
                dr1.Close();
                //cnn.Close();

            }
            catch (Exception)
            {

                throw;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"data\":[");

            for (int Row = 0; Row < 100; Row++)
            {
                string color = "color";
                sb.Append('{');
                for (char Col = 'A'; Col <= 'Z'; Col++)
                {
                    var item = data.Where(x => Convert.ToInt32(x.rowBoundIndex).Equals(Row) && x.dataField.Trim().Equals(Col.ToString())).FirstOrDefault();
                    if (item != null)
                    {
                        if (Col != 'Z')
                        {
                            var str = "\"" + Col + "\":" + "\"" + item.value + "\"," + "\"" + color + "\":" + "\"" + item.colors + "\",".Trim();
                            //var str = "\"" + Col + "\":" + "\"" + item.value + "\",".Trim();
                            sb.Append(str);
                        }
                        else {
                            var str = "\"" + Col + "\":" + "\"" + item.value + "\"," + "\"" + color + "\":" + "\"" + item.colors + "\"".Trim();
                           // var str = "\"" + Col + "\":" + "\"" + item.value + "\"".Trim();
                            sb.Append(str);
                        }
                    }
                    else {
                        if (Col != 'Z')
                        {
                            var str = "\"" + Col + "\":" + "\"\",".Trim();
                            sb.Append(str);
                        }
                        else
                        {
                            var str = "\"" + Col + "\":" + "\"\"".Trim();
                            sb.Append(str);
                        }

                    }

                }
                if (Row != 99)
                {
                    sb.Append("},");
                }
                else {
                    sb.Append("}");
                }
            }

            sb.Append("]}");
            ViewBag.GridValues = sb.ToString();
            
            //Admin Notes
            StringBuilder sb1 = new StringBuilder();
            sb1.Append("{\"data\":[");

            for (int Row = 0; Row < 100; Row++)
            {
                sb1.Append('{');
                for (char Col = 'A'; Col <= 'Z'; Col++)
                {
                    string color = "color";
                    var item = dataAdmin.Where(x => Convert.ToInt32(x.rowBoundIndex).Equals(Row) && x.dataField.Trim().Equals(Col.ToString())).FirstOrDefault();
                    if (item != null)
                    {
                       
                        if (Col != 'Z')
                        {
                           // var str = "\"" + Col + "\":" + "\"" + item.value + "\",".Trim();
                            var str = "\"" + Col + "\":" + "\"" + item.value + "\"," + "\"" + color + "\":" + "\"" + item.colors + "\",".Trim();
                            sb1.Append(str);
                        }
                        else {
                           // var str = "\"" + Col + "\":" + "\"" + item.value + "\"".Trim();
                             var str = "\"" + Col + "\":" + "\"" + item.value + "\"," + "\"" + color + "\":" + "\"" + item.colors + "\"".Trim();
                            sb1.Append(str);
                        }
                    }
                    else {
                        if (Col != 'Z')
                        {
                             var str = "\"" + Col + "\":" + "\"\",".Trim();
                            //var str = "\"" + Col + "\":" + "\"\"," + "\"" + color + "\":" + "\"\",".Trim();
                            sb1.Append(str);
                        }
                        else
                        {
                             var str = "\"" + Col + "\":" + "\"\"".Trim();
                            //var str = "\"" + Col + "\":" + "\"\"," + "\"" + color + "\":" + "\"\"".Trim();
                            sb1.Append(str);
                        }
                    }

                }
                if (Row != 99)
                {
                    sb1.Append("},");
                }
                else
                {
                    sb1.Append("}");
                }
            }
            sb1.Append("]}");
            ViewBag.AdminGridValues = sb1.ToString();

            return View();
        }

        [HttpPost]
        public void getdata(FormCollection form)
        {

            demo dd = new demo
            {
                row = form["obj[rowBoundIndex]"],
                col = form["obj[dataField]"],
                data = form["obj[value]"],
            };
            string cs = ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;
            string sql = "select count(*) from demo where row=" + dd.row + " and col='" + dd.col + "'";
            SqlConnection cnn = new SqlConnection(cs);
            cnn.Open();
            SqlCommand cmd = new SqlCommand(sql, cnn);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            dd.data = dd.data.TrimEnd().TrimStart();
            if (string.IsNullOrEmpty(dd.data))
            {
                dd.data = string.Empty;
            }
            else {
                dd.data = dd.data;
            }
            if (count != 0)
            {

                sql = "UPDATE [dbo].[demo] SET data = '" + dd.data.Trim() + "' where row=" + dd.row + " and col='" + dd.col + "'";
                cmd.CommandText = sql;
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }
            else {
                sql = "INSERT INTO [dbo].[demo] ([row] ,[col] ,[data]) VALUES (" + dd.row + ",'" + dd.col + "','" + dd.data.Trim() + "')";
                cmd.CommandText = sql;
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }

            cnn.Close();
        }
        [HttpPost]
        public void SaveScheduleFormat(FormCollection form)
        {
            ScheduleSaveFormat dd = new ScheduleSaveFormat
            {
                row = form["obj[row]"],
                col = form["obj[col]"],
                colors = form["obj[colors]"],
            };
            string cs = ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;
            string sql = "select count(*) from demo where row=" + dd.row + " and col='" + dd.col + "'";
            SqlConnection cnn = new SqlConnection(cs);
            cnn.Open();
            SqlCommand cmd = new SqlCommand(sql, cnn);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            dd.colors = dd.colors.ToString();
            if (count != 0)
            {
                //sql = "UPDATE [dbo].[AdminSpreadsheet] SET data = '" + dd.data.Trim() + "' where row=" + dd.row + " and cols='" + dd.cols + "'";
                sql = "UPDATE [dbo].[demo] SET colors ='" + dd.colors + "' where row=" + dd.row + " and col='" + dd.col + "'";
                cmd.CommandText = sql;
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }
            else {
                // sql = "INSERT INTO [dbo].[AdminSpreadsheet] ([row] ,[cols] ,[data],[colors]) VALUES (" + dd.row + ",'" + dd.cols + "','" + dd.data.Trim() + "')";
                sql = "INSERT INTO [dbo].[demo] ([row] ,[col] ,[colors]) VALUES (" + dd.row + ",'" + dd.col + "','" + dd.colors + "')";
                cmd.CommandText = sql;
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }

            cnn.Close();
        }

        [HttpPost]
        public JsonResult LoadScheduleStyle()
        {
            List<JsonData> data = new List<JsonData>();
            string rcfield = string.Empty;
            string colors = string.Empty;
            string cs = ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;
            SqlConnection cnn = new SqlConnection(cs);
            cnn.Open();
            //string sql = "select CONCAT(row,cols)rcfield,colors from [dbo].[AdminSpreadsheet];";
            string sql = "select (col+row)rcfield,colors,row,col from demo";
            SqlCommand cmd = new SqlCommand(sql, cnn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                JsonData obj = new JsonData
                {
                    row = dr["row"].ToString(),
                    col = dr["col"].ToString(),
                    rcfield = dr["rcfield"].ToString().Trim(),
                    colors = dr["colors"].ToString(),

                };
                data.Add(obj);
            }
            dr.Close();
            cnn.Close();
            return Json(data, JsonRequestBehavior.AllowGet);
        }


        //Admin Notes
        [HttpPost]
        public void postdata(FormCollection form)
        {
            AdminSpreadsheet dd = new AdminSpreadsheet
            {
                row = form["obj[rowBoundIndex]"],
                cols = form["obj[dataField]"],
                 data = form["obj[value]"]
                //data = "<span style = margin: 4px ; color:#000000;font-weight:bold> testing</span>",
                //colors = form["obj[foreColor]"],
            };
            string cs = ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;
            string sql = "select count(*) from AdminSpreadsheet where row=" + dd.row + " and cols='" + dd.cols + "'";
            SqlConnection cnn = new SqlConnection(cs);
            cnn.Open();
            SqlCommand cmd = new SqlCommand(sql, cnn);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            dd.data = dd.data.TrimEnd().TrimStart();
            //dd.colors = dd.colors.ToString();
            if (string.IsNullOrEmpty(dd.data))
            {
                dd.data = string.Empty;
            }
            else {
                dd.data = dd.data;
            }
            if (count != 0)
            {
               //sql = "UPDATE [dbo].[AdminSpreadsheet] SET data = '" + dd.data.Trim() + "' where row=" + dd.row + " and cols='" + dd.cols + "'";
                sql = "UPDATE [dbo].[AdminSpreadsheet] SET data = '" + dd.data.Trim() + "' where row=" + dd.row + " and cols='" + dd.cols + "'";
                cmd.CommandText = sql;
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }
            else {
               // sql = "INSERT INTO [dbo].[AdminSpreadsheet] ([row] ,[cols] ,[data],[colors]) VALUES (" + dd.row + ",'" + dd.cols + "','" + dd.data.Trim() + "')";
                sql = "INSERT INTO [dbo].[AdminSpreadsheet] ([row] ,[cols] ,[data]) VALUES (" + dd.row + ",'" + dd.cols + "','" + dd.data.Trim() + "')";
                cmd.CommandText = sql;
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }
            cnn.Close();

        }

        [HttpPost]
        public void SaveFormat(FormCollection form)
        {
            AdminSaveFormat dd = new AdminSaveFormat
            {
                row = form["obj[row]"],
                cols = form["obj[cols]"],
                colors = form["obj[colors]"],
            };
            string cs = ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;
            string sql = "select count(*) from AdminSpreadsheet where row=" + dd.row + " and cols='" + dd.cols + "'";
            SqlConnection cnn = new SqlConnection(cs);
            cnn.Open();
            SqlCommand cmd = new SqlCommand(sql, cnn);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            dd.colors = dd.colors.ToString();
            if (count != 0)
            {
                //sql = "UPDATE [dbo].[AdminSpreadsheet] SET data = '" + dd.data.Trim() + "' where row=" + dd.row + " and cols='" + dd.cols + "'";
                sql = "UPDATE [dbo].[AdminSpreadsheet] SET colors ='" + dd.colors + "' where row=" + dd.row + " and cols='" + dd.cols + "'";
                cmd.CommandText = sql;
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }
            else {
                // sql = "INSERT INTO [dbo].[AdminSpreadsheet] ([row] ,[cols] ,[data],[colors]) VALUES (" + dd.row + ",'" + dd.cols + "','" + dd.data.Trim() + "')";
                sql = "INSERT INTO [dbo].[AdminSpreadsheet] ([row] ,[cols] ,[colors]) VALUES (" + dd.row + ",'" + dd.cols + "','" + dd.colors + "')";
                cmd.CommandText = sql;
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }

            cnn.Close();
        }

      

        [HttpPost]
        public JsonResult setcolorfont(string row, string column)
        {
            //List<JsonData> dataAdmin = new List<JsonData>();
            string colors = string.Empty;
            string cs = ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;
            SqlConnection cnn = new SqlConnection(cs);
            cnn.Open();
            //string sql = "select colors from AdminSpreadsheet where row=" + row + " and cols='A'";
            string sql = "select colors from AdminSpreadsheet where row ='" + row + "' and cols = '" + column + "'";
            //string sql = "select * from AdminSpreadsheet";
            SqlCommand cmd = new SqlCommand(sql, cnn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                colors = dr["colors"].ToString();
            }
            dr.Close();
            cnn.Close();
            return Json(colors,JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult LoadStyle()
        {
            List<JsonData> dataAdmin = new List<JsonData>();
            string rcfield = string.Empty;
            string colors = string.Empty;
            string cs = ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;
            SqlConnection cnn = new SqlConnection(cs);
            cnn.Open();
            //string sql = "select CONCAT(row,cols)rcfield,colors from [dbo].[AdminSpreadsheet];";
            string sql = "select (cols+row)rcfield,colors,row,cols from AdminSpreadsheet";
            SqlCommand cmd = new SqlCommand(sql, cnn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                JsonData obj = new JsonData
                {
                    row = dr["row"].ToString(),
                    col = dr["cols"].ToString(),
                    rcfield = dr["rcfield"].ToString().Trim(),
                    colors = dr["colors"].ToString(),

                };
                dataAdmin.Add(obj);
            }
            dr.Close();
            cnn.Close();
            return Json(dataAdmin,JsonRequestBehavior.AllowGet);
        }

    }

}
