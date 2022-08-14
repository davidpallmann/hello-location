using Amazon;
using Amazon.LocationService;
using Amazon.LocationService.Model;

namespace HelloLocation
{ 
    public class Program
    {
        static RegionEndpoint Region = RegionEndpoint.USWest2;
        static AmazonLocationServiceClient Client = null!;

        const string IndexName = "HelloLocationHERE";
        const string RouteCalculatorName = "HelloLocationHERE";

        static async Task Main(string[] args)
        {
            if (args.Length < 2 || args[0] == "-h")
            {
                Console.WriteLine("Usage:   dotnet run -- <action>");
                Console.WriteLine("Actions: search \"<place> ................ searches for places matching the text");
                Console.WriteLine("Actions: near \"<place1> <place2> ........ searches for place-1 near place-2");
                Console.WriteLine("Actions: route \"<place1> <place2> ....... finds the distance and route between place-1 and place-2");
                Console.WriteLine("Example: dotnet run -- geocode \"367 Wildwood Rd, Ronkonkoma, NY\"");
                Environment.Exit(1);
            }

            Client = new AmazonLocationServiceClient(Region);

            switch (args[0].ToLower())
            {
                case "search":

                    // "search" <place> - find place

                    var searchResponse = await SearchPlace(args[1]);
                    foreach (var result in searchResponse.Results)
                    {
                        Console.WriteLine($"{result.Place.Label} ({result.Place.Geometry.Point[0]}° , {result.Place.Geometry.Point[1]}°)");
                    }
                    break;

                case "near":

                    // "near" <place1> <place2> - find place1 near place2

                    var searchPlace2Response = await SearchPlace(args[2]);
                    if (searchPlace2Response.Results.Count == 0)
                    {
                        Console.WriteLine($"I couldn't find this place: {args[2]}");
                        Environment.Exit(2);
                    }

                    Console.WriteLine($"Searching for '{args[1]}' near {args[2]}");

                    // search for place1, using place2 as a bias position

                    var searchNearRequest = new SearchPlaceIndexForTextRequest()
                    {
                        IndexName = IndexName,
                        Text = args[1],
                        BiasPosition = searchPlace2Response.Results[0].Place.Geometry.Point
                    };

                    var searchNearResponse = await Client.SearchPlaceIndexForTextAsync(searchNearRequest);
                    foreach (var result in searchNearResponse.Results)
                    {
                        Console.WriteLine($"{result.Place.Label} ({result.Place.Geometry.Point[0]}° , {result.Place.Geometry.Point[1]}°)");
                    }
                    break;

                case "route":

                    // "route" <place1> <place2> - find the distance and duration between place 1 and place 2

                    var place1Response = await SearchPlace(args[1]);
                    if (place1Response.Results.Count == 0)
                    {
                        Console.WriteLine($"I couldn't find this place: {args[1]}");
                        Environment.Exit(2);
                    }
                    var place1 = place1Response.Results[0].Place;

                    var place2Response = await SearchPlace(args[2]);
                    if (place2Response.Results.Count == 0)
                    {
                        Console.WriteLine($"I couldn't find this place: {args[2]}");
                        Environment.Exit(2);
                    }
                    var place2 = place2Response.Results[0].Place;

                    var routeResponse = await GetRoute(place1, place2);

                    Console.WriteLine($"{place1.Label}\nis {routeResponse.Summary.Distance:F2} {routeResponse.Summary.DistanceUnit} from\n{place2.Label}");
                    Console.WriteLine($"Travel time: {routeResponse.Summary.DurationSeconds / 60:F2} minutes");
                    break;

                default:
                    Console.WriteLine("Unrecognized action");
                    break;
            }
        }

        /// <summary>
        /// Search for a place.
        /// </summary>
        /// <param name="text">Place name or address</param>
        /// <returns>SearchPlaceIndexForTextResponse</returns>

        static async Task<SearchPlaceIndexForTextResponse> SearchPlace(string text)
        {
            var location1Request = new SearchPlaceIndexForTextRequest()
            {
                IndexName = IndexName,
                Text = text
            };
            return await Client.SearchPlaceIndexForTextAsync(location1Request);
        }


        /// <summary>
        /// Calculate route between 2 places.
        /// </summary>
        /// <param name="place1">First place</param>
        /// <param name="place2">Second place</param>
        /// <returns>CalculateRouteResult</returns>

        static async Task<CalculateRouteResponse> GetRoute(Place place1, Place place2)
        {
            var routeRequest = new CalculateRouteRequest()
            {
                CalculatorName = RouteCalculatorName,
                DeparturePosition = place1.Geometry.Point,
                DestinationPosition = place2.Geometry.Point,
                DistanceUnit = DistanceUnit.Miles,
                TravelMode = TravelMode.Car
            };
            return await Client.CalculateRouteAsync(routeRequest);
        }
    }
}