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

    public class OtherController : Controller
    {

        PFSXSession<PFSUserSessionVariables> session;

        /// <summary>
        /// OnActionExecuting is run before the controller methods are run.
        /// </summary>
        /// <param name="context">context</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {


            // define a new user session
            session = new PFSXSession<PFSUserSessionVariables>(context.HttpContext);

            session.SessionExpirationIncrement = new TimeSpan(0, 1, 0);

            // now load the user session
            session.Load();


            // if user is not authenticated
            if (!session.SessionVariables.IsAuthenticated || session.IsExpired || session.IsCorrupt)
            {
                context.Result = new RedirectResult("/Home/Login");
            }

            // execute the base.
            base.OnActionExecuting(context);

        }




        // GET: /<controller>/
        public IActionResult Index()
        {
            return View(session);
        }


        /// <summary>
        /// Returns A Sample Page
        /// </summary>
        /// <returns></returns>
        public IActionResult Test3()
        {
            return View(session);
        }
    }
}
