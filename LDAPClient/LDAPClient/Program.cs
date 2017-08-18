using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDAPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 4)
            {
                Console.WriteLine("Usage: LDAPClient <username> <domain> <password> <LDAP server URL>");
                Environment.Exit(1);
            }

            var client = new Client(args[0], args[1], args[2], args[3]);
            var context = new Context(args[0], args[1], args[2], args[3]);
            Console.WriteLine("Context.authenticated: {0}", "foo");
            var user = "sampleuser";
            var newUser = new UserModel("CN=Sample User,OU=Test,OU=Generic Accounts,OU=Information Technology,OU=roundrocktexas.gov,DC=corr,DC=round-rock,DC=tx,DC=us",
                    "sampleuser", "Test", "pl@int3xtpa55!");

            try
            {
                //Adding a user
                Console.WriteLine("Adding " + user + " to the directory");
                client.addUser(newUser);
                Console.WriteLine("Added " + user + " to the directory\r\n");
            }
            catch (System.DirectoryServices.Protocols.DirectoryOperationException e)
            {
                //The user may already exist
                Console.WriteLine("User already exists, continuing");
                System.Diagnostics.Debug.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught unknown exception");
                System.Diagnostics.Debug.WriteLine("Caught exception in program.cs -> Main -> Adduser\r\n" + e);
                Environment.Exit(1);
            }

            //Searching for all users
            var searchResult = client.search(newUser.DN, "objectClass=*");
            foreach (Dictionary<string, string> d in searchResult)
            {
                Console.WriteLine(String.Join("\r\n", d.Select(x => x.Key + ": " + x.Value).ToArray()));
                Console.WriteLine("\r\nFound user\r\n");
            }

            //Changing active state of user
            client.changeActiveState(newUser.DN);

            //Validating credentials
            //if (client.validateUser("sampleuser", "pl@int3xtpa55!"))
            //{
            //    Console.WriteLine("Valid credentials");
            //}
            //else
            //{
            //    Console.WriteLine("Invalid credentials");
            //}

            //Validating credentials using LDAP bind
            //For this to work the server must be configured to map users correctly to its internal database
            if (client.validateUserByBind("sampleuser@roundrocktexas.gov", "pl@int3xtpa55!"))
            {
                Console.WriteLine("Valid credentials (bind)");
            }
            else
            {
                Console.WriteLine("Invalid credentials (bind)");
            }

            //Modifying a user
            //try
            //{
            //    Console.WriteLine("*** Attempting to change user UID");
            //    client.changeUserUid(newUser.DN, "sampleuser", "newsampleuser");
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Caught exception trying to update user UID: " + e);
            //}


            //Deleting a user
            //try
            //{
            //    string userDN = "CN=Sample User,OU=Test,OU=Generic Accounts,OU=Information Technology,OU=roundrocktexas.gov,DC=corr,DC=round-rock,DC=tx,DC=us";
            //    Console.WriteLine("*** Attempting to delete {0}", newUser.UID);
            //    client.delete(userDN);
            //}
            //catch(Exception e)
            //{
            //    Console.WriteLine("Caught exception during delete attempt: {0}", e);
            //}

            //Searching for all users
            searchResult = client.search(newUser.DN, "objectClass=*");
            foreach (Dictionary<string, string> d in searchResult)
            {
                Console.WriteLine(String.Join("\r\n", d.Select(x => x.Key + ": " + x.Value).ToArray()));
                Console.WriteLine("\r\nFound user\r\n");
            }
        }
    }
}
