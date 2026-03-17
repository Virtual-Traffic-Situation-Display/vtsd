using vTFMS.Models;

namespace vTFMS.Services;

public interface IProfileService
{
    string ProfilesDirectory { get; }
    string FiltersDirectory { get; }
    void SaveFilters(FlightFilterProfile profile, string path);
    FlightFilterProfile LoadFilters(string path);
    string? LastFiltersPath { get; set; }
}