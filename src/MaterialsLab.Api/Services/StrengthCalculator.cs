using MaterialsLab.Api.Models;

namespace MaterialsLab.Api.Services;

public static class StrengthCalculator
{
    public static int CalculateStrengthPsi(decimal diameterIn, decimal maxLoadLbf)
    {
        var radius = diameterIn / 2m;
        var area = (decimal)Math.PI * radius * radius;
        var raw = maxLoadLbf / area;
        return (int)(Math.Round(raw / 10m, MidpointRounding.AwayFromZero) * 10m);
    }

    public static bool IsWithinTolerance(DateTimeOffset sampledAt, SpecimenAge age, DateTimeOffset testedAt)
    {
        var expected = sampledAt.AddDays((int)age);
        var tolerance = age == SpecimenAge.Days7 ? TimeSpan.FromHours(6) : TimeSpan.FromHours(20);
        return (testedAt - expected).Duration() <= tolerance;
    }

    public static SampleResult? CalculateResult(Sample sample)
    {
        var specimen28 = sample.Specimens.Where(sp => sp.Age == SpecimenAge.Days28).ToList();
        if (specimen28.Count == 0 || specimen28.Any(sp => sp.StrengthPsi == null))
        {
            return null;
        }
        var average = specimen28.Average(sp => sp.StrengthPsi!.Value);
        return average >= sample.RequiredStrengthPsi ? SampleResult.Pass : SampleResult.Fail;
    }
}