using CollisionFlow.Domain;
using Shouldly;

namespace CollisionFlow.Domain.Tests;

public sealed class RepairJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly IStatusTransitionPolicy Policy = StatusTransitionPolicy.Default;

    private static RepairJob NewJob() => RepairJob.Open(
        jobNumber: "RO-10001",
        customerName: "Marcus Bell",
        vehicle: new Vehicle(2021, "Toyota", "RAV4"),
        repairCenter: "Westmont",
        nowUtc: Now);

    [Fact]
    public void A_new_repair_order_starts_as_Received()
    {
        NewJob().Status.ShouldBe(RepairStatus.Received);
    }

    [Fact]
    public void A_legal_transition_updates_the_status_and_the_timestamp()
    {
        var job = NewJob();
        var later = Now.AddHours(2);

        var changed = job.ChangeStatus(RepairStatus.InProgress, Policy, later);

        changed.ShouldBeTrue();
        job.Status.ShouldBe(RepairStatus.InProgress);
        job.UpdatedUtc.ShouldBe(later);
    }

    [Fact]
    public void Re_sending_the_current_status_is_a_no_op_which_is_what_makes_PUT_safe_to_retry()
    {
        var job = NewJob();

        var changed = job.ChangeStatus(RepairStatus.Received, Policy, Now.AddHours(5));

        changed.ShouldBeFalse();
        job.Status.ShouldBe(RepairStatus.Received);
        job.UpdatedUtc.ShouldBe(Now, "a no-op must not look like activity");
    }

    [Fact]
    public void An_illegal_transition_is_refused()
    {
        var job = NewJob();

        var ex = Should.Throw<InvalidStatusTransitionException>(
            () => job.ChangeStatus(RepairStatus.Completed, Policy, Now));

        ex.From.ShouldBe(RepairStatus.Received);
        ex.To.ShouldBe(RepairStatus.Completed);
        job.Status.ShouldBe(RepairStatus.Received, "a rejected change must leave the job untouched");
    }

    [Fact]
    public void A_completed_repair_order_cannot_be_reopened()
    {
        var job = RepairJob.Rehydrate(
            Guid.NewGuid(), "RO-10002", "Priya Raman", new Vehicle(2019, "Honda", "Civic"),
            "Naperville", RepairStatus.Completed, Now.AddDays(-30), Now.AddDays(-1));

        Should.Throw<InvalidStatusTransitionException>(
            () => job.ChangeStatus(RepairStatus.InProgress, Policy, Now));
    }

    [Fact]
    public void A_status_outside_the_approved_set_is_refused_even_though_the_cast_compiles()
    {
        var job = NewJob();

        // An enum does not constrain its own values at runtime. This is the check
        // that actually enforces "only approved statuses can be used".
        Should.Throw<ArgumentOutOfRangeException>(
            () => job.ChangeStatus((RepairStatus)99, Policy, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_repair_order_requires_a_customer_name(string customerName)
    {
        Should.Throw<ArgumentException>(() => RepairJob.Open(
            "RO-10003", customerName, new Vehicle(2020, "Ford", "F-150"), "Oak Lawn", Now));
    }

    [Fact]
    public void A_repair_order_requires_a_vehicle()
    {
        Should.Throw<ArgumentNullException>(() => RepairJob.Open(
            "RO-10004", "Wes Kaminski", null!, "Oak Lawn", Now));
    }

    [Fact]
    public void Rehydrating_preserves_a_status_that_Open_would_never_produce()
    {
        var job = RepairJob.Rehydrate(
            Guid.NewGuid(), "RO-10005", "Grace Yeoh", new Vehicle(2020, "Kia", "Telluride"),
            "Naperville", RepairStatus.QualityCheck, Now.AddDays(-8), Now.AddHours(-2));

        job.Status.ShouldBe(RepairStatus.QualityCheck);
    }
}
