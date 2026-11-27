using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TabulaWMS.Models;

public class UpdateProductDto
{
    [StringLength(10, MinimumLength = 1)]
    [RegularExpression(@"^[a-zA-Z0-9\-_]+$")]
    public string? SKU { get; set; }

    [StringLength(50, MinimumLength = 1)]
    public string? Name { get; set; }

    [Range(0, int.MaxValue)]
    public int? Quantity { get; set; }

    private string? _description;

    public string? Description
    {
        get => _description;
        set
        {
            _description = value;
            DescriptionSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DescriptionSpecified { get; private set; }

    private string? _location;

    [StringLength(50)]
    public string? Location
    {
        get => _location;
        set
        {
            _location = value;
            LocationSpecified = true;
        }
    }

    [JsonIgnore]
    public bool LocationSpecified { get; private set; }

    [Range(0.0, double.MaxValue)]
    public decimal? Price { get; set; }
}
