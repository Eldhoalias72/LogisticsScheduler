namespace LogisticsScheduler.API.DTOs
{
    public class GeoapifyRouteResponse
    {
        public List<RouteFeature> features { get; set; }
    }

    public class RouteFeature
    {
        public RouteProperties properties { get; set; }
    }

    public class RouteProperties
    {
        public double distance { get; set; } // in meters
        public double time { get; set; } // in seconds
    }
}