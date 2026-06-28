using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public class IssueDegreeCommandValidator : AbstractValidator<IssueDegreeCommand>
    {
        public IssueDegreeCommandValidator()
        {
            RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Institution id is required.");
            RuleFor(x => x.SignedByRegistrarId).NotEmpty().WithMessage("Signed by registrar id is required.");
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("Student id is required.");
            RuleFor(x => x.Major).NotEmpty().WithMessage("Major is required.");
            RuleFor(x => x.Classification).NotEmpty().WithMessage("Classification is required.");
        }
    }
}
