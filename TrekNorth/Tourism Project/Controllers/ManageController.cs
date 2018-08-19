using Ionic.Zip;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
namespace Tourism_Project.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        //
        // GET: /Manage/
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

        public ManageController()
        {
            connection = new MySqlConnection(connString);
            cmd = new MySqlCommand();
            dr = null;
        }

        
        public ActionResult Index()
        {
            //TempData.Keep();
            return View();
        }

        public ActionResult Backup()
        {
            getVoucherBackup();
            getTourBackup();
            TempData["IsSucceess"] = "1";
            TempData.Keep();
            return RedirectToAction("Index");
        }

        private void getVoucherBackup()
        {
            SqlConnection sqlcon = new SqlConnection();
            SqlCommand sqlcmd = new SqlCommand();
            SqlDataAdapter da = new SqlDataAdapter();
            DataTable dt = new DataTable();

            //Mentioned Connection string make sure that user id and password sufficient previlages
            sqlcon.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["VoucherConnection"].ConnectionString;

            //Enter destination directory where backup file stored
            string destdir = Server.MapPath("~") + "DB_Backup\\" + DateTime.Now.ToString("dd-MM-yyyy") + "\\Voucher\\";

            bool folderExist = System.IO.Directory.Exists(destdir);

            if (!folderExist)
            {
                System.IO.Directory.CreateDirectory(Server.MapPath("~\\DB_Backup\\" + DateTime.Now.ToString("dd-MM-yyyy") + "\\Voucher"));
            }
                        
            string fileName = "DB6823_Voucher-DBBackup-" + DateTime.Now.ToString("dd-MM-yyyy_H_mm_ss");

            sqlcon.Open();
            sqlcmd = new SqlCommand("backup database DB6823_Voucher to disk='" + destdir + fileName + ".Bak'", sqlcon);
            sqlcmd.ExecuteNonQuery();
            sqlcon.Close();

            using (ZipFile zip = new ZipFile())
            {
                zip.UseZip64WhenSaving = Zip64Option.Always;  // utf-8                  
                zip.AddFile(destdir + fileName + ".Bak", "");
                zip.Save(destdir + fileName + ".zip");
                if (System.IO.File.Exists(destdir + fileName + ".Bak"))
                {
                    System.IO.File.Delete(destdir + fileName + ".Bak");
                }
            }

        }
        private void getTourBackup()
        {
            connection = new MySqlConnection(connString);
            cmd = new MySqlCommand();
            
            //Enter destination directory where backup file stored
            string destdir = Server.MapPath("~") + "DB_Backup\\" + DateTime.Now.ToString("dd-MM-yyyy") + "\\Tour\\";

            bool folderExist = System.IO.Directory.Exists(destdir);

            if (!folderExist)
            {
                System.IO.Directory.CreateDirectory(Server.MapPath("~\\DB_Backup\\" + DateTime.Now.ToString("dd-MM-yyyy") + "\\Tour"));
            }
                       

            string fileName = destdir + "db6823_tourism-DBBackup-" + DateTime.Now.ToString("dd-MM-yyyy_H_mm_ss");

            using (MySqlBackup mb = new MySqlBackup(cmd))
            {
                cmd.Connection = connection;
                connection.Open();
                mb.ExportToFile(fileName + ".sql");
                connection.Close();
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.UseZip64WhenSaving = Zip64Option.Always;  // utf-8                  
                zip.AddFile(fileName + ".sql", "");
                zip.Save(fileName + ".zip");
                if (System.IO.File.Exists(fileName + ".sql"))
                {
                    System.IO.File.Delete(fileName + ".sql");
                }
            }

        }

    }
}
