using CollisionFlow.Domain;
using Shouldly;

namespace CollisionFlow.Domain.Tests;

public sealed class VehicleTests
{
    [Fact]
    public void Description_reads_the_way_a_service_advisor_would_say_it()
    {
        new Vehicle(2021, "Toyota", "RAV4").Description.ShouldBe("2021 Toyota RAV4");
    }

    [Fact]
    public void Surrounding_whitespace_is_trimmed_so_it_cannot_reach_a_customer_facing_screen()
    {
        var vehicle = new Vehicle(2022, "  Mazda ", " CX-5  ");

        vehicle.Make.ShouldBe("Mazda");
        vehicle.Model.ShouldBe("CX-5");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public void An_implausible_year_is_rejected(int year)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Vehicle(year, "Ford", "F-150"));
    }

    [Theory]
    [InlineData("", "Civic")]
    [InlineData("   ", "Civic")]
    [InlineData("Honda", "")]
    [InlineData("Honda", "   ")]
    public void Make_and_model_are_required(string make, string model)
    {
        Should.Throw<ArgumentException>(() => new Vehicle(2020, make, model));
    }

    [Fact]
    public void Two_vehicles_with_the_same_details_are_equal_because_it_is_a_value_object()
    {
        new Vehicle(2021, "Toyota", "RAV4").ShouldBe(new Vehicle(2021, "Toyota", "RAV4"));
    }
}
