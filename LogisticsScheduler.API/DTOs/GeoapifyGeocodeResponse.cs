namespace LogisticsScheduler.API.DTOs
{
    public class GeoapifyGeocodeResponse
    {
        public List<GeocodeFeature> features { get; set; }
    }

    public class GeocodeFeature
    {
        public GeocodeGeometry geometry { get; set; }
    }

    public class GeocodeGeometry
    {
        // Geoapify returns coordinates as [lon, lat]
        public List<double> coordinates { get; set; }
    }

}
