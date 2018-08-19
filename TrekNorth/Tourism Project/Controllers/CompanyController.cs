using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Tourism_Project.Models;

namespace Tourism_Project.Controllers
{
    public class CompanyController : Controller
    {
        private CompanyRespository repository = new CompanyRespository();

        //
        // GET: /Company/

        public ActionResult Index(string Name, string Phone, string Comission)
        {
            List<Company> companies = repository.GetList(x => x.IsActive == true);
            if (!string.IsNullOrEmpty(Name))
            {
                companies = companies.Where(v => v.Name.Trim().ToLower().Contains(Name.Trim().ToLower())).ToList();
                @ViewBag.Name = Name;
            }
            if (!string.IsNullOrEmpty(Phone))
            {
                companies = companies.Where(v => v.Phone.ToLower().Trim().Contains(Phone.ToLower().Trim())).ToList();
                @ViewBag.Phone = Phone;
            }
            decimal temp;
            if (!string.IsNullOrEmpty(Comission) && decimal.TryParse(Comission, out temp))
            {
                companies = companies.Where(v => v.Commission == temp).ToList();
                @ViewBag.Comission = Comission;
            }
            return View(companies);
        }

        //
        // GET: /Company/Details/5

        public ActionResult Details(int id = 0)
        {
            Company company = repository.Get(x=>x.CompanyID == id);
            if (company == null)
            {
                return HttpNotFound();
            }
            return View(company);
        }

        //
        // GET: /Company/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Company/Create

        [HttpPost]
        public ActionResult Create(Company company)
        {
            ModelState.Remove(ModelState.Where(x => x.Key == "CompanyID").First());
            company.CompanyID = 0;
            if (ModelState.IsValid)
            {
                company.Create_By = "admin";
                company.Create_Date = DateTime.Now.Date;
                company.IsActive = true;
                repository.Add(company);
                return RedirectToAction("Index");
            }

            return View(company);
        }

        //
        // GET: /Company/Edit/5

        public ActionResult Edit(int id = 0)
        {
            Company company = repository.Get(x=>x.CompanyID == id);
            if (company == null)
            {
                return HttpNotFound();
            }
            return View(company);
        }

        //
        // POST: /Company/Edit/5

        [HttpPost]
        public ActionResult Edit(Company company)
        {
            if (ModelState.IsValid)
            {
                Company c = repository.Get(x=>x.CompanyID == company.CompanyID);
                if (c == null)
                {
                    return HttpNotFound();
                }
                c.Commission = company.Commission;
                c.Email = company.Email;
                c.Name = company.Name;
                c.Phone = company.Phone;

                c.Modify_By = "admin";
                c.Modify_Date = DateTime.Now.Date;

                repository.Update(c);
                return RedirectToAction("Index");
            }
            return View(company);
        }

        //
        // GET: /Company/Delete/5

        public ActionResult Delete(int id = 0)
        {
            Company company = repository.Get(x=>x.CompanyID == id);
            if (company == null)
            {
                return HttpNotFound();
            }
            return View(company);
        }

        //
        // POST: /Company/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Company company = repository.Get(x=>x.CompanyID == id);
            company.IsActive = false;
            repository.Update(company);
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        [HttpGet, ActionName("Commision")]
        public decimal? Commision(int id)
        {
            Company company = repository.Get(x => x.CompanyID == id);
            return company.Commission;
        }

        [HttpGet, ActionName("CommisionAndPhone")]
        public string CommisionAndPhone(int id)
        {
            Company company = repository.Get(x => x.CompanyID == id);
            //return company.Commission;
            return "{\"Details\":[{\"Commission\":\"" + ((company.Commission == null) ? "0" : company.Commission.ToString()) + "\", \"Phone\":\"" + ((company.Phone == null) ? "0" : company.Phone.ToString()) + "\"}]}";
        }

    }
}