using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using XSession.Models;



namespace XSession.Controllers
{
    public class HomeController : Controller
    {

        PFSXSession<PFSUserSessionVariables> session;


        /// <summary>
        /// OnActionExecuting is run before the controller methods are run.
        /// </summary>
        /// <param name="context">context</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {


            // get the action
            string action = this.ControllerContext.ActionDescriptor.ActionName.ToUpper();

            // define a new user session
            session = new PFSXSession<PFSUserSessionVariables>(context.HttpContext);

            session.SessionExpirationIncrement = new TimeSpan(0, 1, 0);

            // now load the user session
            session.Load(AutoInitialize:true);


      
            bool bypass = false;


            if (string.Compare(action, "LOGIN") == 0)
            {
                bypass = true;
            }

            if (string.Compare(action, "LOGINSUBMIT") == 0)
            {
                bypass = true;
            }

            // go to logout
            if (string.Compare(action, "LOGOUT") == 0)
            {
                bypass = true;
            }


            if (!bypass)
            {
                // if user is not authenticated
                if (!session.SessionVariables.IsAuthenticated || session.IsExpired || session.IsCorrupt)
                {
                    context.Result = new RedirectResult("/Home/Login");
                }
            }



            // execute the base.
            base.OnActionExecuting(context);

        }




        public IActionResult Index()
        {
            return View(session);
        }

        public IActionResult Login()
        {
            session.Reset();
            return View(session);
        }


        /// <summary>
        /// LoginSubmit Handler
        /// </summary>
        /// <param name="uid">User ID</param>
        /// <param name="pwd">Password</param>
        /// <returns></returns>
        public IActionResult LoginSubmit(string uid, string pwd, string sid)
        {
            const string UID = "admin";
            const string PWD = "admin";
            IActionResult rslt = new RedirectResult("/Home/Login");

            string sUid = "";
            string sPwd = "";
            string sSid = "";

            if (!string.IsNullOrEmpty(sid))
            {
                sSid = sid;

                if (string.Compare(sid, session.SessionIDHex) == 0)
                {


                    if (!string.IsNullOrEmpty(uid))
                    {
                        sUid = uid.Trim().ToLower();

                        if (string.Compare(sUid, UID) == 0)
                        {

                            if (!string.IsNullOrEmpty(pwd))
                            {
                                sPwd = pwd;

                                if (string.Compare(sPwd, PWD) == 0)
                                {
                                    Debug.WriteLine("AUTHENTICATING");
                                    session.SessionVariables.IsAuthenticated = true;
                                    session.SessionVariables.UserID = sUid;
                                    session.Save();
                                    rslt = new RedirectResult("/Home/Index");
                                }

                            }

                        }

                    }

                }
            }

            return rslt;

        }


        /// <summary>
        /// Logs out
        /// </summary>
        /// <returns></returns>
        public IActionResult Logout()
        {
            session.Kill();
            return new RedirectResult("/Home/Login");
        }



        /// <summary>
        /// Returns A Sample Page
        /// </summary>
        /// <returns></returns>
        public IActionResult Test1()
        {
            return View(session);
        }


        /// <summary>
        /// Returns a sample page
        /// </summary>
        /// <returns></returns>
        public IActionResult Test2()
        {
            return View(session);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
