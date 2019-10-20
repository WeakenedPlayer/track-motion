using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Tobii.StreamEngine;

namespace TrackMotion
{
	public static class Program
	{
		public static void OnGazePoint(ref tobii_gaze_point_t gazePoint )
		{
			if (gazePoint.validity == tobii_validity_t.TOBII_VALIDITY_VALID)
			{
				Console.WriteLine($"Gaze point: {gazePoint.position.x}, {gazePoint.position.y}");
			}
		}
		public static tobii_gaze_point_callback_t callback = OnGazePoint;

		static void Main(string[] args)
		{
			// Create API context
			IntPtr apiContext;
			tobii_error_t result = Interop.tobii_api_create(out apiContext, null);
			Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

			// Enumerate devices to find connected eye trackers
			List<string> urls;
			result = Interop.tobii_enumerate_local_device_urls(apiContext, out urls);
			Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
			if (urls.Count == 0)
			{
				Console.WriteLine("Error: No device found");
				return;
			}

			// Connect to the first tracker found
			IntPtr deviceContext;
			result = Interop.tobii_device_create(apiContext, urls[0], out deviceContext);
			Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

			// Subscribe to gaze data
			result = Interop.tobii_gaze_point_subscribe(deviceContext, callback);
			Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

			// ################################################################
			ConsoleKeyInfo cki;

			while( true )
			{
				// ----------------------------------------------------------------
				// [Apps]キーが押されるまで待機。
				Console.WriteLine("--------------------------------------------------------------------------------");
				Console.WriteLine("Press [Apps] to start/stop tracking. Press [Esc] to quit.");
				do
				{
					cki = Console.ReadKey(true);
				}
				while ( ( cki.Key != ConsoleKey.Applications ) && (cki.Key != ConsoleKey.Escape) );

				// quit
				if (cki.Key == ConsoleKey.Escape) break;

				// ----------------------------------------------------------------
				// [Apps]キーが押されるまでトラッキング
				Console.WriteLine("--------------------------------------------------------------------------------");
				Console.WriteLine("Then press [Apps] to stop tracking. Press [Esc] to quit.");
				do
				{
					while (!Console.KeyAvailable)
					{
						// Optionally block this thread until data is available. Especially useful if running in a separate thread.
						Interop.tobii_wait_for_callbacks(apiContext, new[] { deviceContext });
						Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR || result == tobii_error_t.TOBII_ERROR_TIMED_OUT);

						// Process callbacks on this thread if data is available
						Interop.tobii_device_process_callbacks(deviceContext);
						Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
					}
					cki = Console.ReadKey(true);
				} while ((cki.Key != ConsoleKey.Applications) && (cki.Key != ConsoleKey.Escape));

				if (cki.Key == ConsoleKey.Escape) break;
			}

			// Cleanup
			result = Interop.tobii_gaze_point_unsubscribe(deviceContext);
			Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
			result = Interop.tobii_device_destroy(deviceContext);
			Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
			result = Interop.tobii_api_destroy(apiContext);
			Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
		}
	}
}
