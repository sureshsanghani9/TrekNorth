using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Tourism_Project
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterAuth();
            var log4NetPath = Server.MapPath("~/log4net.config");
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(log4NetPath));

            //CultureInfo info = new CultureInfo(System.Threading.Thread.CurrentThread.CurrentCulture.ToString());
            //info.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            //System.Threading.Thread.CurrentThread.CurrentCulture = info;
        }

        void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError().GetBaseException();
            log.Error(ex.Message, ex);
        }

        void Application_BeginRequest(object sender, EventArgs e)
        {
            ////////CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ////////culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            ////////culture.DateTimeFormat.LongTimePattern = "";
            ////////Thread.CurrentThread.CurrentCulture = culture;
            //CultureInfo info = new CultureInfo(System.Threading.Thread.CurrentThread.CurrentCulture.ToString());
            //info.DateTimeFormat.ShortDatePattern = "dd-MM-yyyy";
            //System.Threading.Thread.CurrentThread.CurrentCulture = info;
        }
    }
}