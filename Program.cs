
using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace StockbridgeFinancial.Crawler
{
    
    /// <summary>
    /// CefSharp.OffScreen Minimal Example
    /// </summary>
    public static class Program
    {

        /// <summary>
        /// Asynchronous demo using CefSharp.OffScreen
        /// Loads google.com, uses javascript to fill out the search box then takes a screenshot which is opened
        /// in the default image viewer.
        /// For a synchronous demo see <see cref="MainSync(string[])"/> below.
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>exit code</returns>
        public static int Main(string[] args)
        {
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

            const string testUrl = "https://cars.com/";

            Console.WriteLine("This example application will load {0}, take a screenshot, and save it to your desktop.", testUrl);
            Console.WriteLine("You may see Chromium debugging output, please wait...");
            Console.WriteLine();

            //Console apps don't have a SynchronizationContext, so to ensure our await calls continue on the main thread we use a super simple implementation from
            //https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/
            //Continuations will happen on the main thread. Cef.Initialize/Cef.Shutdown must be called on the same Thread.
            //The Nito.AsyncEx.Context Nuget package has a more advanced implementation
            //should you wish to use a pre-build implementation.
            //https://github.com/StephenCleary/AsyncEx/blob/8a73d0467d40ca41f9f9cf827c7a35702243abb8/doc/AsyncContext.md#console-example-using-asynccontext
            //NOTE: This is only required if you use await

            AsyncContext.Run(async delegate
            {
                var settings = new CefSettings()
                {
                    //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                    CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
                };

                //Perform dependency check to make sure all relevant resources are in our output directory.
                var success = await Cef.InitializeAsync(settings, performDependencyCheck: true, browserProcessHandler: null);

                if (!success)
                {
                    throw new Exception("Unable to initialize CEF, check the log file.");
                }

                //document.getElementsByClassName('nav-user-name')[0].click();
                //document.querySelector("body > div.global-header-container > cars-global-header").shadowRoot.querySelector("ep-modal > div:nth-child(3) > div > ep-button:nth-child(1)").click();
                //document.querySelector("#auth-modal-email").value = 'johngerson808@gmail.com';
                //document.querySelector("#auth-modal-current-password").value = 'test8008';
                //document.querySelector("body > div.global-header-container > cars-global-header > cars-auth-modal").shadowRoot.querySelector("ep-modal > form > ep-button").shadowRoot.querySelector("button").click();


                //document.querySelector("#make-model-search-stocktype").value = "used";
                //document.querySelector("#makes").value = "tesla";
                //document.querySelector("#models").value = "tesla-model_s";
                //document.querySelector("#make-model-max-price").value = "100000";
                //document.querySelector("#make-model-maximum-distance").value = "all";
                //document.querySelector("#make-model-zip").value = "94596";
                //document.querySelector("#by-make-tab > div > div.sds-field.sds-home-search__submit > button").click();




                // Create the CefSharp.OffScreen.ChromiumWebBrowser instance
                using (var browser = new ChromiumWebBrowser(testUrl))
                {
                    async Task WaitBrowserLoading(ChromiumWebBrowser _browser)
                    {
                        while (_browser.IsLoading)
                        {
                            await Task.Delay(500);
                        }
                    }
                    var initialLoadResponse = await browser.WaitForInitialLoadAsync();

                    if (!initialLoadResponse.Success)
                    {
                        throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
                    }

                    _ = await browser.EvaluateScriptAsync(@"
                document.getElementsByClassName('nav-user-name')[0].click();
document.querySelector(""body > div.global-header-container > cars-global-header"").shadowRoot.querySelector(""ep-modal > div:nth-child(3) > div > ep-button:nth-child(1)"").click();
setTimeout(() => {
    document.querySelector(""#auth-modal-email"").value = 'johngerson808@gmail.com';
    document.querySelector(""#auth-modal-current-password"").value = 'test8008';
    document.querySelector(""body > div.global-header-container > cars-global-header > cars-auth-modal"").shadowRoot.querySelector(""ep-modal > form > ep-button"").shadowRoot.querySelector(""button"").click();
}, 100);
");

                    await Task.Delay(500);



                    await WaitBrowserLoading(browser);

                    _ = await browser.EvaluateScriptAsync(@"
                 document.querySelector(""#make-model-search-stocktype"").value = ""used"";
                document.querySelector(""#makes"").value = ""tesla"";
                document.querySelector(""#models"").value = ""tesla-model_s"";
                document.querySelector(""#make-model-max-price"").value = ""100000"";
                document.querySelector(""#make-model-maximum-distance"").value = ""all"";
                document.querySelector(""#make-model-zip"").value = ""94596"";
                document.querySelector(""#by-make-tab > div > div.sds-field.sds-home-search__submit > button"").click();
");
                    await Task.Delay(500);

                    await WaitBrowserLoading(browser);

                    var JSgatherDataFn = @"
function gatherData(el){
    return {
        StockType: el.querySelector('.stock-type')?.innerText,
        DetailLink: el.querySelector('.vehicle-card-link')?.href,
        Title: el.querySelector('.vehicle-card-link')?.innerText,
        MileAge: el.querySelector('.mileage')?.innerText,
        PrimaryPrice: el.querySelector('.primary-price')?.innerText,
        SecondaryPrice: el.querySelector('.secondary-price')?.innerText,
        EstimatedMonthlyPayment: el.querySelector('.js-estimated-monthly-payment-formatted-value-with-abr')?.innerText,
        DealerName: el.querySelector('.dealer-name')?.innerText,
        SdsRatingCount: el.querySelector('.sds-rating__count')?.innerText,
        SdsRatingReviewCount: el.querySelector('.sds-rating__link')?.innerText,
        MilesFromUser: el.querySelector('.miles-from-user')?.innerText,
        Images: [...el.querySelectorAll('.image-wrap')].map((p) => p.querySelector('img')?.src)
    }
}";

                    var a = await browser.EvaluateScriptAsync(@$"
                    let data = [];
                    {JSgatherDataFn}
                    document.querySelector(""#vehicle-cards-container"").querySelectorAll('.vehicle-card').forEach(v => {{data.push(gatherData(v))}});
                    data;
");

                    var vehicleData = (IList<object>)a.Result;






                    // Wait for the screenshot to be taken.
                    var bitmapAsByteArray = await browser.CaptureScreenshotAsync();

                    // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
                    var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

                    Console.WriteLine();
                    Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

                    File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

                    Console.WriteLine("Screenshot saved. Launching your default image viewer...");

                    // Tell Windows to launch the saved image.
                    Process.Start(new ProcessStartInfo(screenshotPath)
                    {
                        // UseShellExecute is false by default on .NET Core.
                        UseShellExecute = true
                    });

                    Console.WriteLine("Image viewer launched. Press any key to exit.");
                }

                // Wait for user to press a key before exit
                Console.ReadKey();

                // Clean up Chromium objects. You need to call this in your application otherwise
                // you will get a crash when closing.
                Cef.Shutdown();
            });

            return 0;
        }

        /// <summary>
        /// Synchronous demo using CefSharp.OffScreen
        /// Loads google.com, uses javascript to fill out the search box then takes a screenshot which is opened
        /// in the default image viewer.
        /// For a asynchronous demo see <see cref="Main(string[])"/> above.
        /// To use this demo simply delete the <see cref="Main(string[])"/> method and rename this method to Main.
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>exit code</returns>
        public static int MainSync(string[] args)
        {
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

            const string testUrl = "https://www.google.com/";

            Console.WriteLine("This example application will load {0}, take a screenshot, and save it to your desktop.", testUrl);
            Console.WriteLine("You may see Chromium debugging output, please wait...");
            Console.WriteLine();

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            // Create the offscreen Chromium browser.
            var browser = new ChromiumWebBrowser(testUrl);

            EventHandler<LoadingStateChangedEventArgs> handler = null;

            handler = (s, e) =>
            {
                // Check to see if loading is complete - this event is called twice, one when loading starts
                // second time when it's finished
                if (!e.IsLoading)
                {
                    // Remove the load event handler, because we only want one snapshot of the page.
                    browser.LoadingStateChanged -= handler;

                    var scriptTask = browser.EvaluateScriptAsync("document.querySelector('[name=q]').value = 'CefSharp Was Here!'");

                    scriptTask.ContinueWith(t =>
                    {
                        if(!t.Result.Success)
                        {
                            throw new Exception("EvaluateScriptAsync failed:" + t.Result.Message);
                        }

                        //Give the browser a little time to render
                        Thread.Sleep(500);
                        // Wait for the screenshot to be taken.
                        var task = browser.CaptureScreenshotAsync();
                        task.ContinueWith(x =>
                        {
                            // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
                            var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

                            Console.WriteLine();
                            Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

                            var bitmapAsByteArray = x.Result;

                            // Save the Bitmap to the path.
                            File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

                            Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

                            // Tell Windows to launch the saved image.
                            Process.Start(new ProcessStartInfo(screenshotPath)
                            {
                                // UseShellExecute is false by default on .NET Core.
                                UseShellExecute = true
                            });

                            Console.WriteLine("Image viewer launched.  Press any key to exit.");
                        }, TaskScheduler.Default);
                    });
                }
            };

            // An event that is fired when the first page is finished loading.
            // This returns to us from another thread.
            browser.LoadingStateChanged += handler;

            // We have to wait for something, otherwise the process will exit too soon.
            Console.ReadKey();

            // Clean up Chromium objects. You need to call this in your application otherwise
            // you will get a crash when closing.
            //The ChromiumWebBrowser instance will be disposed
            Cef.Shutdown();

            return 0;
        }
    }
}