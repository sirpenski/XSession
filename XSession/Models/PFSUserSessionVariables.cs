using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XSession.Models
{
    [Serializable]
    public class PFSUserSessionVariables
    {

        // Properties.  Can Be Any
        public bool IsAuthenticated { get; set; } = false;
        public string UserID { get; set; } = "";



        //constructor - MANDATORY
        public PFSUserSessionVariables() { }
    }
}
