using FluentValidation;

namespace ChainDegree.Core.Application.Reports.Commands.SubmitReport
{
    public class SubmitReportCommandValidator : AbstractValidator<SubmitReportCommand>
    {
        public SubmitReportCommandValidator()
        {
            RuleFor(x => x.TargetDegreeId)
                .NotEmpty()
                .WithMessage("Target degree ID is required.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Report description is required.")
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(x => x.EvidenceStream)
                .NotNull()
                .WithMessage("Evidence file stream is required.");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .WithMessage("Evidence content type is required.");

            RuleFor(x => x.FileName)
                .NotEmpty()
                .WithMessage("Evidence file name is required.");
        }
    }
}
