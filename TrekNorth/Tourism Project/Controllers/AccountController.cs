using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using WebMatrix.WebData;
using Tourism_Project.Filters;
using Tourism_Project.Models;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Web.Configuration;
using System.IO;

namespace Tourism_Project.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        static string connString = WebConfigurationManager.AppSettings["ServerDBConnection"].ToString();

        private MySqlConnection connection;
        private MySqlCommand cmd;
        public AccountController()
        {
            connection = new MySqlConnection(connString);
            cmd = new MySqlCommand();
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            HttpCookie cookie = Request.Cookies.Get("TntqTrackit");

            if (cookie != null)
            {
                string value = EncryptionManager.DecryptRijndael(cookie.Value);
                connection.Open();
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from users where username='" + value.Split(',')[1] + "' AND (active = 1 OR active is null)";
                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    //int a = dr.GetInt32(8);

                    FormsAuthentication.SetAuthCookie(value, true);
                    Session["ShowVouchers"] = dr.GetBoolean(13);
                    if (string.IsNullOrEmpty(returnUrl))
                        return RedirectToAction("AddBookingB", "Booking");
                    return RedirectToLocal(returnUrl);
                }

            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    connection.Open();
                    MySqlCommand cmd = connection.CreateCommand();
                    cmd.CommandText = "Select * from users where username='" + model.UserName + "' And password='" + model.Password + "' AND (active = 1 OR active is null)";
                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {



                        //FormsAuthentication.SignOut();
                        FormsAuthentication.SetAuthCookie(string.Format("{0},{1},{2}", dr.GetInt32(0), model.UserName, dr.GetInt32(9)), model.RememberMe);

                        RemoveCookie("TntqTrackit", Request, Response);
                        if (model.RememberMe)
                        {
                            var cookie = new HttpCookie("TntqTrackit", EncryptionManager.EncryptRijndael(string.Format("{0},{1},{2}", dr.GetInt32(0), model.UserName, dr.GetInt32(9)))) { };
                            cookie.Expires = DateTime.Now.AddMonths(6);
                            Response.Cookies.Add(cookie);
                        }
                        Session["ShowVouchers"] = dr.GetBoolean(13);


                        var usertype = dr.GetInt32(9);
                        if (usertype == 4)
                        {
                            return RedirectToAction("Reports", "Booking");
                        }
                        if(usertype == 5)
                        {
                            return RedirectToAction("Index", "Voucher");
                        }

                        if (string.IsNullOrEmpty(returnUrl))
                        {
                            return RedirectToAction("AddBookingB", "Booking");

                        }
                        RedirectToLocal(returnUrl);
                    }

                    ModelState.AddModelError("", "The user name or password provided is incorrect.");

                }
                catch (Exception e)
                {

                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
            }
            return View(model);
        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            RemoveCookie("TntqTrackit", Request, Response);
            Session.Remove("ShowVouchers");
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        private void RemoveCookie(string p, HttpRequestBase Request, HttpResponseBase Response)
        {
            HttpCookie currentUserCookie = Request.Cookies[p];
            if (currentUserCookie != null)
            {
                Response.Cookies.Remove(p);
                currentUserCookie.Expires = DateTime.Now.AddDays(-10);
                currentUserCookie.Value = null;
                Response.SetCookie(currentUserCookie);
            }
        }

        public ActionResult EditRegister(int id)
        {
            RegisterModel model = null;
            bool isError = false;
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "Select * from users where id='" + id + "';";
                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    model = new RegisterModel
                    {
                        ID = id,
                        Name = dr.GetString(1),
                        Address = dr.GetString(2),
                        Phone = dr.GetString(3),
                        Email = dr.GetString(4),
                        Commission = dr.GetFloat(5),
                        Credit = dr.GetInt32(6),
                        UserName = dr.GetString(7),
                        Password = dr.GetString(8),
                        ConfirmPassword = dr.GetString(8),
                        Comments = dr.GetString(11) ?? ""
                    };
                    ViewBag.CreditStatus = dr.GetInt32(6);
                    ViewBag.UserType = dr.GetInt32(9);
                    ViewBag.PaymentType = dr.GetInt32(10);
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
        [ValidateAntiForgeryToken]
        public ActionResult EditRegister(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to update the user
                bool isDuplicate = false;
                int ID = -1;
                int counter = 0;


                //Start changes on  20-08-2016

                int credit = 0;
                if (model.PaymentType == 3) // Change 2 to 3 on 30-03-2017 if Deposite only then we are setting credit 1
                {
                    credit = 1;
                }
                //end changes on 20-08-2016





                try
                {
                    connection.Open();
                    cmd = connection.CreateCommand();
                    cmd.CommandText = "Select * from users where username='" + model.UserName.ToLower() + "';";
                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        if (model.UserName.Trim().ToLower().Equals(dr.GetString(7)))
                        {
                            counter++;
                            if (counter == 1)
                                ID = dr.GetInt32(0);
                            else
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                    }
                    dr.Close();
                    connection.Close();
                    if (isDuplicate)
                    {
                        ModelState.AddModelError("", "The user name already exists");
                        ViewBag.Status = "false";
                        return View(model);
                    }
                    else
                    {
                        connection.Open();
                        cmd = connection.CreateCommand();
                        cmd.CommandText = "UPDATE users SET name = '" + model.Name.Trim() + "',address='" +
                                          model.Address.Trim() + "', phone='" + model.Phone.Trim() + "', email='" +
                                          model.Email.Trim() + "', commission='" + model.Commission + "', credit='" +
                                          credit + "', username='" + model.UserName.Trim().ToLower() +
                                          "', password='" + model.Password + "' , usertype='" + model.UserType +
                                          "', paymenttype='" + model.PaymentType + "', comments='";
                        cmd.CommandText += model.Comments ?? string.Empty;
                        cmd.CommandText += "' where id='" + ID + "';";
                        cmd.ExecuteNonQuery();
                    }

                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
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

            return View();
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            ViewBag.Status = "false";
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                try
                {
                    if (isDuplicate(model.UserName.Trim()))
                    {
                        ModelState.AddModelError("", "The user name already exists");
                        ViewBag.Status = "false";
                        return View(model);
                    }
                    else
                    {

                        //Start changes on  20-08-2016
                        int credit = 0;
                        if (model.PaymentType == 3) // Change 2 to 3 on 30-03-2017 if Deposite only then we are setting credit 1
                        {
                            credit = 1;
                        }
                        //end changes on 20-08-2016

                        connection.Open();
                        cmd = connection.CreateCommand();
                        cmd.CommandText = "INSERT INTO Users(name,address,phone,email,commission,credit,username,password,usertype,paymenttype,comments,active) VALUES(@name,@address,@phone,@email,@commission,@credit, @username,@password,@usertype,@paymenttype,@comments,1)";
                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        cmd.Parameters.AddWithValue("@address", model.Address.Trim());
                        cmd.Parameters.AddWithValue("@phone", model.Phone.Trim());
                        cmd.Parameters.AddWithValue("@email", model.Email.Trim());
                        cmd.Parameters.AddWithValue("@commission", model.Commission);
                        cmd.Parameters.AddWithValue("@credit", credit);
                        cmd.Parameters.AddWithValue("@username", model.UserName.Trim().ToLower());
                        cmd.Parameters.AddWithValue("@password", model.Password.Trim());
                        cmd.Parameters.AddWithValue("@usertype", model.UserType);
                        cmd.Parameters.AddWithValue("@paymenttype", model.PaymentType);
                        cmd.Parameters.AddWithValue("@comments", model.Comments ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
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

            return View();
        }

        private bool isDuplicate(string username)
        {
            bool check = false;
            try
            {
                connection.Open();
                MySqlCommand cmd = connection.CreateCommand();
                //cmd.CommandText = "Select * from users where username='" + username.ToLower() + "' and active <> 0;";
                cmd.CommandText = "Select * from users where username='" + username.ToLower() + "' and active <> 0;";
                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    check = true;
                    break;
                }
            }
            catch (Exception e) { }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            return check;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disassociate(string provider, string providerUserId)
        {
            string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
            ManageMessageId? message = null;

            // Only disassociate the account if the currently logged in user is the owner
            if (ownerAccount == User.Identity.Name)
            {
                // Use a transaction to prevent the user from deleting their last login credential
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
                {
                    bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
                    if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
                    {
                        OAuthWebSecurity.DeleteAccount(provider, providerUserId);
                        scope.Complete();
                        message = ManageMessageId.RemoveLoginSuccess;
                    }
                }
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : "";
            ViewBag.HasLocalPassword = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(LocalPasswordModel model)
        {
            bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.HasLocalPassword = hasLocalAccount;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasLocalAccount)
            {
                if (ModelState.IsValid)
                {
                    // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                    bool changePasswordSucceeded;
                    try
                    {
                        changePasswordSucceeded = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
                    }
                    catch (Exception)
                    {
                        changePasswordSucceeded = false;
                    }

                    if (changePasswordSucceeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                    }
                }
            }
            else
            {
                // User does not have a local password so remove any validation errors caused by a missing
                // OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        WebSecurity.CreateAccount(User.Identity.Name, model.NewPassword);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    catch (Exception e)
                    {
                        ModelState.AddModelError("", e);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            return new ExternalLoginResult(provider, Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginCallback(string returnUrl)
        {
            AuthenticationResult result = OAuthWebSecurity.VerifyAuthentication(Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
            if (!result.IsSuccessful)
            {
                return RedirectToAction("ExternalLoginFailure");
            }

            if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
            {
                return RedirectToLocal(returnUrl);
            }

            if (User.Identity.IsAuthenticated)
            {
                // If the current user is logged in add the new account
                OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // User is new, ask for their desired membership name
                string loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
                ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
                ViewBag.ReturnUrl = returnUrl;
                return View("ExternalLoginConfirmation", new RegisterExternalLoginModel { UserName = result.UserName, ExternalLoginData = loginData });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
        {
            string provider = null;
            string providerUserId = null;

            if (User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Insert a new user into the database
                using (UsersContext db = new UsersContext())
                {
                    UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());
                    // Check if user already exists
                    if (user == null)
                    {
                        // Insert name into the profile table
                        db.UserProfiles.Add(new UserProfile { UserName = model.UserName });
                        db.SaveChanges();

                        OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.UserName);
                        OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                    }
                }
            }

            ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [AllowAnonymous]
        [ChildActionOnly]
        public ActionResult ExternalLoginsList(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return PartialView("_ExternalLoginsListPartial", OAuthWebSecurity.RegisteredClientData);
        }

        [ChildActionOnly]
        public ActionResult RemoveExternalLogins()
        {
            ICollection<OAuthAccount> accounts = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name.Remove(User.Identity.Name.LastIndexOf(",")));
            List<ExternalLogin> externalLogins = new List<ExternalLogin>();
            foreach (OAuthAccount account in accounts)
            {
                AuthenticationClientData clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

                externalLogins.Add(new ExternalLogin
                {
                    Provider = account.Provider,
                    ProviderDisplayName = clientData.DisplayName,
                    ProviderUserId = account.ProviderUserId,
                });
            }

            ViewBag.ShowRemoveButton = externalLogins.Count > 1 || OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name.Remove(User.Identity.Name.LastIndexOf(","))));
            return PartialView("_RemoveExternalLoginsPartial", externalLogins);
        }
        [HttpPost]
        public ActionResult DeleteAgent(int id)
        {
            List<RegisterModel> models = new List<RegisterModel>();
            try
            {
                connection.Open();
                cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE users SET active = 0 where id = " + id + ";";
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw new Exception("New Exception", e);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
            return Json("Agent Deleted");
        }
        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        internal class ExternalLoginResult : ActionResult
        {
            public ExternalLoginResult(string provider, string returnUrl)
            {
                Provider = provider;
                ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
            }
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion
    }
}
