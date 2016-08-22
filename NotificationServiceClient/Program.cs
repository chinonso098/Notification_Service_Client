using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using System.Globalization;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NotificationServiceClient
{
    class Program
    {
        #region Init
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
        //
        // To authenticate to the To Do list service, the client needs to know the service's App ID URI.
        // To contact the To Do list service we need its URL as well.
        //
        private static string notificationSerResourceId = ConfigurationManager.AppSettings["ns:NotificationServResourceId"];
        private static string notificationSerBaseAddress = ConfigurationManager.AppSettings["ns:NotificationServBaseAddress"];
        private static AuthenticationContext authContext = null;
        #endregion

        static void Main(string[] args)
        {
            string commandString = string.Empty;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("************************************************************");
            Console.WriteLine("*              Notificatin Service Text Client             *");
            Console.WriteLine("*                                                          *");
            Console.WriteLine("*             Type commands to manage your ToDos           *");
            Console.WriteLine("*                                                          *");
            Console.WriteLine("************************************************************");
            Console.WriteLine("");

            // Initialize the AuthenticationContext for the AAD tenant of choice.
            // Add a persistent file-based token cache.
            authContext = new AuthenticationContext(authority, new FileCache());

            // main command cycle
            while (!commandString.Equals("Exit", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Enter command (get | send | clear | exit | help) >");
                commandString = Console.ReadLine();

                switch (commandString.ToUpper())
                {
                    case "Get": GetNotification();
                        break;
                    case "SEND": SendNotification();
                        break;
                    case "CLEAR": ClearCache();
                        break;
                    case "HELP": Help();
                        break;
                    case "EXIT": Console.WriteLine("Bye!"); ;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
        }

        static void GetNotification()
        {
            #region Obtain token
            AuthenticationResult result = null;
            // first, try to get a token silently
            try
            {
                result = authContext.AcquireTokenSilent(notificationSerResourceId, clientId);
            }
            catch (AdalException ex)
            {
                // There is no token in the cache; prompt the user to sign-in.
                if (ex.ErrorCode == "failed_to_acquire_token_silently")
                {
                    UserCredential uc = TextualPrompt();
                    // if you want to use Windows integrated auth, comment the line above and uncomment the one below
                    // UserCredential uc = new UserCredential();
                    try
                    {
                        result = authContext.AcquireToken(notificationSerResourceId, clientId, uc);
                    }
                    catch (Exception ee)
                    {
                        ShowError(ee);
                        return;
                    }
                }
                else
                {
                    // An unexpected error occurred.
                    ShowError(ex);
                    return;
                }
            }
            #endregion

            #region call the web api

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            #endregion
            string commandString = string.Empty;
            string userInput = string.Empty;
            var notificationSerArray = JArray.Parse("");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Press (1) to get a notification status by id.");
            Console.WriteLine("Press (2) to get a notification respose by id.");
            Console.WriteLine("Press (3) to get a notification response by date.");

            commandString = Console.ReadLine();

            if (commandString.Equals("1")) 
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Please enter the exteral reference id");
                userInput = Console.ReadLine();

                HttpResponseMessage response = httpClient.GetAsync(notificationSerBaseAddress + "/api/Notification/GetNotificationStatusById/" + userInput).Result;

                if (response.IsSuccessStatusCode)
                {
                    string rezstring = response.Content.ReadAsStringAsync().Result;
                    char[] charToBeRemoved = { '{', '}' };

                    rezstring = rezstring.Trim(charToBeRemoved);
                    string[] outputArray = rezstring.Split(',');

                    Console.ForegroundColor = ConsoleColor.Green;

                    foreach (var item in outputArray)
                    {
                        Console.WriteLine(item);
                    }
                }
                #region Error handling
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // If the To Do list service returns access denied, clear the token cache and have the user sign-in again.                    
                        Console.WriteLine("Sorry, you don't have access to the To Do Service. You might need to sign up.");
                        authContext.TokenCache.Clear();
                    }
                    else
                    {
                        Console.WriteLine("Sorry, an error occurred accessing your To Do list.  Please try again.");
                    }
                }
                #endregion
            }
            else if (commandString.Equals("2"))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Please enter the exteral reference id");
                userInput = Console.ReadLine();

                HttpResponseMessage response = httpClient.GetAsync(notificationSerBaseAddress + "/api/Notification/GetNotificationResponseById/" + userInput).Result;
                if (response.IsSuccessStatusCode)
                {
                    string rezstring = response.Content.ReadAsStringAsync().Result;
                    char[] charToBeRemoved = { '{', '}' };

                    rezstring = rezstring.Trim(charToBeRemoved);
                    string[] outputArray = rezstring.Split(',');

                    Console.ForegroundColor = ConsoleColor.Green;

                    foreach (var item in outputArray)
                    {
                        Console.WriteLine(item);
                    }
                }
                #region Error handling
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // If the To Do list service returns access denied, clear the token cache and have the user sign-in again.                    
                        Console.WriteLine("Sorry, you don't have access to the To Do Service. You might need to sign up.");
                        authContext.TokenCache.Clear();
                    }
                    else
                    {
                        Console.WriteLine("Sorry, an error occurred accessing your To Do list.  Please try again.");
                    }
                }
                #endregion
            }
            else if (commandString.Equals("3"))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Please enter the date in this format yyyyMMddHHmmss");
                userInput = Console.ReadLine();

                HttpResponseMessage response = httpClient.GetAsync(notificationSerBaseAddress + "/api/Notification/GetNotificationResponseByDate/" + userInput).Result;
                if (response.IsSuccessStatusCode)
                {
                    string rezstring = response.Content.ReadAsStringAsync().Result;
                    char[] charToBeRemoved = { '{', '}' };

                    rezstring = rezstring.Trim(charToBeRemoved);
                    string[] outputArray = rezstring.Split(',');

                    Console.ForegroundColor = ConsoleColor.Green;

                    foreach (var item in outputArray)
                    {
                        Console.WriteLine(item);
                    }
                   
                }
                #region Error handling
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // If the To Do list service returns access denied, clear the token cache and have the user sign-in again.                    
                        Console.WriteLine("Sorry, you don't have access to the To Do Service. You might need to sign up.");
                        authContext.TokenCache.Clear();
                    }
                    else
                    {
                        Console.WriteLine("Sorry, an error occurred accessing your To Do list.  Please try again.");
                    }
                }
                #endregion
            }
            else 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Input rejected.");
            }


        }

       

        #region Textual UX

        // Gather user credentials form the command line
        static UserCredential TextualPrompt()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("There is no token in the cache or you are not connected to your domain.");
            Console.WriteLine("Please enter username and password to sign in.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("User>");
            string user = Console.ReadLine();
            Console.WriteLine("Password>");
            string password = ReadPasswordFromConsole();
            Console.WriteLine("");
            return new UserCredential(user, password);
        }



        static void ShowError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("An unexpected error occurred.");
            string message = ex.Message;
            if (ex.InnerException != null)
            {
                message += Environment.NewLine + "Inner Exception : " + ex.InnerException.Message;
            }
            Console.WriteLine("Message: {0}", message);
        }

        // Obscure the password being entered
        static string ReadPasswordFromConsole()
        {
            string password = string.Empty;
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            while (key.Key != ConsoleKey.Enter);
            return password;
        }
        #endregion


                // Add a new ToDo in the list of the current user.
        // If there is no valid token available, obtain a new one.
        static void SendNotification()
        {
            #region Obtain token
            AuthenticationResult result = null;
            // first, try to get a token silently            
            try
            {
                result = authContext.AcquireTokenSilent(notificationSerResourceId, clientId);
            }
            catch (AdalException ex)
            {
                // There is no access token in the cache, so prompt the user to sign-in.
                if (ex.ErrorCode == "failed_to_acquire_token_silently")
                {
                    UserCredential uc = TextualPrompt();
                    // if you want to use Windows integrated auth, comment the line above and uncomment the one below
                    // UserCredential uc = new UserCredential();
                    try
                    {
                        result = authContext.AcquireToken(notificationSerResourceId, clientId, uc);
                    }
                    catch (Exception ee)
                    {
                        ShowError(ee);
                        return;
                    }
                }
                else
                {
                    // An unexpected error occurred.
                    ShowError(ex);
                    return;
                }
            }
            #endregion

            #region Call Web API
            Console.WriteLine("Notification Being sent, i don't care about your input");
            string notificationName = "test";

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

            Notification notification = new Notification()
            {
                MessageID = 102,
                Receiver = "+13179370914",
                MessageTitle = "Wensday",
                MessageBody = "Doing some final rounds of testing",
                DeliveryType = "sms",
                EndsAt = DateTime.ParseExact("20150610140000", "yyyyMMddHHmmss",System.Globalization.CultureInfo.InvariantCulture),
                ErrorMessage = "sorry you responded late, call (317-441-3005) to give your answer",
                ResponseNeeded =true
            };

            var response = httpClient.PostAsJsonAsync(notificationSerBaseAddress + "api/Notification/Post", notification).Result;

            var result1 =  response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("New ToDo '{0}' successfully added.", notificationName);
            }
            #endregion
            #region Error handling
            else 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // If the To Do list service returns access denied, clear the token cache and have the user sign-in again.                    
                    Console.WriteLine("Sorry, you don't have access to the To Do Service. You might need to sign up.");
                    authContext.TokenCache.Clear();
                    Console.ReadLine();
                }
                else  
                {
                    Console.WriteLine("");
                    Console.WriteLine(result1.Result);
                    Console.ReadLine();
                }

            }
            #endregion
        }


        // Empties the token cache
        static void ClearCache()
        {
            authContext.TokenCache.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Token cache cleared.");
        }
        static void Help()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("LIST  - lists all of your ToDo. Requires sign in");
            Console.WriteLine("ADD   - adds a new ToDo to your list. Requires sign in");
            Console.WriteLine("CLEAR - empties the token cache, allowing you to sign in as a different user");
            Console.WriteLine("HELP  - displays this page");
            Console.WriteLine("EXIT  - closes this program");
            Console.WriteLine("");
        }
        
    }
}
