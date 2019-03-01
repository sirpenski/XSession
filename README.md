# XSession
<H3>Makes using Session Variables Easy For AspNetCore</H3>

This is a simple project that illustrates how to use the generic XSession class to handle session variables is aspnetcore web applications.  THis sample provides a generic class so any type of object can be stored as a session variable.  To use the class, do the following

<b>1.  Define any class that corresponds to the data model you want to save in a session.  Make sure the class is serializable and has a default constructor.</b>
<br>
<br>

<pre>
public class UserSessionVariables
{
  public bool IsAuthenticated {get; set;} = false;
  public string UserID {get; set; } = "";
  
  public UserSessionVariables() {}

}
</pre>

<br>
<br>
<b>2.  Add the following namespaces to all your controllers.</b>
<br>
<br>
<pre>
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
</pre>
<br>
<br>
<b>3.  In all controllers, define a controller level variable to store the session.</b>
<br>
<br>
<pre>
PFSXSession&lt;UserSessionVariables&gt; session;
</pre>
<br>
<br>
<b>4.  In each controller, define an OnActionExecuting function.</b>
<br>
<br>
<pre>
public override void OnActionExecuting(ActionExecutingContext context)<br>
{

     // get the action<br>
     string action = this.ControllerContext.ActionDescriptor.ActionName.ToUpper();

     // define a new user session
     session = new PFSXSession<PFSUserSessionVariables>(context.HttpContext);

     // now load the user session
     session.Load(AutoInitialize:true);
     
     bool bypass = false;

      // this is just simple login, login submit, and logout logic.  It determines whether 
      // we want to check the session authentication, etc.
     if (string.Compare(action, "LOGIN") == 0 || string.Compare(action, "LOGINSUBMIT") == 0 || 
         string.Compare(action, "LOGOUT") == 0)
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
</pre>

<br>
<br>
<b>5. In the ConfigureServices function in startup.cs, add the following to configure asp.net core to use session management and to store in server side memory.</b>
<br>
<br>
<pre> 
public void ConfigureServices(IServiceCollection services)
{

              // add server memory cache
            services.AddDistributedMemoryCache();

            // add session
            services.AddSession();
}
</pre>
<br>
<br>
<b>6. In the Configure function in startup.cs, add the UseSession(); and make sure it is before the UseMvc().</b>
<br>
<br>
<pre>
  
         // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStaticFiles();

            // use the session
            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
</pre>
<br>
<br>
<b>7. Have Fun</b>
<br>
<br>
*** The class file is located in the Models folder of the project.  The file name is PFSXSession.cs  Rename it and change the namespace, etc.
<br>
<br>
Best Regards,<br>
Paul Sirpenski.

