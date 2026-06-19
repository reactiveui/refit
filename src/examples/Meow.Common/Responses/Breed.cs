// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Meow.Responses;

/// <summary>Describes a cat breed.</summary>
public class Breed
{
    /// <summary>Gets or sets the weight of the breed.</summary>
    [JsonPropertyName("weight")]
    public Weight? Weight { get; set; }

    /// <summary>Gets or sets the breed identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Gets or sets the breed name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the CFA reference URL.</summary>
    [JsonPropertyName("cfa_url")]
    public string? CfaUrl { get; set; }

    /// <summary>Gets or sets the Vetstreet reference URL.</summary>
    [JsonPropertyName("vetstreet_url")]
    public string? VetstreetUrl { get; set; }

    /// <summary>Gets or sets the VCA Hospitals reference URL.</summary>
    [JsonPropertyName("vcahospitals_url")]
    public string? VcaHospitalsUrl { get; set; }

    /// <summary>Gets or sets the breed temperament.</summary>
    [JsonPropertyName("temperament")]
    public string? Temperament { get; set; }

    /// <summary>Gets or sets the breed origin.</summary>
    [JsonPropertyName("origin")]
    public string? Origin { get; set; }

    /// <summary>Gets or sets the list of country codes.</summary>
    [JsonPropertyName("country_codes")]
    public string? CountryCodes { get; set; }

    /// <summary>Gets or sets the primary country code.</summary>
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    /// <summary>Gets or sets the breed description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the typical life span.</summary>
    [JsonPropertyName("life_span")]
    public string? LifeSpan { get; set; }

    /// <summary>Gets or sets the indoor rating.</summary>
    [JsonPropertyName("indoor")]
    public int Indoor { get; set; }

    /// <summary>Gets or sets the lap-cat rating.</summary>
    [JsonPropertyName("lap")]
    public int Lap { get; set; }

    /// <summary>Gets or sets alternative breed names.</summary>
    [JsonPropertyName("alt_names")]
    public string? AltNames { get; set; }

    /// <summary>Gets or sets the adaptability rating.</summary>
    [JsonPropertyName("adaptability")]
    public int Adaptability { get; set; }

    /// <summary>Gets or sets the affection level rating.</summary>
    [JsonPropertyName("affection_level")]
    public int AffectionLevel { get; set; }

    /// <summary>Gets or sets the child-friendliness rating.</summary>
    [JsonPropertyName("child_friendly")]
    public int ChildFriendly { get; set; }

    /// <summary>Gets or sets the dog-friendliness rating.</summary>
    [JsonPropertyName("dog_friendly")]
    public int DogFriendly { get; set; }

    /// <summary>Gets or sets the energy level rating.</summary>
    [JsonPropertyName("energy_level")]
    public int EnergyLevel { get; set; }

    /// <summary>Gets or sets the grooming requirement rating.</summary>
    [JsonPropertyName("grooming")]
    public int Grooming { get; set; }

    /// <summary>Gets or sets the health issues rating.</summary>
    [JsonPropertyName("health_issues")]
    public int HealthIssues { get; set; }

    /// <summary>Gets or sets the intelligence rating.</summary>
    [JsonPropertyName("intelligence")]
    public int Intelligence { get; set; }

    /// <summary>Gets or sets the shedding level rating.</summary>
    [JsonPropertyName("shedding_level")]
    public int SheddingLevel { get; set; }

    /// <summary>Gets or sets the social needs rating.</summary>
    [JsonPropertyName("social_needs")]
    public int SocialNeeds { get; set; }

    /// <summary>Gets or sets the stranger-friendliness rating.</summary>
    [JsonPropertyName("stranger_friendly")]
    public int StrangerFriendly { get; set; }

    /// <summary>Gets or sets the vocalisation rating.</summary>
    [JsonPropertyName("vocalisation")]
    public int Vocalisation { get; set; }

    /// <summary>Gets or sets a value indicating whether the breed is experimental.</summary>
    [JsonPropertyName("experimental")]
    public int Experimental { get; set; }

    /// <summary>Gets or sets a value indicating whether the breed is hairless.</summary>
    [JsonPropertyName("hairless")]
    public int Hairless { get; set; }

    /// <summary>Gets or sets a value indicating whether the breed is natural.</summary>
    [JsonPropertyName("natural")]
    public int Natural { get; set; }

    /// <summary>Gets or sets a value indicating whether the breed is rare.</summary>
    [JsonPropertyName("rare")]
    public int Rare { get; set; }

    /// <summary>Gets or sets a value indicating whether the breed is a rex.</summary>
    [JsonPropertyName("rex")]
    public int Rex { get; set; }

    /// <summary>Gets or sets a value indicating whether the breed has a suppressed tail.</summary>
    [JsonPropertyName("suppressed_tail")]
    public int SuppressedTail { get; set; }

    /// <summary>Gets or sets a value indicating whether the breed has short legs.</summary>
    [JsonPropertyName("short_legs")]
    public int ShortLegs { get; set; }

    /// <summary>Gets or sets the Wikipedia reference URL.</summary>
    [JsonPropertyName("wikipedia_url")]
    public string? WikipediaUrl { get; set; }

    /// <summary>Gets or sets a value indicating whether the breed is hypoallergenic.</summary>
    [JsonPropertyName("hypoallergenic")]
    public int Hypoallergenic { get; set; }
}
