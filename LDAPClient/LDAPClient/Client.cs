using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Cryptography;

namespace LDAPClient
{
    /// <summary>
    /// A sample LDAP client. For simplicity reasons, this clients only uses synchronous requests.
    /// </summary>
    public class Client
    {
        public Client(string username, string domain, string password, string url)
        {
            var credentials = new NetworkCredential(username, password, domain);
            var serverId = new LdapDirectoryIdentifier(url);

            connection = new LdapConnection(serverId, credentials);
            try
            {
                connection.Bind();
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception while connecting to LDAP server: " + e);
            }

        }

        /// <summary>
        /// Performs a search in the LDAP server. This method is generic in its return value to show the power
        /// of searches. A less generic search method could be implemented to only search for users, for instance.
        /// </summary>
        /// <param name="baseDn">The distinguished name of the base node at which to start the search</param>
        /// <param name="ldapFilter">An LDAP filter as defined by RFC4515</param>
        /// <returns>A flat list of dictionaries which in turn include attributes and the distinguished name (DN)</returns>
        public List<Dictionary<string, string>> search(string baseDn, string ldapFilter)
        {
            var request = new SearchRequest(baseDn, ldapFilter, SearchScope.Subtree, null);
            var response = (SearchResponse)connection.SendRequest(request);

            var result = new List<Dictionary<string, string>>();

            foreach (SearchResultEntry entry in response.Entries)
            {
                var dic = new Dictionary<string, string>();
                dic["DN"] = entry.DistinguishedName;

                foreach (string attrName in entry.Attributes.AttributeNames)
                {
                    //For simplicity, we ignore multi-value attributes
                    dic[attrName] = string.Join(",", entry.Attributes[attrName].GetValues(typeof(string)));
                }

                result.Add(dic);
            }

            return result;
        }

        /// <summary>
        /// Adds a user to the LDAP server database. This method is intentionally less generic than the search one to
        /// make it easier to add meaningful information to the database.
        /// </summary>
        /// <param name="user">The user to add</param>
        public void addUser(UserModel user)
        {
            var sha1 = new SHA1Managed();
            var digest = Convert.ToBase64String(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(user.UserPassword)));

            var request = new AddRequest(user.DN, new DirectoryAttribute[] {
                new DirectoryAttribute("uid", user.UID),
                new DirectoryAttribute("ou", user.OU),
                new DirectoryAttribute("userPassword", "{SHA}" + digest),
                new DirectoryAttribute("objectClass", new string[] { "top", "person", "organizationalPerson", "user" })
            });

            //var request = new AddRequest(user.DN, new DirectoryAttribute("objectClass", new string[] { "top", "person", "organizationalPerson", "user" }));

            connection.SendRequest(request);
        }

        /// <summary>
        /// This method shows how to modify an attribute.
        /// </summary>
        /// <param name="dn">Distinguished Name for user</param>
        /// <param name="oldUid">Old user UID</param>
        /// <param name="newUid">New user UID</param>
        public void changeUserUid(string dn, string oldUid, string newUid)
        {
            //var userDN = "OU=Test,OU=Generic Accounts,OU=Information Technology,OU=roundrocktexas.gov,DC=corr,DC=round-rock,DC=tx,DC=us";
            //var oldDn = string.Format("uid={0},{1}", oldUid, userDN);
            //var newDn = string.Format("uid={0},{1}", newUid, userDN);

            //DirectoryRequest request = new ModifyDNRequest(oldDn, userDN, "uid=" + newUid);
            //connection.SendRequest(request);

            DirectoryRequest request = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, "uid", new string[] { newUid });
            connection.SendRequest(request);
        }

        /// <summary>
        /// This method enables a user account
        /// </summary>
        /// <param name="dn">Distinguished name of the account to activate</param>
        public void changeActiveState(string dn)
        {
            Console.WriteLine("\r\nchangeActiveState: performing search");
            try
            {
                var searchResult = search(dn, "objectClass=user");
                string uac;

                Dictionary<string, string> res;
                res = searchResult[0];
                Console.WriteLine("Copied dict out of list");
                res.TryGetValue("useraccountcontrol", out uac);
                Console.WriteLine("UAC: {0}", uac);
                //newUac = (int.Parse(uac) & (~int.Parse(uac)));

                
                //foreach (Dictionary<string, string> d in searchResult)
                //{
                //    d.TryGetValue("useraccountcontrol", out uac);
                //    Console.WriteLine("userAccountControl: " + uac);
                //    int testInt = int.Parse(uac);
                //    newUac = (testInt & (~2));
                //    Console.WriteLine("~testInt: " + (~testInt).ToString());
                //    Console.WriteLine("userAccountControl modified (& ~): " + (newUac).ToString());
                //}
                //Console.WriteLine("*** Attempting to activate user with UAC {0}", "544");
                //Console.WriteLine("*** Attempting to deactivate user with UAC {0}", "546");
                Console.WriteLine("*** Attempting to modify {0} attribute using mod collection", "description");
                DirectoryAttributeModificationCollection modColl = new DirectoryAttributeModificationCollection();
                var dirMods = new DirectoryAttributeModification();
                dirMods.Name = "description";
                dirMods.Add("testing");
                dirMods.Operation = DirectoryAttributeOperation.Replace;
                modColl.Add(dirMods);
                DirectoryRequest request = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, "useraccountcontrol", new string[] { "544" });
                DirectoryRequest request1 = new ModifyRequest(dn, new DirectoryAttributeModification[] { dirMods });
                connection.SendRequest(request1);
            }
            catch (System.DirectoryServices.Protocols.DirectoryOperationException e)
            {
                Console.WriteLine("Encountered problem activating " + dn);
                Console.WriteLine("Caught exception: " + e);
            }
            catch (System.ArgumentException e)
            {
                Console.WriteLine("Caught argument exception: " + e);
            }


        }

        /// <summary>
        /// This method shows how to delete anything by its distinguised name (DN).
        /// </summary>
        /// <param name="dn">Distinguished name of the entry to delete</param>
        public void delete(string dn)
        {
            var request = new DeleteRequest(dn);
            connection.SendRequest(request);
        }

        /// <summary>
        /// Searches for a user and compares the password.
        /// We assume all users are at base DN ou=users,dc=example,dc=com and that passwords are
        /// hashed using SHA1 (no salt) in OpenLDAP format.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>true if the credentials are valid, false otherwise</returns>
        public bool validateUser(string username, string password)
        {
            var sha1 = new SHA1Managed();
            var digest = Convert.ToBase64String(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
            var request = new CompareRequest(string.Format("uid={0},OU=Test,OU=Generic Accounts,OU=Information Technology,OU=roundrocktexas.gov,DC=corr,DC=round-rock,DC=tx,DC=us", username),
                "userPassword", "{SHA}" + digest);
            var response = (CompareResponse)connection.SendRequest(request);
            return response.ResultCode == ResultCode.CompareTrue;
        }

        /// <summary>
        /// Another way of validating a user is by performing a bind. In this case the server
        /// queries its own database to validate the credentials. It is defined by the server
        /// how a user is mapped to its directory.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>true if the credentials are valid, false otherwise</returns>
        public bool validateUserByBind(string username, string password)
        {
            bool result = true;
            var credentials = new NetworkCredential(username, password);
            var serverId = new LdapDirectoryIdentifier(connection.SessionOptions.HostName);

            var conn = new LdapConnection(serverId, credentials);
            try
            {
                conn.Bind();
            }
            catch (Exception)
            {
                Console.WriteLine("Caught exception during bind operation");
                result = false;
            }

            conn.Dispose();

            return result;
        }

        private LdapConnection connection;
    }
}
