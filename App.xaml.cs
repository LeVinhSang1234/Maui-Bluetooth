﻿namespace Bluetooth;
using Bluetooth.Pages;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new NavigationPage(new HomePage());
	}
}
