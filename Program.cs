
using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text.Json;
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
                    CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
                    LogSeverity = LogSeverity.Disable
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

                    var firstpage20 = await browser.EvaluateScriptAsync(@$"
                    let data = [];
                    {JSgatherDataFn}
                    document.querySelector(""#vehicle-cards-container"").querySelectorAll('.vehicle-card').forEach(v => {{data.push(gatherData(v))}});
                    data;
");

                    List<VehicleSearchResultItemDTO> gatheredVehicles = new List<VehicleSearchResultItemDTO>();
                    var firstpage20Result = JsonSerializer.Deserialize<List<VehicleSearchResultItemDTO>>(JsonSerializer.Serialize(firstpage20.Result));

                    Console.WriteLine("First page gathered");

                    Console.WriteLine("Total gathered vehicles: " + gatheredVehicles.Count);
                    Console.WriteLine($"Gathered Vehicles writes into file named 1_{gatheredVehicles.Count}.json");

                    File.WriteAllText($"1_{gatheredVehicles.Count}.json", JsonSerializer.Serialize(firstpage20Result));

                    gatheredVehicles.AddRange(firstpage20Result);

                    _ = await browser.EvaluateScriptAsync(@"
let a = location.search;
location.href = '?page=2&page_size=20&'+a.slice(1,a.length)
                    ");


                    await Task.Delay(500);

                    await WaitBrowserLoading(browser);




                    var secondpage20 = await browser.EvaluateScriptAsync(@$"
                    let data = [];
                    {JSgatherDataFn}
                    document.querySelector(""#vehicle-cards-container"").querySelectorAll('.vehicle-card').forEach(v => {{data.push(gatherData(v))}});
                    data;
");

                    var secondpage20Result = JsonSerializer.Deserialize<List<VehicleSearchResultItemDTO>>(JsonSerializer.Serialize(secondpage20.Result));

                    Console.WriteLine("Second page gathered");

                    Console.WriteLine("Total gathered vehicles: " + gatheredVehicles.Count);
                    Console.WriteLine($"Gathered Vehicles writes into file named 2_{gatheredVehicles.Count}.json");

                    File.WriteAllText($"2_{gatheredVehicles.Count}.json", JsonSerializer.Serialize(secondpage20Result));

                    gatheredVehicles.AddRange(secondpage20Result);


                    Console.WriteLine("Total gathered vehicles: " + gatheredVehicles.Count);
                    Console.WriteLine();
                    Console.WriteLine("Choose specific vehicle to gather details");
                    Console.WriteLine("------Vehicles-------");
                    foreach (var item in gatheredVehicles)
                    {
                        var index = gatheredVehicles.IndexOf(item);
                        Console.WriteLine($"[ {index} ] - {item.Title} - {item.DealerName}");
                    }
                    Console.WriteLine();
                    int selectedVehicleIndex = -1;
                    Console.WriteLine("Enter vehicle index: ");
                    while (!int.TryParse(Console.ReadLine(), out selectedVehicleIndex) || gatheredVehicles.Count < selectedVehicleIndex || selectedVehicleIndex < 0)
                    {
                        Console.WriteLine($"Invalid Input. Input must between 0 and {gatheredVehicles.Count}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("Gathering vehicle details...");
                    var selectedVehicle = gatheredVehicles[selectedVehicleIndex];
                    var selectedVehicleId = selectedVehicle.DetailLink.Split('/').Last();
                    _ = await browser.EvaluateScriptAsync(@$"location.href = '{selectedVehicle.DetailLink}'");
                    await Task.Delay(500);
                    await WaitBrowserLoading(browser);

                    var JSGatherDetailDataFn = @"function getDetailData() {
    let basicKeys = [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.sds-page-section.basics-section > dl"").querySelectorAll('dt')].map(v => v?.innerText);
    let basicValues = [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.sds-page-section.basics-section > dl"").querySelectorAll('dd')].map(v => v?.innerText);
    let basicData = {};
    basicKeys.forEach((key, index) => {
        basicData[key] = basicValues[index];
    });

    let featuresKeys = [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.sds-page-section.features-section > dl"").querySelectorAll('dt')].map(v => v?.innerText);
    let featuresValues = [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.sds-page-section.features-section > dl"").querySelectorAll('dd')].map(v => v?.innerText);
    let featuresData = {};
    featuresKeys.forEach((key, index) => {
        featuresData[key] = featuresValues[index];
    });

    let ratingBreakdownKeys = [...document.querySelector(""#vehicle-reviews > div > div.review-breakdown > ul"").querySelectorAll('.sds-definition-list__display-name')].map(v => v?.innerText);
    let ratingBreakdownValues = [...document.querySelector(""#vehicle-reviews > div > div.review-breakdown > ul"").querySelectorAll('.sds-definition-list__value')].map(v => v?.innerText);
    let ratingBreakdownData = {};
    ratingBreakdownKeys.forEach((key, index) => {
        ratingBreakdownData[key] = ratingBreakdownValues[index];
    });

    let reviews = [];
    let reviewRatings = [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"").querySelectorAll('.sds-rating__count')].map(v => v?.innerText);
    let reviewTitles = [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"").querySelectorAll('h3')].map(v => v?.innerText);
    let reviewDates = [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"").querySelectorAll('div.review-byline.review-section > div:nth-child(1)')].map(v => v?.innerText);
    let reviewBy = [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"").querySelectorAll('div.review-byline.review-section > div:nth-child(2)')].map(v => v?.innerText);
    let reviewDescription =  [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"").querySelectorAll('p')].map(v => v?.innerText);
    reviewRatings.forEach((rating, index) => {
        reviews.push({
            ""Rating"": rating,
            ""Title"": reviewTitles[index],
            ""Date"": reviewDates[index],
            ""By"": reviewBy[index],
            ""Description"": reviewDescription[index]
        });
    });


    return {
        ""Link"": window.location.href,
        ""Images"": [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > vdp-gallery > gallery-slides"").querySelectorAll('img')].map(t => t.src),
        ""Contact"": document.querySelector(""#dealer-section--embedded1 > div > section > div"")?.innerText,
        ""StockType"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.title-section > p"")?.innerText,
        ""Title"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.title-section > h1"")?.innerText,
        ""MileAge"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.title-section > div.listing-mileage"")?.innerText,
        ""PrimaryPrice"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.price-section > span.primary-price"")?.innerText,
        ""PriceDrop"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.price-section > span.secondary-price.price-drop"")?.innerText,
        ""AvgMarketPrice"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.deal-gauge--container.great-deal > div.deal-gauge-graph-container > div.deal-gauge-box-plot > div > span > strong"")?.innerText,
        ""EstimatedMonthlyPayment"": document.querySelector(""#emp-tooltip-1 > span > a > span.js-estimated-monthly-payment-formatted-value-with-abr"")?.innerText,
        ""VehicleBadges"": [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.vehicle-badging"").querySelectorAll('span')].map(t => t?.innerText),
        ""BasicData"": basicData,
        ""FeaturesData"": featuresData,
        ""PriceHistory"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.price-history-container > cars-price-history"").priceHistoryData,
        ""SellerName"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.seller-info > h3"")?.innerText,
        ""SellerRate"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.seller-info > div.sds-rating > span"")?.innerText,
        ""SellerAddress"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.seller-info > div:nth-child(4) > div"")?.innerText,
        ""SellerNoteAboutCar"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.seller-info > section.sds-page-section.seller-notes.scrubbed-html > div.sellers-notes"")?.innerText + document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.seller-info > section.sds-page-section.seller-notes.scrubbed-html > div.sellers-notes > span.sellers-notes-truncated.hidden"")?.innerText,
        ""ConsumerReviewRating"": document.querySelector(""#vehicle-reviews > div > div.sds-rating.sds-rating--big > span"")?.innerText,
        ""ConsumerReviewCount"": document.querySelector(""#vehicle-reviews > div > div.sds-rating.sds-rating--big > a"")?.innerText,
        ""ConsumerReviewRatingBreakdown"": ratingBreakdownData,
        ""ConsumerReviews"": reviews

    }
}";

                    var detailResult = await browser.EvaluateScriptAsync($@"
                        getDetailData()
                    ");

                    var detailData = JsonSerializer.Deserialize<VehicleDetailResultItemDto>(JsonSerializer.Serialize(detailResult.Result));

                    Console.WriteLine("Detail data gathered");
                    Console.WriteLine($"Writes into {selectedVehicleId}.json");


                    File.WriteAllText($"{selectedVehicleId}.json", JsonSerializer.Serialize(detailData));

                    Console.WriteLine($"Detail data written into json file named {selectedVehicleId}.json");








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