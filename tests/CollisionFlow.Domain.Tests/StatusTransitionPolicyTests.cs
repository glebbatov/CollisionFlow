using CollisionFlow.Domain;
using Shouldly;

namespace CollisionFlow.Domain.Tests;

/// <summary>
/// Exercises the workflow graph over its entire input space.
/// </summary>
public sealed class StatusTransitionPolicyTests
{
    /// <summary>
    /// The workflow, transcribed by hand from the specification.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT derived from <see cref="StatusTransitionPolicy.DefaultTransitions"/>.
    /// A test that reads its expectations from the code under test proves only that
    /// the code equals itself. This table is an independent statement of what the
    /// business agreed to, so editing the production workflow without agreement
    /// breaks the build.
    /// </remarks>
    private static readonly Dictionary<RepairStatus, RepairStatus[]> ExpectedAllowed = new()
    {
        [RepairStatus.Received] = [RepairStatus.InProgress, RepairStatus.WaitingOnParts],
        [RepairStatus.InProgress] = [RepairStatus.WaitingOnParts, RepairStatus.QualityCheck],
        [RepairStatus.WaitingOnParts] = [RepairStatus.InProgress],
        [RepairStatus.QualityCheck] = [RepairStatus.InProgress, RepairStatus.ReadyForPickup],
        [RepairStatus.ReadyForPickup] = [RepairStatus.Completed],
        [RepairStatus.Completed] = [],
    };

    /// <summary>Every ordered pair of statuses - all 36 of them.</summary>
    public static TheoryData<RepairStatus, RepairStatus> AllStatusPairs()
    {
        var data = new TheoryData<RepairStatus, RepairStatus>();

        foreach (var from in Enum.GetValues<RepairStatus>())
        {
            foreach (var to in Enum.GetValues<RepairStatus>())
            {
                data.Add(from, to);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllStatusPairs))]
    public void IsAllowed_agrees_with_the_specified_workflow_for_every_pair(
        RepairStatus from,
        RepairStatus to)
    {
        var expected = ExpectedAllowed[from].Contains(to);

        StatusTransitionPolicy.Default.IsAllowed(from, to).ShouldBe(
            expected,
            $"transition {from} -> {to} should be {(expected ? "allowed" : "rejected")}");
    }

    [Fact]
    public void Completed_is_terminal_so_a_closed_repair_order_cannot_be_reopened()
    {
        StatusTransitionPolicy.Default.AllowedNextFrom(RepairStatus.Completed).ShouldBeEmpty();
        RepairStatusInfo.IsTerminal(RepairStatus.Completed).ShouldBeTrue();
    }

    [Fact]
    public void Quality_check_can_send_work_back_for_rework()
    {
        // The rework loop is what makes this a workflow rather than a progress bar.
        StatusTransitionPolicy.Default
            .IsAllowed(RepairStatus.QualityCheck, RepairStatus.InProgress)
            .ShouldBeTrue();
    }

    [Fact]
    public void A_parts_hold_resumes_rather_than_restarts()
    {
        StatusTransitionPolicy.Default
            .IsAllowed(RepairStatus.WaitingOnParts, RepairStatus.InProgress)
            .ShouldBeTrue();

        StatusTransitionPolicy.Default
            .IsAllowed(RepairStatus.WaitingOnParts, RepairStatus.Received)
            .ShouldBeFalse();
    }

    [Fact]
    public void AllowedNextFrom_returns_transitions_in_workflow_order()
    {
        var next = StatusTransitionPolicy.Default.AllowedNextFrom(RepairStatus.QualityCheck);

        next.ShouldBe(new[] { RepairStatus.InProgress, RepairStatus.ReadyForPickup });
    }

    [Fact]
    public void A_policy_can_be_built_from_an_arbitrary_edge_set()
    {
        // This is the path the SQL-backed policy takes: rows in, rules out.
        var policy = new StatusTransitionPolicy(new[]
        {
            new StatusTransition(RepairStatus.Received, RepairStatus.Completed),
        });

        policy.IsAllowed(RepairStatus.Received, RepairStatus.Completed).ShouldBeTrue();
        policy.IsAllowed(RepairStatus.Received, RepairStatus.InProgress).ShouldBeFalse();
    }

    [Fact]
    public void Every_status_has_a_display_name()
    {
        foreach (var status in Enum.GetValues<RepairStatus>())
        {
            RepairStatusInfo.DisplayName(status).ShouldNotBeNullOrWhiteSpace();
        }
    }
}
