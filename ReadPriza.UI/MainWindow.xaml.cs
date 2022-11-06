using HtmlAgilityPack;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ReadPriza.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            myWebBrowser.Navigating += MyWebBrowser_Navigating;
        }

        private void MyWebBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            dynamic activeX = sender.GetType().InvokeMember("ActiveXInstance",
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, this.myWebBrowser, new object[] { });

            activeX.Silent = true;
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            GetLoggedInClientAsync();
        }

        Uri baseAddress = new Uri(@"https://hackeru.priza.net/");
        string aspNetSessionId = "";

        private static Lazy<Login> loginProvider = new(() => throw new InvalidOperationException("Missing login provider."));
        private async void GetLoggedInClientAsync()
        {

            var httpClient = new HttpClient()
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(200)
            };

            var response = await httpClient.GetAsync("/").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            foreach (var header in response.Headers)
            {
                if (header.Key == "Set-Cookie")
                {
                    var cookieValue = header.Value.First();
                    var startIndex = cookieValue.IndexOf('=') + 1;
                    var endIndex = cookieValue.IndexOf(';');
                    aspNetSessionId = cookieValue.Substring(startIndex, endIndex - startIndex);
                    break;
                }
            }


            //response.Headers.TryGetValues("ASP.NET_SessionId", out aspNetSessionId).First();

            var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            //var token = doc.DocumentNode.SelectSingleNode("//input[@name='_token']");

            //var login = loginProvider.Value;


            var content = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string,string>("username", "efraimt@gmail.com"),
                new KeyValuePair<string,string>("pass", ""),

                //action=&action2=&userResponse=&recordId=&docType=&docPass=&ttl=&username=efraimt%40gmail.com&pass=Et%21135792468

            });

            using (var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, baseAddress + "default.aspx"))
            {
                requestMessage.Headers.Add("ASP.NET_SessionId", aspNetSessionId);
                requestMessage.Content = content;
                response = await httpClient.SendAsync(requestMessage);
            }
            //response = await httpClient.PostAsync("/default.aspx", content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // TODO handle failed login
            SetWebPage(html);
            SetMessage("Done");

        }

        public static KeyValuePair<string, string> KVP(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }

        public void SetMessage(string statusMessage)
        {
            this.Dispatcher.Invoke(new Action(() => txtStatus.Content = statusMessage));
        }

        public void SetWebPage(string html)
        {
            this.Dispatcher.Invoke(new Action(() => myWebBrowser.NavigateToString(html)));
        }

        async void btnOTP_Click(object sender, RoutedEventArgs e)
        {  
            SetMessage("OTP....");
            if (txtOTP.Text == string.Empty || txtOTP.Text.Length != 6)
            {
                SetWebPage("Check OTP!");
                SetMessage("Check OTP");
                return;
            }
            /**********************************/
          


            string otpPass = txtOTP.Text.Trim();
            List<KeyValuePair<string, string>> contentPairs = new List<KeyValuePair<string, string>>();
            contentPairs.Add(KVP("username", "efraimt@gmail.com"));
            contentPairs.Add(KVP("pass", otpPass));

            contentPairs.Add(KVP("action2", "otp"));
            for (int i = 0; i < otpPass.Length; i++)
            {
                contentPairs.Add(KVP("digit-" + (i + 1).ToString(), otpPass[i].ToString()));
            }

            var content = new FormUrlEncodedContent(contentPairs);

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, baseAddress + "default.aspx"))
            {
                requestMessage.Headers.Add("Referer", baseAddress + "default.aspx?action=otp");
                requestMessage.Headers.Add("ASP.NET_SessionId", aspNetSessionId);
                requestMessage.Content = content;


                var httpClient = new HttpClient()
                {
                    BaseAddress = baseAddress,
                    Timeout = TimeSpan.FromSeconds(200)
                };

                var response = await httpClient.SendAsync(requestMessage);

                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                SetWebPage(html);
                SetMessage("OTP Done");
            }
        }


    }
    // https://hackeru.priza.net/admin_KCoursesTbl.aspx?
    // KActionFrm=search
    // &advanced=y
    // &KActiveFld=%D7%9B%D7%9F
    // &KStartDateFldfilter=on
    // &KStartDateFldx=01%2F01%2F2018
    // &KEndDateFldfilter=on
    // &KEndDateFldy=31%2F12%2F2026
    // &lines=150
    // &order=KCoursesTbl.KStartDateFld




    public record Login(string Email, string Password);

    public record Page<T>(int page, T value);

}
