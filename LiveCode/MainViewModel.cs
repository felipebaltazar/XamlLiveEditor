using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace LiveCode
{
    public class MainViewModel : ObservableObject
    {
        private const string PAGEXAML = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage xmlns = ""http://xamarin.com/schemas/2014/forms""
             xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
             x:Class=""LiveCode.EmptyPage"">
    <ContentPage.Content>
        <StackLayout>
            <Label Text = ""Welcome to Xamarin.Forms!""
                VerticalOptions=""CenterAndExpand""
                HorizontalOptions=""CenterAndExpand"" />
        </StackLayout>
    </ContentPage.Content>
</ContentPage>";


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
            if (!Guid.TryParse(Code, out _))
                return;

           App.SignalRConnection = new HubConnectionBuilder()
                .WithUrl($"https://liveeditorapi.azurewebsites.net/LiveEditor?connectionId={Code}&iseditor=False", BuildOptions)
                .WithAutomaticReconnect()
                .Build();

            await App.SignalRConnection.StartAsync();

            if (App.SignalRConnection.State == HubConnectionState.Connected)
            {
                await _currentPage.Navigation.PushAsync(_nextPage);
                await App.SignalRConnection.SendAsync("CurrentPageChanged", Code, PAGEXAML).ConfigureAwait(false);

                App.SignalRConnection.On<string>("CodeChanged", OnCodeChanged);
                App.SignalRConnection.On("RequestCurrentPage", OnRequestCurrentPage);
            }
        }

        private void OnRequestCurrentPage()
        {
            _ = App.SignalRConnection.SendAsync("CurrentPageChanged", Code, PAGEXAML);
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
