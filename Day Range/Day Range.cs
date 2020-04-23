/*  CTRADER GURU --> Template 1.0.3

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/channel/UCKkgbw09Fifj65W5t5lHeCQ
    GitHub      : https://github.com/cTraderGURU/
    TOS         : https://ctrader.guru/termini-del-servizio/

*/

using System;
using cAlgo.API;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

// --> Microsoft Visual Studio 2017 --> Strumenti --> Gestione pacchetti NuGet --> Gestisci pacchetti NuGet per la soluzione... --> Installa
using Newtonsoft.Json;

namespace cAlgo
{

    // --> AccessRights = AccessRights.FullAccess se si vuole controllare gli aggiornamenti
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class DayRange : Indicator
    {

        #region Enums & Class

        public enum _CalculateMode
        {

            Calculated,
            Canonical

        }

        /// <summary>
        /// Una rappresentazione di una barra daily su un timeframe inferiore
        /// </summary>
        public class DailyBar
        {

            /// <summary>
            /// Il prezzo d'apertura della candela
            /// </summary>
            public double Open { get; private set; }

            /// <summary>
            /// L'orario di apertura della candela
            /// </summary>
            public DateTime OpenTime { get; private set; }

            /// <summary>
            /// Il prezzo di chiusura della candela
            /// </summary>
            public double Close { get; private set; }

            /// <summary>
            /// Il prezzo massimo della candela
            /// </summary>
            public double High { get; private set; }

            /// <summary>
            /// Il prezzo minimo della candela
            /// </summary>
            public double Low { get; private set; }

            /// <summary>
            /// Costruttore della classe
            /// </summary>
            /// <param name="pOpen">Il prezzo di apertura della giornata in esame</param>
            /// <param name="pOpenTime">L'orario di apertura della candela sotto esame</param>
            /// <param name="pClose">Il prezzo di chiusura della giornata in esame</param>
            /// <param name="pHigh">Il prezzo massimo della giornata in esame</param>
            /// <param name="pLow">Il prezzo minimo della giornata in esame</param>
            public DailyBar(double pOpen, DateTime pOpenTime, double pClose, double pHigh, double pLow)
            {

                Open = pOpen;
                OpenTime = pOpenTime;
                Close = pClose;
                High = pHigh;
                Low = pLow;

            }

            /// <summary>
            /// Calcola il body della candela
            /// </summary>
            /// <returns>La differenza tra chiusura e apertura, -1 in caso di dati non inizializzati, -2 dati errati</returns>
            public double Body()
            {

                if (Open == 0 || Close == 0)
                    return -1;

                return (Open > Close) ? Open - Close : (Close > Open) ? Close - Open : -2;

            }

            /// <summary>
            /// Calcola la shadow della candela
            /// </summary>
            /// <returns>La differenza tra massimo e minimo, -1 in caso di dati non inizializzati, -2 dati errati</returns>
            public double Shadow()
            {

                if (High == 0 || Low == 0)
                    return -1;

                return (High > Low) ? High - Low : -2;

            }

        }

        #endregion

        #region Identity

        /// <summary>
        /// ID prodotto, identificativo, viene fornito da ctrader.guru, 74909 è il riferimento del template in uso
        /// </summary>
        public const int ID = 60601;

        /// <summary>
        /// Nome del prodotto, identificativo, da modificare con il nome della propria creazione
        /// </summary>
        public const string NAME = "Day Range";

        /// <summary>
        /// La versione del prodotto, progressivo, utilie per controllare gli aggiornamenti se viene reso disponibile sul sito ctrader.guru
        /// </summary>
        public const string VERSION = "1.0.2";

        #endregion

        #region Params

        /// <summary>
        /// Identità del prodotto nel contesto di ctrader.guru
        /// </summary>
        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = "https://ctrader.guru/product/day-range/")]
        public string ProductInfo { get; set; }

        /// <summary>
        /// Il metodo di calcolo
        /// </summary>
        [Parameter("Calculate Mode", Group = "Params", DefaultValue = _CalculateMode.Canonical)]
        public _CalculateMode MyCalculate { get; set; }

        /// <summary>
        /// L'ora in cui apre la candela daily
        /// </summary>
        [Parameter("Daily Start Hour", Group = "Params", DefaultValue = 0, MaxValue = 23, MinValue = 0, Step = 1)]
        public int DayStartHour { get; set; }

        /// <summary>
        /// Il minuto in cui apre la candela daily
        /// </summary>
        [Parameter("Daily Start Minute", Group = "Params", DefaultValue = 0, MaxValue = 59, MinValue = 0, Step = 1)]
        public int DayStartMinute { get; set; }

        /// <summary>
        /// Il numero di giorni da visualizzare
        /// </summary>
        [Parameter("Day To Show", Group = "Params", DefaultValue = 10, MinValue = 1, Step = 1)]
        public int DayShow { get; set; }

        /// <summary>
        /// Il Box, lo stile del bordo
        /// </summary>
        [Parameter("Line Style Box", Group = "Styles", DefaultValue = LineStyle.DotsRare)]
        public LineStyle LineStyleBox { get; set; }

        /// <summary>
        /// Il Box, lo spessore del bordo
        /// </summary>
        [Parameter("Tickness", Group = "Styles", DefaultValue = 1, MaxValue = 5, MinValue = 1, Step = 1)]
        public int TicknessBox { get; set; }

        /// <summary>
        /// Il Box, il colore del massimo
        /// </summary>
        [Parameter("High Color", Group = "Styles", DefaultValue = "DodgerBlue")]
        public string ColorHigh { get; set; }

        /// <summary>
        /// Il Box, il colore del minimo
        /// </summary>
        [Parameter("Low Color", Group = "Styles", DefaultValue = "Red")]
        public string ColorLow { get; set; }

        /// <summary>
        /// Il Box, l'opacità
        /// </summary>
        [Parameter("Opacity", Group = "Styles", DefaultValue = 30, MinValue = 1, MaxValue = 100, Step = 1)]
        public int Opacity { get; set; }

        /// <summary>
        /// Il Box, il riempimento
        /// </summary>
        [Parameter("Fill Range ?", Group = "Styles", DefaultValue = true)]
        public bool FillBox { get; set; }

        #endregion

        #region Property

        List<DailyBar> DailyBars = new List<DailyBar>();
        Bar FirstCandleOfTheDay;
        Bar LastCandleOfTheDay;

        double DayHighestPrice = 0;
        double DayLowestPrice = 0;

        #endregion

        #region Indicator Events

        /// <summary>
        /// Viene generato all'avvio dell'indicatore, si inizializza l'indicatore
        /// </summary>
        protected override void Initialize()
        {

            // --> Se il timeframe è superiore o uguale al giornaliero devo uscire
            if (TimeFrame >= TimeFrame.Daily)
                Chart.DrawStaticText("Alert", string.Format("{0} : USE THIS INDICATOR ON TIMEFRAME LOWER 1DAY", NAME), VerticalAlignment.Center, HorizontalAlignment.Center, Color.Red);

            // --> Stampo nei log la versione corrente
            Print("{0} : {1}", NAME, VERSION);

            // --> Se viene settato l'ID effettua un controllo per verificare eventuali aggiornamenti
            _checkProductUpdate();

            // --> L'utente potrebbe aver inserito un colore errato
            if (Color.FromName(ColorHigh).ToArgb() == 0)
                ColorHigh = "DodgerBlue";

            if (Color.FromName(ColorLow).ToArgb() == 0)
                ColorLow = "Red";

        }

        /// <summary>
        /// Generato ad ogni tick, vengono effettuati i calcoli dell'indicatore
        /// </summary>
        /// <param name="index">L'indice della candela in elaborazione</param>
        public override void Calculate(int index)
        {


            // --> Non esiste ancora un metodo per rimuovere l'indicatore dal grafico, quindi ci limitiamo a uscire
            // --> Risparmio risorse controllando solo quando mi trovo sull'ultima candela, quella corrente
            // --> Devo avere in memoria abbastanza candele daily
            if (TimeFrame >= TimeFrame.Daily)
                return;

            try
            {

                switch (MyCalculate)
                {

                    case _CalculateMode.Calculated:

                        _drawLevelFromCurrentBar(index);

                        break;
                    default:


                        _drawLevelFromDailyBar();

                        break;

                }



            } catch (Exception exp)
            {

                Chart.DrawStaticText("Alert", string.Format("{0} : error, {1}", NAME, exp), VerticalAlignment.Center, HorizontalAlignment.Center, Color.Red);

            }


        }

        #endregion

        #region Private Methods

        private void _drawLevelFromCurrentBar(int index)
        {

            // --> Deve essere inizializzata
            if (FirstCandleOfTheDay == null)
            {

                FirstCandleOfTheDay = Bars[index];
                LastCandleOfTheDay = Bars[index];

            }

            // --> Poichè l'indice non corrisponde a quello giornaliero, devo ricreare le aperture e le chiusure
            DateTime now = Bars.OpenTimes[index];

            if (Bars[index].High > DayHighestPrice || DayHighestPrice == 0)
                DayHighestPrice = Bars[index].High;
            if (Bars[index].Low < DayLowestPrice || DayLowestPrice == 0)
                DayLowestPrice = Bars[index].Low;

            // --> Ricreo il cambio candela daily
            // --> Ad ogni cambio candela corrente devo aggiornare i dati
            if (FirstCandleOfTheDay.OpenTime != now)
            {

                // --> Se è la prima candela del giorno sarà anche l'inizio della daily
                if (now.Hour == DayStartHour && now.Minute == DayStartMinute)
                {

                    // --> Siamo in un nuovo giorno, registro la candela appena chiusa
                    DailyBars.Add(new DailyBar(FirstCandleOfTheDay.Open, FirstCandleOfTheDay.OpenTime, LastCandleOfTheDay.Close, DayHighestPrice, DayLowestPrice));

                    FirstCandleOfTheDay = Bars[index];

                    // --> Resettiamo le memorie
                    DayHighestPrice = 0;
                    DayLowestPrice = 0;

                }
                else
                {

                    // --> Registro la candela precedente, sarà l'ultima del giorno
                    LastCandleOfTheDay = Bars[index];

                }

            }
            else
            {

                // --> Inutile proseguire, la traccia daily è già stata disegnata
                return;

            }

            // --> Se non ho abbastanza candele devo uscire
            if (DailyBars.Count < 1)
                // --> BoxPeriod )
                return;

            // --> Indice Giornaliero
            int DailyIndex = DailyBars.Count - 1;

            // --> Ricavo l'inizio e la fine temporale del box, verrà preso in considerazione solo per timeframe inferiori
            DateTime today = DailyBars[DailyIndex].OpenTime;
            // --> FirstCandleOfTheDay.OpenTime;
            // --> Facendo attenzione al Venerdì ?
            DateTime tomorrow = today.AddDays(1);

            string rangeFlag = today.ToString();

            // --> Disegnamo il riferimento 
            Chart.DrawTrendLine("High" + rangeFlag, today, DailyBars[DailyIndex].High, tomorrow, DailyBars[DailyIndex].High, Color.FromName(ColorHigh), TicknessBox, LineStyleBox);
            Chart.DrawTrendLine("Low" + rangeFlag, today, DailyBars[DailyIndex].Low, tomorrow, DailyBars[DailyIndex].Low, Color.FromName(ColorLow), TicknessBox, LineStyleBox);

        }

        /// <summary>
        /// Parto dalle ultime candele daily e le disegno ogni volta
        /// </summary>
        /// <param name="index"></param>
        private void _drawLevelFromDailyBar()
        {

            // --> Prelevo le candele daily
            Bars BarsDaily = MarketData.GetBars(TimeFrame.Daily);

            int index = BarsDaily.Count - 1;

            // --> eseguo un ciclo aretroso per disegnare le ultime candele
            for (int i = 0; i < DayShow; i++)
            {

                // --> Il numero di candele da visualizzare potrebbero essere troppe
                try
                {

                    DateTime today = BarsDaily[index - i].OpenTime;
                    // --> FirstCandleOfTheDay.OpenTime;
                    // --> Facendo attenzione al Venerdì ?
                    DateTime tomorrow = today.AddDays(1);

                    string rangeFlag = today.ToString();

                    Chart.DrawTrendLine("High" + rangeFlag, today, BarsDaily[index - i].High, tomorrow, BarsDaily[index - i].High, Color.FromName(ColorHigh), TicknessBox, LineStyleBox);
                    Chart.DrawTrendLine("Low" + rangeFlag, today, BarsDaily[index - i].Low, tomorrow, BarsDaily[index - i].Low, Color.FromName(ColorLow), TicknessBox, LineStyleBox);

                } catch
                {


                }

            }

        }

        /// <summary>
        /// Effettua un controllo sul sito ctrader.guru per mezzo delle API per verificare la presenza di aggiornamenti, solo in realtime
        /// </summary>
        private void _checkProductUpdate()
        {

            // --> Controllo solo se solo in realtime, evito le chiamate in backtest
            if (RunningMode != RunningMode.RealTime)
                return;

            // --> Organizzo i dati per la richiesta degli aggiornamenti
            Guru.API.RequestProductInfo Request = new Guru.API.RequestProductInfo 
            {

                MyProduct = new Guru.Product 
                {

                    ID = ID,
                    Name = NAME,
                    Version = VERSION

                },
                AccountBroker = Account.BrokerName,
                AccountNumber = Account.Number

            };

            // --> Effettuo la richiesta
            Guru.API Response = new Guru.API(Request);

            // --> Controllo per prima cosa la presenza di errori di comunicazioni
            if (Response.ProductInfo.Exception != "")
            {

                Print("{0} Exception : {1}", NAME, Response.ProductInfo.Exception);

            }
            // --> Chiedo conferma della presenza di nuovi aggiornamenti
            else if (Response.HaveNewUpdate())
            {

                string updatemex = string.Format("{0} : Updates available {1} ( {2} )", NAME, Response.ProductInfo.LastProduct.Version, Response.ProductInfo.LastProduct.Updated);

                // --> Informo l'utente con un messaggio sul grafico e nei log del cbot
                Chart.DrawStaticText(NAME + "Updates", updatemex, VerticalAlignment.Top, HorizontalAlignment.Left, Color.Red);
                Print(updatemex);

            }

        }

        #endregion

    }

}

/// <summary>
/// NameSpace che racchiude tutte le feature ctrader.guru
/// </summary>
namespace Guru
{
    /// <summary>
    /// Classe che definisce lo standard identificativo del prodotto nel marketplace ctrader.guru
    /// </summary>
    public class Product
    {

        public int ID = 0;
        public string Name = "";
        public string Version = "";
        public string Updated = "";

    }

    /// <summary>
    /// Offre la possibilità di utilizzare le API messe a disposizione da ctrader.guru per verificare gli aggiornamenti del prodotto.
    /// Permessi utente "AccessRights = AccessRights.FullAccess" per accedere a internet ed utilizzare JSON
    /// </summary>
    public class API
    {
        /// <summary>
        /// Costante da non modificare, corrisponde alla pagina dei servizi API
        /// </summary>
        private const string Service = "https://ctrader.guru/api/product_info/";

        /// <summary>
        /// Costante da non modificare, utilizzata per filtrare le richieste
        /// </summary>
        private const string UserAgent = "cTrader Guru";

        /// <summary>
        /// Variabile dove verranno inserite le direttive per la richiesta
        /// </summary>
        private RequestProductInfo RequestProduct = new RequestProductInfo();

        /// <summary>
        /// Variabile dove verranno inserite le informazioni identificative dal server dopo l'inizializzazione della classe API
        /// </summary>
        public ResponseProductInfo ProductInfo = new ResponseProductInfo();

        /// <summary>
        /// Classe che formalizza i parametri di richiesta, vengono inviate le informazioni del prodotto e di profilazione a fini statistici
        /// </summary>
        public class RequestProductInfo
        {

            /// <summary>
            /// Il prodotto corrente per il quale richiediamo le informazioni
            /// </summary>
            public Product MyProduct = new Product();

            /// <summary>
            /// Broker con il quale effettiamo la richiesta
            /// </summary>
            public string AccountBroker = "";

            /// <summary>
            /// Il numero di conto con il quale chiediamo le informazioni
            /// </summary>
            public int AccountNumber = 0;

        }

        /// <summary>
        /// Classe che formalizza lo standard per identificare le informazioni del prodotto
        /// </summary>
        public class ResponseProductInfo
        {

            /// <summary>
            /// Il prodotto corrente per il quale vengono fornite le informazioni
            /// </summary>
            public Product LastProduct = new Product();

            /// <summary>
            /// Eccezioni in fase di richiesta al server, da utilizzare per controllare l'esito della comunicazione
            /// </summary>
            public string Exception = "";

            /// <summary>
            /// La risposta del server
            /// </summary>
            public string Source = "";

        }

        /// <summary>
        /// Richiede le informazioni del prodotto richiesto
        /// </summary>
        /// <param name="Request"></param>
        public API(RequestProductInfo Request)
        {

            RequestProduct = Request;

            // --> Non controllo se non ho l'ID del prodotto
            if (Request.MyProduct.ID <= 0)
                return;

            // --> Dobbiamo supervisionare la chiamata per registrare l'eccexione
            try
            {

                // --> Strutturo le informazioni per la richiesta POST
                NameValueCollection data = new NameValueCollection 
                {
                    {
                        "account_broker",
                        Request.AccountBroker
                    },
                    {
                        "account_number",
                        Request.AccountNumber.ToString()
                    },
                    {
                        "my_version",
                        Request.MyProduct.Version
                    },
                    {
                        "productid",
                        Request.MyProduct.ID.ToString()
                    }
                };

                // --> Autorizzo tutte le pagine di questo dominio
                Uri myuri = new Uri(Service);
                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                // -->> Richiedo le informazioni al server
                using (var wb = new WebClient())
                {

                    wb.Headers.Add("User-Agent", UserAgent);

                    var response = wb.UploadValues(myuri, "POST", data);
                    ProductInfo.Source = Encoding.UTF8.GetString(response);

                }

                // -->>> Nel cBot necessita l'attivazione di "AccessRights = AccessRights.FullAccess"
                ProductInfo.LastProduct = JsonConvert.DeserializeObject<Product>(ProductInfo.Source);

            } catch (Exception Exp)
            {

                // --> Qualcosa è andato storto, registro l'eccezione
                ProductInfo.Exception = Exp.Message;

            }

        }

        /// <summary>
        /// Esegue un confronto tra le versioni per determinare la presenza di aggiornamenti
        /// </summary>
        /// <returns></returns>
        public bool HaveNewUpdate()
        {

            // --> Voglio essere sicuro che stiamo lavorando con le informazioni giuste
            return (ProductInfo.LastProduct.ID == RequestProduct.MyProduct.ID && ProductInfo.LastProduct.Version != "" && RequestProduct.MyProduct.Version != "" && new Version(RequestProduct.MyProduct.Version).CompareTo(new Version(ProductInfo.LastProduct.Version)) < 0);

        }

    }

}
