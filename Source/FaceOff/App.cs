﻿using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace FaceOff
{
	public class App : Application
	{
		public App()
		{
			var welcomePage = new NavigationPage(new WelcomePage())
			{
				BarBackgroundColor = Color.FromHex("1FAECE"),
				BarTextColor = Color.White
			};

			MainPage = welcomePage;
		}

		protected override void OnStart()
		{
			base.OnStart();

			AnalyticsHelpers.Start();
		}
	}
}

