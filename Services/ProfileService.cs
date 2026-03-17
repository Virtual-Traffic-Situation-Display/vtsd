using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using vTFMS.Models;

namespace vTFMS.Services;

public class ProfileService : IProfileService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public string ProfilesDirectory { get; }
    public string FiltersDirectory { get; }
    public string? LastFiltersPath { get; set; }

    public ProfileService()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);
        ProfilesDirectory = Path.Combine(appData, "vTFMS", "Profiles");
        FiltersDirectory = Path.Combine(appData, "vTFMS", "Filters");

        Directory.CreateDirectory(ProfilesDirectory);
        Directory.CreateDirectory(FiltersDirectory);
    }

    public void SaveFilters(FlightFilterProfile profile, string path)
    {
        var json = JsonSerializer.Serialize(profile, _options);
        File.WriteAllText(path, json);
        LastFiltersPath = path;
    }

    public FlightFilterProfile LoadFilters(string path)
    {
        if (!File.Exists(path)) return new();

        var json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<FlightFilterProfile>(json);
        LastFiltersPath = path;
        return profile ?? new();
    }
}