using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tourism_Project.Models;

namespace Tourism_Project.Controllers
{
    public class TourCodePriceController : Controller
    {
        //
        // GET: /TourCodePrice/
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
            }
        }

        public ActionResult Index()
        {
            if (userType == 1)
            {
                var rep = new TourCodePricesRepository();
                return View(rep.GetList());
            }
            else
                return RedirectToAction("AddBookingB", "Booking");
        }


        [HttpPost]
        public JsonResult SaveChanges(List<TourCodePrice> list)
        {
            var rep = new TourCodePricesRepository();
            rep.UpdateList(list);
            return Json(true);
        }

        [HttpGet]
        public ActionResult GetTourPrice(int TourCodeID)
        {
            var rep = new TourCodePricesRepository();
            return Json(rep.Get(TourCodeID), JsonRequestBehavior.AllowGet);
        }

    }
}
