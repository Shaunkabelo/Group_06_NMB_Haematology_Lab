using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels
{
    public class WorkloadModel
    {
        public int TestsStartedToday { get; set; }
        public int TestsCompletedToday { get; set; }
        public int UrgentStartedToday { get; set; }
        public int UrgentCompletedToday { get; set; }
        public int VerificationsProcessedToday { get; set; }
        public int VerificationsDoneToday { get; set; }

        public int TestsCompletedPercent => TestsStartedToday == 0 ? 0 : (int)(100.0 * TestsCompletedToday / TestsStartedToday);
        public int UrgentHandledPercent => UrgentStartedToday == 0 ? 0 : (int)(100.0 * UrgentCompletedToday / UrgentStartedToday);
        public int VerificationsDonePercent => VerificationsProcessedToday == 0 ? 0 : (int)(100.0 * VerificationsDoneToday / VerificationsProcessedToday);
    }

    public class TechDashboardIndexModel
    {
        public List<AvailableRequestModel> AvailableRequests { get; set; } = new();
        public List<MyActiveRequestModel> MyActiveRequests { get; set; } = new();
        public List<AwaitingVerificationModel> PendingVerification { get; set; } = new();
        public List<ReturnedForReviewModel> AwaitingMyReview { get; set; } = new();
        public WorkloadModel Workload { get; set; } = new();

        public int AvailableTestsCount => AvailableRequests.SelectMany(r => r.Tests).Count(t => t.TestStatus == "Submitted");
        public int MyActiveTestsCount => MyActiveRequests.SelectMany(r => r.Tests).Count();
        public int OverdueTestsCount => MyActiveRequests.SelectMany(r => r.Tests).Count(t => t.IsOverdue);
        public int AwaitingMyReviewCount => AwaitingMyReview.Count;
        public int UrgentActiveCount => MyActiveRequests.Where(r => r.Urgency == "Urgent").Sum(r => r.Tests.Count);
        public int StatActiveCount => MyActiveRequests.Where(r => r.Urgency == "STAT").Sum(r => r.Tests.Count);
        public int NearingTurnaroundCount => MyActiveRequests.SelectMany(r => r.Tests).Count(t => t.IsNearingDeadline);
    }
}
