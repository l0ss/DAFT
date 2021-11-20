using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace DAFT.Modules
{
    class SQLServerLoginDefaultPw : Module
    {
        private NameValueCollection logins;

        internal SQLServerLoginDefaultPw(Credentials credentials) : base(credentials)
        {
            logins = new NameValueCollection();
        }

        internal bool InjestConfig()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "DefaultCredentials.xml";

                Stream stream = assembly.GetManifestResourceStream(resourceName);

                XmlDocument doc = new XmlDocument();
                doc.Load(stream);
                XmlNodeList nodeList = doc.SelectNodes("configuration");
                XmlNode configuration = nodeList[0];
                string[] userPass = new string[2];
                foreach (XmlNode instance in configuration.ChildNodes)
                {
                    string name = instance.Attributes["name"].Value;
                    string user = instance.SelectSingleNode("username").InnerText;
                    string pass = instance.SelectSingleNode("password").InnerText;
                    logins.Add(name, user + "\0" + pass);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }

        internal override bool Query()
        {
            using (SQLConnection sql = new SQLConnection(instance))
            {
                string dbInstance = instance.Split(new string[] { @"\", @"," }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                string[] defaults = logins.GetValues(dbInstance);
                if (0 == defaults.Length)
                    return false;

                foreach (var d in defaults)
                {
                    Console.WriteLine(d);
                    string[] userpass = d.Split('\0');
                    Credentials creds = new Credentials(userpass[0], userpass[1]);
                    sql.BuildConnectionString(creds);
                    if (sql.Connect())
                        Console.WriteLine("[+] {0} : {1}:{2}", instance, creds.GetUsername(), creds.GetPassword());
                }
            }
                
            return true;
        }
    }
}
