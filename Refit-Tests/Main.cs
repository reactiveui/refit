using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace RefitTestsiOS
{
	public class Application
	{
		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			// if you want to use a different Application Delegate class from "UnitTestAppDelegate"
			// you can specify it here.
			UIApplication.Main (args, null, "UnitTestAppDelegate");
		}
	}
}
