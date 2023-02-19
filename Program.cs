
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
    public static class Program
    {

        public static int Main(string[] args)
        {
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

            const string testUrl = "https://cars.com/";

            Console.WriteLine($"{testUrl} data crawling is starting");
            Console.WriteLine("please wait...");
            Console.WriteLine();

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

                List<string> files = new List<string>();

                // Create the CefSharp.OffScreen.ChromiumWebBrowser instance
                using (var browser = new ChromiumWebBrowser(testUrl))
                {
                    async Task WaitBrowserLoading(ChromiumWebBrowser _browser)
                    {
                        while (_browser.IsLoading && _browser.CanExecuteJavascriptInMainFrame)
                        {
                            await Task.Delay(500);
                        }
                    }
                    #region Gathering Data For Specific Filter Function

                    async Task GatherDataForSpecificFilter(
                            ChromiumWebBrowser _browser,
                            string stocktype = "used",
                            string make = "tesla",
                            string model = "tesla-model_s",
                            string maxprice = "100000",
                            string maxdistance = "all",
                            string zip = "94596"
                        )
                    {
                        var fileNamePrefix = $"{stocktype}_{make}_{model}_{maxprice}_{maxdistance}_{zip}";

                        // routing to cars.com
                        _ = await _browser.EvaluateScriptAsync(@"
                         window.location.href = ""https://www.cars.com/""
");

                        await Task.Delay(500);
                        await WaitBrowserLoading(_browser);

                        Console.WriteLine("Searching for " + stocktype + " " + make + " " + model + " within " + maxdistance + " miles of " + zip + " with max price of " + maxprice);

                        // filters retrieved from function is apply to form
                        _ = await _browser.EvaluateScriptAsync($@"
                 document.querySelector(""#make-model-search-stocktype"").value = ""{stocktype}"";
                document.querySelector(""#makes"").value = ""{make}"";
                document.querySelector(""#models"").value = ""{model}"";
                document.querySelector(""#make-model-max-price"").value = ""{maxprice}"";
                document.querySelector(""#make-model-maximum-distance"").value = ""{maxdistance}"";
                document.querySelector(""#make-model-zip"").value = ""{zip}"";
                document.querySelector(""#by-make-tab > div > div.sds-field.sds-home-search__submit > button"").click();
");
                        await Task.Delay(500);

                        await WaitBrowserLoading(_browser);

                        // gathering data from search result is provided by js function named gatherData
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

                        // runs gatherData function on console and get results from console by EvaluateScriptAsync function
                        var firstpage20 = await _browser.EvaluateScriptAsync(@$"
                    let data = [];
                    {JSgatherDataFn}
                    document.querySelector(""#vehicle-cards-container"").querySelectorAll('.vehicle-card').forEach(v => {{data.push(gatherData(v))}});
                    data;
");

                        List<VehicleSearchResultItemDTO> gatheredVehicles = new List<VehicleSearchResultItemDTO>();
                        var firstpage20Result = JsonSerializer.Deserialize<List<VehicleSearchResultItemDTO>>(JsonSerializer.Serialize(firstpage20.Result));

                        Console.WriteLine("First page gathered");

                        Console.WriteLine("Total gathered vehicles: " + firstpage20Result.Count);
                        Console.WriteLine($"Gathered Vehicles writes into file named {fileNamePrefix}_1_{gatheredVehicles.Count}.json");

                        files.Add($"{fileNamePrefix}_1_{gatheredVehicles.Count}.json");
                        File.WriteAllText($"{fileNamePrefix}_1_{gatheredVehicles.Count}.json", JsonSerializer.Serialize(firstpage20Result));

                        gatheredVehicles.AddRange(firstpage20Result);

                        // go page 2 by changing url
                        _ = await _browser.EvaluateScriptAsync(@"
let a = location.search;
location.href = '?page=2&page_size=20&'+a.slice(1,a.length)
                    ");


                        await Task.Delay(500);

                        await WaitBrowserLoading(_browser);



                        // gather second page data same method as page 1 crawling
                        var secondpage20 = await _browser.EvaluateScriptAsync(@$"
                    let data = [];
                    {JSgatherDataFn}
                    document.querySelector(""#vehicle-cards-container"").querySelectorAll('.vehicle-card').forEach(v => {{data.push(gatherData(v))}});
                    data;
");

                        var secondpage20Result = JsonSerializer.Deserialize<List<VehicleSearchResultItemDTO>>(JsonSerializer.Serialize(secondpage20.Result));

                        Console.WriteLine("Second page gathered");

                        Console.WriteLine("Total gathered vehicles: " + secondpage20Result.Count);
                        Console.WriteLine($"Gathered Vehicles writes into file named {fileNamePrefix}_2_{gatheredVehicles.Count}.json");

                        files.Add($"{fileNamePrefix}_2_{gatheredVehicles.Count}.json");
                        File.WriteAllText($"{fileNamePrefix}_2_{gatheredVehicles.Count}.json", JsonSerializer.Serialize(secondpage20Result));

                        gatheredVehicles.AddRange(secondpage20Result);

                        // after that ask prompt to choose vehicle for specific car crawling

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
                        while (!int.TryParse(Console.ReadLine(), out selectedVehicleIndex) || gatheredVehicles.Count - 1 < selectedVehicleIndex || selectedVehicleIndex < 0)
                        {
                            Console.WriteLine($"Invalid Input. Input must between 0 and {gatheredVehicles.Count}");
                        }

                        Console.WriteLine();
                        Console.WriteLine("Gathering vehicle details...");
                        var selectedVehicle = gatheredVehicles[selectedVehicleIndex];
                        var selectedVehicleId = selectedVehicle.DetailLink.Split('/')[selectedVehicle.DetailLink.Split('/').Length - 2];

                        // routing to car's detail page
                        _ = await _browser.EvaluateScriptAsync(@$"location.href = '{selectedVehicle.DetailLink}'");

                        await Task.Delay(500);
                        await WaitBrowserLoading(_browser);


                        // on detail page gathering data from detail page is provided by js function named getDetailData
                        var JSGatherDetailDataFn = @"
function getDetailData(){
    let basicKeys = [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.sds-page-section.basics-section > dl"")?.querySelectorAll('dt')].map(v => v?.innerText);
    let basicValues = [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.sds-page-section.basics-section > dl"")?.querySelectorAll('dd')].map(v => v?.innerText);
    let basicData = {};
    basicKeys.forEach((key, index) => {
        basicData[key] = basicValues[index];
    });

    let featuresKeys = [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.sds-page-section.features-section > dl"")?.querySelectorAll('dt')].map(v => v?.innerText);
    let featuresValues = [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > div.basics-content-wrapper > section.sds-page-section.features-section > dl"")?.querySelectorAll('dd')].map(v => v?.innerText);
    let featuresData = {};
    featuresKeys.forEach((key, index) => {
        featuresData[key] = featuresValues[index];
    });

    let ratingBreakdownKeys = [...document.querySelector(""#vehicle-reviews > div > div.review-breakdown > ul"")?.querySelectorAll('.sds-definition-list__display-name')].map(v => v?.innerText);
    let ratingBreakdownValues = [...document.querySelector(""#vehicle-reviews > div > div.review-breakdown > ul"")?.querySelectorAll('.sds-definition-list__value')].map(v => v?.innerText);
    let ratingBreakdownData = {};
    ratingBreakdownKeys.forEach((key, index) => {
        ratingBreakdownData[key] = ratingBreakdownValues[index];
    });

    let reviews = [];
    let reviewRatings = [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"")?.querySelectorAll('.sds-rating__count')].map(v => v?.innerText);
    let reviewTitles = [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"")?.querySelectorAll('h3')].map(v => v?.innerText);
    let reviewDates = [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"")?.querySelectorAll('div.review-byline.review-section > div:nth-child(1)')].map(v => v?.innerText);
    let reviewBy = [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"")?.querySelectorAll('div.review-byline.review-section > div:nth-child(2)')].map(v => v?.innerText);
    let reviewDescription =  [...document.querySelector(""#vehicle-reviews > div > div.section-content.sds-template-sidebar__content > div"")?.querySelectorAll('p')].map(v => v?.innerText);
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
        ""Images"": [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > vdp-gallery > gallery-slides"")?.querySelectorAll('img')].map(t => t.src),
        ""Contact"": document.querySelector(""#dealer-section--embedded1 > div > section > div"")?.innerText,
        ""StockType"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.title-section > p"")?.innerText,
        ""Title"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.title-section > h1"")?.innerText,
        ""MileAge"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.title-section > div.listing-mileage"")?.innerText,
        ""PrimaryPrice"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.price-section > span.primary-price"")?.innerText,
        ""PriceDrop"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.price-section > span.secondary-price.price-drop"")?.innerText,
        ""AvgMarketPrice"": document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.deal-gauge--container.great-deal > div.deal-gauge-graph-container > div.deal-gauge-box-plot > div > span > strong"")?.innerText,
        ""EstimatedMonthlyPayment"": document.querySelector(""#emp-tooltip-1 > span > a > span.js-estimated-monthly-payment-formatted-value-with-abr"")?.innerText,
        ""VehicleBadges"": [...document.querySelector(""#ae-skip-to-content > div.vdp-content-wrapper.price-history-grid > section > header > div.vehicle-badging"")?.querySelectorAll('span')].map(t => t?.innerText),
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
}".Trim();

                        await Task.Delay(3000);
                        var scriptResult = await _browser.EvaluateScriptAsync($@"
var detailResult = null;
{JSGatherDetailDataFn}
detailResult = getDetailData();
".Trim());
                        // runs function and get result from browser
                        var detailResult = await _browser.EvaluateScriptAsync($@"detailResult");

                        var detailData =JsonSerializer.Serialize(detailResult.Result);

                        Console.WriteLine("Detail data gathered");
                        Console.WriteLine($"Writes into {fileNamePrefix}_{selectedVehicleId}.json");


                        files.Add($"{fileNamePrefix}_{selectedVehicleId}.json");
                        File.WriteAllText($"{fileNamePrefix}_{selectedVehicleId}.json", detailData);

                        Console.WriteLine($"Detail data written into json file named {selectedVehicleId}.json");
                    }
                    #endregion

                    var initialLoadResponse = await browser.WaitForInitialLoadAsync();

                    if (!initialLoadResponse.Success)
                    {
                        throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
                    }

                    Console.WriteLine("Signing in to cars.com with credentials");
                    Console.WriteLine("Username: johngerson808@gmail.com");
                    Console.WriteLine("Password: test8008");
                    Console.WriteLine();

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

                    Console.WriteLine("Login successful");
                    Console.WriteLine();

                    // Tesla model s search
                    await GatherDataForSpecificFilter(browser);

                    Console.WriteLine("Tesla Model S data gathered successfully");
                    Console.WriteLine();


                    // Tesla model x search
                    await GatherDataForSpecificFilter(browser, model: "tesla-model_x");

                    Console.WriteLine("Tesla Model X data gathered successfully");
                    Console.WriteLine();

                    Console.WriteLine("Do you want me to different crawling? [Y/N]");
                    var startAgain = Console.ReadKey();

                    while (startAgain.Key == ConsoleKey.Y)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("Please enter the filter you want to crawl");
                        Console.WriteLine("Example: tesla-model_y");
                        Console.WriteLine();

                        var filter = Console.ReadLine();

                        await GatherDataForSpecificFilter(browser, model: filter);

                        Console.WriteLine();
                        Console.WriteLine();

                        Console.WriteLine("Do you want me to different crawling? [Y/N]");
                        startAgain = Console.ReadKey();
                    }

                }

                Console.WriteLine();
                Console.WriteLine("Files:");
                foreach (var item in files)
                {
                    Console.WriteLine($"{item}");
                }


                Console.WriteLine("Press any key to exit program");

                Console.ReadKey();

                Cef.Shutdown();
            });

            return 0;
        }

    }
}