using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.Reports
{
    public static class ReportErrors
    {
        public static readonly Error NotFound =
            Error.NotFound("Report.NotFound", "The specified report was not found.");

        public static readonly Error UnauthorizedReporter =
            Error.Validation("Report.UnauthorizedReporter", "Only Students and Recruiters are authorized to submit reports.");

        public static readonly Error StudentCannotReportOthersDegree =
            Error.Validation("Report.StudentCannotReportOthersDegree", "Students are only permitted to submit reports for degrees issued to themselves.");

        public static readonly Error EvidenceRequired =
            Error.Validation("Report.EvidenceRequired", "An evidence file (PDF, PNG, or JPG) is required for submitting a report.");

        public static readonly Error ReportAlreadyExistsUnderReview =
            Error.Conflict("Report.AlreadyExistsUnderReview", "A report for this degree is already under review by your account.");

        public static readonly Error InvalidEvidenceFormat =
            Error.Validation("Report.InvalidEvidenceFormat", "The evidence file signature or content type is invalid. Only valid PDF, PNG, or JPG files are allowed.");

        public static readonly Error UnauthorizedEvidenceDownload =
            Error.Validation("Report.UnauthorizedEvidenceDownload", "You do not have permission to download evidence for this report.");

        public static readonly Error EmptyRejectionReason =
            Error.Validation("Report.EmptyRejectionReason", "A valid reason must be provided when rejecting a report.");

        public static readonly Error AlreadyReviewed =
            Error.Conflict("Report.AlreadyReviewed", "This report has already been reviewed.");
    }
}
