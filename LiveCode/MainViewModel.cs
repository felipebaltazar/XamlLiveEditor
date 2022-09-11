using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Resources = LiveCodeXamlEditor.LiveEditorResources;

namespace LiveCode
{
    public class MainViewModel : ObservableObject
    {
        private readonly Page _currentPage;
        private readonly Page _nextPage;
        private string _code;

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        public ICommand LoginCommand => new Command(async () => await LoginCommandExecuteAsync());

        public MainViewModel(Page currentPage)
        {
            _currentPage = currentPage;
            _nextPage = new EmptyPage();
        }

        private async Task LoginCommandExecuteAsync()
        {
            if (!Code.All(c => char.IsDigit(c)))
                return;

            App.SignalRConnection = new HubConnectionBuilder()
                 .WithUrl($"https://liveeditorapi.azurewebsites.net/LiveEditor?connectionId={Code}&iseditor=False", BuildOptions)
                 .WithAutomaticReconnect()
                 .Build();

            await App.SignalRConnection.StartAsync();

            if (App.SignalRConnection.State == HubConnectionState.Connected)
            {
                await _currentPage.Navigation.PushAsync(_nextPage);

                var pageXaml = Resources.XamlValues.FirstOrDefault(x => x.Key == _nextPage.GetType().Name).Value;
                await App.SignalRConnection.SendAsync("CurrentPageChanged", Code, pageXaml).ConfigureAwait(false);

                App.SignalRConnection.On<string>("CodeChanged", OnCodeChanged);
                App.SignalRConnection.On("RequestCurrentPage", OnRequestCurrentPage);
            }
        }

        private void OnRequestCurrentPage()
        {
            var currentPage = Application.Current.MainPage;
            while (currentPage is NavigationPage nav)
            {
                currentPage = nav.CurrentPage;
            }

            var pageXaml = Resources.XamlValues.FirstOrDefault(x => x.Key == currentPage.GetType().Name).Value;
            _ = App.SignalRConnection.SendAsync("CurrentPageChanged", Code, pageXaml);
        }

        private void OnCodeChanged(string newXaml)
        {
            try
            {
                if (string.IsNullOrEmpty(newXaml))
                    return;

                var element = _nextPage.LoadFromXaml(newXaml);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void BuildOptions(HttpConnectionOptions options)
        {
            options.HttpMessageHandlerFactory = handler =>
            {
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = CertificateValidationCallback
                };
            };
        }

        private static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
