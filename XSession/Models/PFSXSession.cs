// **********************************************************************
// Programmer: Paul F. Sirpenski
// MIT License
// **********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.AspNetCore.Http;


namespace XSession.Models
{
    
    /// <summary>
    /// This is a class that manages sessions is aspnet core.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PFSXSession<T>:IDisposable where T: new()
    {

        private const string  DEFAULT_SESSION_VARIABLE_NAME = "USR";

        private const int DEFAULT_SESSION_EXPIRATION_INCREMENT_HOURS = 0;

        private const int DEFAULT_SESSION_EXPIRATION_INCREMENT_MINUTES = 20;

        private const int DEFAULT_SESSION_EXPIRATION_INCREMENT_SECONDS = 0;


        // this is the xsession data. this is a combination of the user session variables <T> and 
        // the internal session variables of the class.
        private XSessionData xSessionData = null;


        // creates a new hash generator
        private XHash256Generator xHash = null;

        // random number generator
        private Random xRND = null;

        // private session corrupt flag
        private bool boolSessionIsCorrupt = false;


        /// <summary>
        /// Ctx is the current http context.  It must be set prior to 
        /// loading or saving 
        /// </summary>
        public HttpContext Context { get; set; } = null;


        // name of the session variable
        public string Name { get; set; } = DEFAULT_SESSION_VARIABLE_NAME;


        /// <summary>
        /// Gives access to the serializable session variables
        /// </summary>
        public T SessionVariables
        {
            get { return xSessionData.SessionVariables; }
            set { xSessionData.SessionVariables = value; }
        }


        /// <summary>
        /// Determines if the session is initialized
        /// </summary>
        public bool SessionInitialized
        {
            get { return xSessionData.SessionInitialized; }
            set { xSessionData.SessionInitialized = value; }
        }

        /// <summary>
        /// Session Start Time
        /// </summary>
        public DateTime SessionStart
        {
            get { return xSessionData.SessionStart; }
            set { xSessionData.SessionStart = value; }
        }


        /// <summary>
        /// Session  Expiration Time
        /// </summary>
        public DateTime SessionExpires
        {
            get { return xSessionData.SessionExpires; }
            set { xSessionData.SessionExpires = value; }
        }

        public DateTime SessionCurrentTime
        {
            get { return xSessionData.SessionCurrentTime; }
            set { xSessionData.SessionCurrentTime = value; }
        }

        /// <summary>
        /// The amount of time the session expiration date is incremented each call to extend
        /// </summary>
        public TimeSpan SessionExpirationIncrement { get; set; }


        /// <summary>
        /// This is a unique session id generated in initialize
        /// </summary>
        public byte[] SessionID
        {
            get { return xSessionData.SessionID; }
            set { xSessionData.SessionID = value; }
        }
  

        /// <summary>
        /// Gets the session id as a hexadecimal string
        /// </summary>
        public string SessionIDHex
        {
            get { return ByteArrayToHex(xSessionData.SessionID); }
        }


        /// <summary>
        /// Session Hash Value
        /// </summary>

        public byte[] SessionHashValue
        {
            get { return xSessionData.SessionHashValue; }
            set { xSessionData.SessionHashValue = value; }
        }

        /// <summary>
        /// Session Hash Value as a Hexadecimal string
        /// </summary>
        public string SessionHashValueHex
        {
            get { return ByteArrayToHex(SessionHashValue); }
        }


        /// <summary>
        /// Session Random number generator.  This might be used instead of using the 
        /// internal one.
        /// </summary>
        public Random SessionRandomNumberGenerator
        {
            get { return xRND; }
            set { xRND = value; }
        }


        /// <summary>
        /// Is Session Expired
        /// </summary>
        public bool IsExpired
        {
            get {
                    bool rt = false;
                    if (DateTime.Now > xSessionData.SessionExpires)
                    {
                        rt = true;
                    }
                    return rt;
                }
        }

        /// <summary>
        /// This flag is set if the session is corrupt, ie the hash value is not correct.
        /// </summary>
        public bool IsCorrupt
        {
            get { return boolSessionIsCorrupt; }
        }



        /// <summary>
        /// Session is error
        /// </summary>
        public bool IsError
        {
            get { return IsExpired || IsCorrupt; }
        }


        /// <summary>
        /// Read only variant of SessionInitialized
        /// </summary>
        public bool IsInitialized
        {
            get { return SessionInitialized; }
        }


        // ***************************************************************
        // BEGIN CONSTRUCTORS
        // ***************************************************************

        /// <summary>
        /// Base Constructor
        /// </summary>
        public PFSXSession()
        {
            ConstructorCommon();
        }

        /// <summary>
        /// Constructor that accepts httpcontext
        /// </summary>
        /// <param name="ctx">HttpContext</param>
        public PFSXSession(HttpContext ctx)
        {
            Context = ctx;
            ConstructorCommon();
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="SessionVariableName">The name of the variable used to store the session data</param>
        /// <param name="ctx">HttpContext</param>

        public PFSXSession(string SessionVariableName, HttpContext ctx)
        {
            Context = ctx;
            Name = SessionVariableName;
            ConstructorCommon();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx">HttpContext</param>
        /// <param name="xr">Random Number Generator</param>
        public PFSXSession(HttpContext ctx, Random xr)
        {
            Context = ctx;
            xRND = xr;
            ConstructorCommon();
        }


        /// <summary>
        /// Constructor with 3 parameters
        /// </summary>
        /// <param name="SessionVariableName">Session Variable Name</param>
        /// <param name="ctx">HttpContext</param>
        /// <param name="xr">Random Number Generator</param>
        public PFSXSession (string SessionVariableName, HttpContext ctx, Random xr )
        {
            Name = SessionVariableName;
            Context = ctx;
            xRND = xr;
        }


        /// <summary>
        /// Common Constructor Code
        /// </summary>
        private void ConstructorCommon()
        {

            bool b = Attribute.IsDefined(typeof(T), typeof(SerializableAttribute));
            if (!b)
            {
                throw new Exception("Object Is Not Serializable");
            }

            SessionExpirationIncrement = new TimeSpan(DEFAULT_SESSION_EXPIRATION_INCREMENT_HOURS,
                                                      DEFAULT_SESSION_EXPIRATION_INCREMENT_MINUTES,
                                                      DEFAULT_SESSION_EXPIRATION_INCREMENT_SECONDS);

            xSessionData = new XSessionData();
            xSessionData.SessionExpirationIncrement = SessionExpirationIncrement;


            xHash = new XHash256Generator();

        }

        /// <summary>
        /// Increments the Session Expiration Time
        /// </summary>
        public void IncrementSessionExpiration()
        {

            Debug.WriteLine("**BEGIN INCREMENT SESSION EXPIRATION");

            Debug.WriteLine("SESSION EXPIRATION BEFORE INCREMENT: " + xSessionData.SessionExpires.ToString());

            Debug.WriteLine("SESSION EXPIRATION INCREMENT: " + xSessionData.SessionExpirationIncrement.ToString());

            DateTime cNow = DateTime.Now;

            Debug.WriteLine("CURRENT TIME: " + cNow.ToString());

            xSessionData.SessionCurrentTime = cNow;

            xSessionData.SessionExpires = cNow.Add(xSessionData.SessionExpirationIncrement);

            Debug.WriteLine("SESSION EXPIRATION AFTER INCREMENT: " + xSessionData.SessionExpires.ToString());

            Debug.WriteLine("**EXIT INCREMENT SESSION EXPIRATION");
        }


        /// <summary>
        /// Initializes the session
        /// </summary>
        /// <returns></returns>
        public void Initialize()
        {

            Debug.WriteLine("**BEGIN INITIALIZING SESSION");

 
            // define new session data object
            xSessionData = new XSessionData();


            // set the session start time
            xSessionData.SessionStart = DateTime.Now;

            // create a unique session id
            xSessionData.SessionID = CreateNewSessionID();

            // create a new user variable
            xSessionData.SessionVariables = new T();


            // set the session expiration increment
            xSessionData.SessionExpirationIncrement = SessionExpirationIncrement;


            // increment the session expiration date time
            IncrementSessionExpiration();

            // init the session
            SessionInitialized = true;
            
            Save();

            Debug.WriteLine("**EXIT INITIALIZE SESSION");
        }



        /// <summary>
        /// Converts the byte array to a hex string of the form aaaa:bbbb:cccc
        /// </summary>
        /// <param name="b">byte array</param>
        /// <returns>string</returns>
        public string ByteArrayToHex(byte[] b, string default_sep = "")
        {
            string rt = "";
            string sep = "";
            for (int i = 0; i < b.Length; i++)
            {
                rt += sep + b[i].ToString("X2");
                if ((i % 2) == 1)
                {
                    sep = default_sep;
                }
                else
                {
                    sep = "";
                }
               
            }
            return rt;
        }


        /// <summary>
        /// ObjectToByteArray serializes an object to a byte array.  
        /// This can then be saved to a session variable
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }


        /// <summary>
        /// ByteArrayToObject unserializes an object
        /// </summary>
        /// <param name="arrBytes"></param>
        /// <returns></returns>
        private object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                Object obj = (Object) binForm.Deserialize(memStream);
                return obj;
            }
        }

        /// <summary>
        /// Load loads the session variables from the HttpContext.Session object, 
        /// unserializes it and makes it available.
        /// </summary>
        public void Load(bool AutoInitialize = false)
        {

            byte[] b = null;
            
            Debug.WriteLine("");
            Debug.WriteLine("**BEGIN SESSION LOAD");

            // try and get the session variable from the http context

            try
            {
                b = Context.Session.Get(Name);

            }
            catch (Exception) 
            {
                b = null;
            }


            // if found, then load the session data.
            if (b != null)
            {

                Debug.WriteLine("SESSION IS NOT NULL");

                // load the data from the byte array
                xSessionData = (XSessionData) ByteArrayToObject(b);

                // now we want to verify the data is valid, ie it hasn't been tampered with
                byte[] CurrentHashValue = ComputeHash(xSessionData);

                // determine if the two byte arrays are equal.
                if (CurrentHashValue.SequenceEqual(xSessionData.SessionHashValue))
                {
                    Debug.WriteLine("SESSION IS NOT CORRUPT");


                    // check if session expired.  If not, then 
                    // increment expiration time.
                    if (!IsExpired)
                    {
                        Debug.WriteLine("SESSION IS NOT EXPIRED");


                        Debug.WriteLine("INCREMENTING SESSION IN LOAD");

                    
                        // increment the expiration data
                        IncrementSessionExpiration();


                        Debug.WriteLine("SAVING SESSION IN LOAD");

                        // Saving Session
                        Save();

                    }
                    else
                    {
                        Debug.WriteLine("SESSION IS EXPIRED");
                    }
                }
  
              
                // hashes do not match, set session corrup value
                else
                {

                    Debug.WriteLine("SESSION IS CORRUPT");

                    boolSessionIsCorrupt = true;
                }
            }


            // b was null, session not found, initialize
            else
            {
                Debug.WriteLine("SESSION IS NULL");
                
                // If session not found, create a new and initialize
                if (AutoInitialize)
                {
                    Debug.WriteLine("INTIALIZING SESSION");

                    Initialize();
                }
                else
                {
                    Debug.WriteLine("NOT INITIALIZING SESSION");
                }
            }

            Debug.WriteLine("**EXIT SESSION LOAD");


        }


  

        /// <summary>
        /// Saves the current session variables to the HttpContext.Session object
        /// </summary>
        public void Save()
        {
            // if the HttpContext is not null then proceed
            if (Context != null)
            {
               
                // compute a SHA256 hash of the data
                xSessionData.SessionHashValue = ComputeHash(xSessionData);

                // convert the x_session_data to an byte array
                byte[] b = ObjectToByteArray(xSessionData);

                // set the session variable to the byte array
                Context.Session.Set(Name, b);
            }
        }


        /// <summary>
        /// Destroys the Session
        /// </summary>
        public void Kill()
        {
            if (Context != null)
            {
                if (Context.Session != null)
                {
                    try
                    {
                        Context.Session.Remove(Name);
                        Dispose();
                    }
                    catch (Exception) { }
                }
            }
        }



        // ----------------------------------------------------------
        // destroys session variables and reinitializes system
        // ----------------------------------------------------------

        public void Reset()
        {

            // save the session increment
            TimeSpan tmpSessionIncrement = SessionExpirationIncrement;

            // reinstantiate system.
            ConstructorCommon();

            // now rename the session incremet
            SessionExpirationIncrement = tmpSessionIncrement;




            // reinitialize system.  note that save is called in 
            // initialize so we don't have to call it again
            Initialize();
        }


        /// <summary>
        /// Creates a new 32 byte unique session id.  Note, this uses a random number generator.  
        /// The best idea is to use one that is active in the main application.  However, if one 
        /// isn't present, we need to create one.
        /// </summary>
        /// <returns></returns>

        private byte[] CreateNewSessionID()
        {
            byte[] rt = new byte[32];

            // if the random number generator is null, then we need to create one.  
            // we try and compute a unique seed so that other sessions that may be occuring 
            // simultaneously do not get an identical number generator. 
            if (xRND == null)
            {

                // what we are trying to do here is compute a random number between 
                // o and 
                xRND = new Random();

                UInt32 a = (UInt32)xRND.Next();

                // get the lowest byte.
                a = (a & 0xFF);


                // get the next random integer
                UInt32 b = (UInt32)xRND.Next();

                // recompute a byte value
                b = (b & 0xFF000000) >> 24;

                // compute the seed
                int c = (int)Math.Abs(a ^ b);

                xRND = new Random(c);


            }

            // now we loop 32 bytes
            for (int i = 0; i < 32; i++)
            {
                // get a random integer and cast to UInt32
                UInt32 RandomUInt = (UInt32) xRND.Next();
                rt[i] = (byte)(RandomUInt & 0x000000FF);
            }

            return rt;


        }



        /// <summary>
        /// Computes the session hash of the current state of data
        /// </summary>
        /// <returns></returns>
        public byte[] ComputeHash()
        {
            return ComputeHash(xSessionData);
        }


        /// <summary>
        /// Computes the hash of the session variables
        /// </summary>
        /// <param name="xsobj"></param>
        /// <returns></returns>
        private byte[] ComputeHash(XSessionData xsCurrent)
        {
            // create a new xession data
            XSessionData xsNew = new XSessionData();

            // copy over the session variables. Note, we won't be 
            // copying over the session hash value.  we are going to hash a 
            // byte[0] for that value.  Otherwise, we'd be recursively hashing.
            xsNew.SessionExpirationIncrement = xsCurrent.SessionExpirationIncrement;
            xsNew.SessionExpires = xsCurrent.SessionExpires;
            xsNew.SessionCurrentTime = xsCurrent.SessionCurrentTime;
            xsNew.SessionInitialized = xsCurrent.SessionInitialized;
            xsNew.SessionStart = xsCurrent.SessionStart;

            // these are the user session variables, we are going to 
            // set a reference only, no deep copy
            xsNew.SessionVariables = xsCurrent.SessionVariables;


            // now, create a byte array by serializing the xsnew object
            byte[] byteArrayToHash = ObjectToByteArray(xsNew);

            // now, get the sha256 hash of this value.
            byte[] hashValue = xHash.Create(byteArrayToHash);

            // return the hash value
            return hashValue;
        }


        /// <summary>
        /// Destroys the session.  Called By Logout
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Context != null)
                {
                    try
                    {
                        SessionVariables = default(T);
                        Context = null;
                        xSessionData = null;
                        xHash.Dispose();
                    }
                    catch (Exception) { }
                }
            }
        }


        /// <summary>
        /// XSessionData is a class that contains both the data for the session and 
        /// the user session variables
        /// </summary>
        [Serializable]
        private class XSessionData
        {

            // unique session id
            public byte[] SessionID { get; set; } = new byte[32];

            // session initializion flag
            public bool SessionInitialized { get; set; } = false;

            // session start
            public DateTime SessionStart { get; set; } = DateTime.MinValue;

            // session expires
            public DateTime SessionExpires { get; set; } = DateTime.MaxValue;


            // session Current Time 
            public DateTime SessionCurrentTime { get; set; } = DateTime.MinValue;

            // session expiration increment.
            public TimeSpan SessionExpirationIncrement { get; set; } = new TimeSpan(0, 20, 0);

            // session data hash value.  
            public byte[] SessionHashValue { get; set; } = new byte[0];

            // set a new session varialbe
            public T SessionVariables { get; set; } = new T();

        }



        /// <summary>
        /// A Private Hash Generator.
        /// </summary>
        private class XHash256Generator: IDisposable
        {
            private SHA256 algorithm = null;


            public XHash256Generator()
            {
                algorithm = SHA256.Create();
            }


            public byte[] Create(byte[] b)
            {
                return algorithm.ComputeHash(b);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose method
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try
                    {
                        algorithm = null;   
                    }
                    catch (Exception) { }
                }
            }


        }



    }



}
