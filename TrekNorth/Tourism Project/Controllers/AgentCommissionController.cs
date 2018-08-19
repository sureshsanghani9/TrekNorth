using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tourism_Project.Models;

namespace Tourism_Project.Controllers
{
    public class AgentCommissionController : Controller
    {
        //
        // GET: /AgentCommission/

        public ActionResult Index()
        {
            var bookingRepository = new BookingRepository();
            var agentTourCommissionRepository = new AgentTourCommissionRepository();

            List<SelectListItem> selectListItems1 = new List<SelectListItem>();
            SelectListItem item = new SelectListItem();
            foreach (BookingModel c in bookingRepository.GetTourCodes())
            {
                item = new SelectListItem();
                item.Value = c.Tour.ToString();
                item.Text = c.tourcodevalues;
                selectListItems1.Add(item);
            }
            ViewBag.TourCodes = selectListItems1;

            List<SelectListItem> selectListItems2 = new List<SelectListItem>();
            foreach (RegisterModel c in bookingRepository.GetUsers())
            {
                item = new SelectListItem();
                item.Value = c.ID.ToString();
                item.Text = c.Name;
                selectListItems2.Add(item);
            }
            ViewBag.Users = selectListItems2;

            var agentTourCommissions = agentTourCommissionRepository.GetList();
            return View(agentTourCommissions);
        }


        /* comment on  04-08-2015
        [HttpPost]
        public JsonResult submitAgentTourCommissions(List<AgentTourCommission> obj)
        {
            try
            {
                var rep = new AgentTourCommissionRepository();
               
                foreach (var ob in obj)
                {
                    if (ob.Commission != null)
                    {
                        var temp = rep.Get(ob.UserID, ob.TourCodeID);
                        if (temp.Commission != 0 && temp.Commission != null)
                        {
                            //ob.active_f = 1;  //on 08-04-2015
                            rep.Update(ob);
                        }
                        else
                        {
                            //ob.active_f = 1; //on 08-04-2015
                            rep.Insert(ob);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(ex.Message + " " + ex.InnerException);
            }
            return Json(true);

        }
         * 
         * */


        [HttpPost]
        public JsonResult submitAgentTourCommissions(List<AgentTourCommission> obj)
        {
            try
            {
                var rep = new AgentTourCommissionRepository();

                foreach (var ob in obj)
                {
                    if (ob.Commission != null)
                    {
                        var temp = rep.Get(ob.UserID, ob.TourCodeID);
                        if (temp.Commission != 0 && temp.Commission != null)
                        {
                            //ob.active_f = 1;  //on 08-04-2015
                            rep.Update(ob);
                        }
                        else
                        {
                            //ob.active_f = 1; //on 08-04-2015
                            if (ob.active_f == 1)
                            {
                                rep.Insert(ob);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(ex.Message + " " + ex.InnerException);
            }
            return Json(true);

        }

        [HttpPost]
        public JsonResult getAgentTourCommissions(int UserID)
        {
            var repository = new AgentTourCommissionRepository();
            return Json(repository.GetList(UserID));

        }


        [HttpPost]
        public JsonResult getAgentTourCommission(AgentTourCommission obj)
        {
            var repository = new AgentTourCommissionRepository();
            var result = repository.Get(obj.UserID, obj.TourCodeID);
            if (result.Commission != null && result.Commission != 0)
            {
                return Json(result.Commission);
            }
            else
            {
                var rep = new BookingRepository();
                return Json(rep.GetCommission(obj.UserID));
            }
        }


        [HttpPost]
        public JsonResult getActiveTourCodes()
        {
            var repository = new BookingRepository();
            return Json(repository.GetTourCodes());

        }

    }
}
