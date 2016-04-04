using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Nethereum.Web3;
using System.Net.Http;
using System.Numerics;
using Nethereum.Maker.ERC20Token;

namespace Nethereum.Android
{
    [Activity(Label = "Nethereum.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
   
        Web3.Web3 web3;
        MakerTokenRegistryService makerService;
        EthTokenService tokenService;
        MakerTokenConvertor makerTokenConvertor;
        string address = "0xbb7e97e5670d7475437943a1b314e661d7a9fa2a";
        string addressTo = "0x6ea09719c8bc400315014fef5cfd972df2ccd73b";
        string password = "password";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            makerTokenConvertor = new MakerTokenConvertor();

            // Get our button from the layout resource,
            // and attach an event to it
            Button buttonConnect = FindViewById<Button>(Resource.Id.connectButton);
            Button buttonBalance = FindViewById<Button>(Resource.Id.buttonBalance);
            Button buttonTransfer = FindViewById<Button>(Resource.Id.buttonTransfer);
            FindViewById<EditText>(Resource.Id.editTextTransferTo).Text = addressTo;

            web3 = new Web3.Web3("http://192.168.2.211:8545");

            buttonConnect.Click += async delegate
            {
                try
                {

                    makerService = new MakerTokenRegistryService(web3, "0x877c5369c747d24d9023c88c1aed1724f1993efe");
                    var textView = FindViewById<TextView>(Resource.Id.textViewBalance);
                    tokenService = await makerService.GetEthTokenServiceAsync("MKR");
                    var balance = await tokenService.GetBalanceOfAsync<BigInteger>(address);

                    textView.Text = makerTokenConvertor.ConvertFromMei(balance).ToString();
                    buttonConnect.Visibility = ViewStates.Gone;
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Short).Show();
                }
            };


            buttonBalance.Click += async delegate
            {
                try
                {
                    var textView = FindViewById<TextView>(Resource.Id.textViewBalance);
                    var balance = await tokenService.GetBalanceOfAsync<BigInteger>(address);
                    textView.Text = makerTokenConvertor.ConvertFromMei(balance).ToString();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Short).Show();
                }


            };

            buttonTransfer.Click += async delegate
            {
                try
                {
                    var textTo = FindViewById<EditText>(Resource.Id.editTextTransferTo);
                    var textAmount = FindViewById<EditText>(Resource.Id.editTextTransferAmout);
                    
                    var amount = makerTokenConvertor.ConvertToMei(Convert.ToDecimal(textAmount.Text));
                    var unlocked = await web3.Personal.UnlockAccount.SendRequestAsync(address, password, new Hex.HexTypes.HexBigInteger(5));
                    if (unlocked)
                    {
                        var txid = await tokenService.TransferAsync(address, textTo.Text, amount, new Hex.HexTypes.HexBigInteger(150000));
                        Toast.MakeText(this, txid, ToastLength.Long).Show();
                    }else
                    {
                        Toast.MakeText(this, "Could not unlock", ToastLength.Short).Show();
                    }

                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Short).Show();
                }


            };
        }
    }

    public class MakerTokenConvertor
    {

        private const long MakerMeiUnitValue = 1000000000000000000;

        private int CalculateNumberOfDecimalPlaces(decimal value, int currentNumberOfDecimals = 0)
        {
            decimal multiplied = (decimal)((double)value * Math.Pow(10, currentNumberOfDecimals));
            if (Math.Round(multiplied) == multiplied)
                return currentNumberOfDecimals;
            return CalculateNumberOfDecimalPlaces(value, currentNumberOfDecimals + 1);
        }

        /// <summary>
        /// Mei like Wei is the smallest unit for Maker 
        /// </summary>
        /// <param name="makerAmount"></param>
        /// <returns></returns>
        public BigInteger ConvertToMei(decimal makerAmount)
        {
            var decimalPlaces = CalculateNumberOfDecimalPlaces(makerAmount);
            if (decimalPlaces == 0) return BigInteger.Multiply(new BigInteger(makerAmount), MakerMeiUnitValue);

            var decimalConversionUnit = (decimal)Math.Pow(10, decimalPlaces);

            var makerAmountFromDec = new BigInteger(makerAmount * decimalConversionUnit);
            var meiUnitFromDec = new BigInteger(MakerMeiUnitValue / decimalConversionUnit);
            return makerAmountFromDec * meiUnitFromDec;
        }

        public decimal ConvertFromMei(BigInteger meiAmount)
        {
            return (decimal)meiAmount / MakerMeiUnitValue;
        }
    }
}

