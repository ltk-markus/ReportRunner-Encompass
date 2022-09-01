using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportRunnerSupreme
{
    class EncompassCredentials
    {
        public string CID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string URL { get; set; }

        public EncompassCredentials(string _cid, string _username, string _password)
        {
            this.CID = _cid;
            this.UserName = _username;
            this.Password = _password;

            if (!string.IsNullOrEmpty(this.CID.Trim()))
            {
                this.URL = $"https://{this.CID}.ea.elliemae.net${this.CID}";
            } else
            {
                this.URL = "";
            }

            Log.WriteLine($"Encompass URL: {this.URL}");
        }
    }
}
